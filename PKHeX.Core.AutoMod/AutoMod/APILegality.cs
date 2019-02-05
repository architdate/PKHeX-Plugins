using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static PKHeX.Core.LegalityCheckStrings;

namespace PKHeX.Core.AutoMod
{
    public static class API
    {
        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <param name="sav">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is satisfactory, otherwise use current auto legality functionality</param>
        public static PKM GetLegalFromTemplate(this SaveFile sav, PKM template, ShowdownSet set, out bool satisfied)
        {
            int Form = template.AltForm;
            if (set.Form != null && FixFormes(set, out set))
            {
                Form = set.FormIndex;
                template.ApplySetDetails(set);
            }
            int HPType = template.HPType;
            var destType = sav.PKMType;

            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves);
            foreach (var enc in encounters)
            {
                var ver = enc is IVersion v ? (int)v.Version: sav.Game;
                var tr = TrainerSettings.GetSavedTrainerData(ver, sav);
                var raw = enc.ConvertToPKM(tr);
                var pk = PKMConverter.ConvertToType(raw, destType, out _);
                ApplySetDetails(pk, set, Form, HPType, raw, sav);

                var la = new LegalityAnalysis(pk);
                if (la.Valid)
                {
                    satisfied = true;
                    return pk;
                }
                Console.WriteLine(la.Report());
            }
            satisfied = false;
            return template;
        }

        /// <summary>
        /// Modifies the provided <see cref="pk"/> to the specifications required by <see cref="set"/>.
        /// </summary>
        /// <param name="pk">Converted final pkm to apply details to</param>
        /// <param name="set">Set details required</param>
        /// <param name="Form">Alternate form required</param>
        /// <param name="HPType">Hidden Power type requirement</param>
        /// <param name="unconverted">Original pkm data</param>
        private static void ApplySetDetails(PKM pk, ShowdownSet set, int Form, int HPType, PKM unconverted, ITrainerInfo handler)
        {
            var info = new LegalInfo(pk);
            var pidiv = info.PIDIV ?? MethodFinder.Analyze(pk);
            var Method = pidiv?.Type ?? PIDType.None;

            SetVersion(pk, unconverted); // PreEmptive Version setting
            pk.SetSpeciesLevel(set, Form);
            pk.SetMovesEVsItems(set);
            pk.SetTrainerDataAndMemories(handler);
            pk.SetNatureAbility(set);
            SetIVsPID(pk, set, Method, HPType, unconverted);

            PrintLegality(pk);

            ColosseumFixes(pk);
            pk.SetSuggestedHyperTrainingData(pk.IVs); // Hypertrain
            pk.SetEncryptionConstant();
            pk.SetShinyBoolean(set.Shiny);
            CheckAndSetFateful(pk);
            pk.FixGender(set);
            pk.SetSuggestedRibbons();
            pk.SetSuggestedMemories();
            pk.SetSuggestedBall();
            pk.SetHappiness();
            pk.SetBelugaValues();
        }

        /// <summary>
        /// Debugging tool
        /// </summary>
        /// <param name="pk">PKM whose legality must be printed</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void PrintLegality(PKM pk) => Debug.WriteLine(new LegalityAnalysis(pk).Report());

        /// <summary>
        /// Validate and Set the gender if needed
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        private static void ValidateGender(PKM pk)
        {
            bool genderValid = pk.IsGenderValid();
            if (!genderValid)
            {
                if (pk.Format == 4 && pk.Species == 292) // Shedinja glitch
                {
                    // should match original gender
                    var gender = PKX.GetGenderFromPIDAndRatio(pk.PID, 0x7F); // 50-50
                    if (gender == pk.Gender)
                        genderValid = true;
                }
                else if (pk.Format > 5 && (pk.Species == 183 || pk.Species == 184))
                {
                    var gv = pk.PID & 0xFF;
                    if (gv > 63 && pk.Gender == 1) // evolved from azurill after transferring to keep gender
                        genderValid = true;
                }
            }
            else
            {
                // check for mixed->fixed gender incompatibility by checking the gender of the original species
                if (Legal.FixedGenderFromBiGender.Contains(pk.Species) && pk.Gender != 2) // shedinja
                {
                    var gender = PKX.GetGenderFromPID(new LegalInfo(pk).EncounterMatch.Species, pk.EncryptionConstant);
                    pk.Gender = gender;
                    // genderValid = true; already true if we reach here
                }
            }

            if (genderValid)
                return;

            switch (pk.Gender)
            {
                case 0: pk.Gender = 1; break;
                case 1: pk.Gender = 0; break;
                default: pk.GetSaneGender(); break;
            }
        }

        /// <summary>
        /// Set Version override for GSC and RBY games
        /// </summary>
        /// <param name="pk">Return PKM</param>
        /// <param name="original">Generated PKM</param>
        private static void SetVersion(PKM pk, PKM original)
        {
            switch (original.Version)
            {
                case (int)GameVersion.RBY:
                    pk.Version = (int)GameVersion.RD;
                    break;
                case (int)GameVersion.GSC:
                    pk.Version = (int)GameVersion.C;
                    break;
                case (int)GameVersion.UM:
                case (int)GameVersion.US:
                    if (original.Species == 658 && original.AltForm == 1)
                        pk.Version = (int)GameVersion.SN; // Ash-Greninja
                    else
                        pk.Version = original.Version;
                    break;
                default:
                    pk.Version = original.Version;
                    break;
            }
        }

        private static void CheckAndSetFateful(PKM pk)
        {
            var la = new LegalityAnalysis(pk);
            string Report = la.Report();
            if (Report.Contains(LFatefulMysteryMissing) || Report.Contains(LFatefulMissing))
                pk.FatefulEncounter = true;
            else if (Report.Contains(LFatefulInvalid))
                pk.FatefulEncounter = false;
        }

        /// <summary>
        /// Fix Formes that are illegal outside of battle
        /// </summary>
        /// <param name="set">Original Showdown Set</param>
        /// <param name="changedSet">Edited Showdown Set</param>
        /// <returns>boolen that checks if a form is fixed or not</returns>
        private static bool FixFormes(ShowdownSet set, out ShowdownSet changedSet)
        {
            changedSet = set;
            var badForm = ShowdownUtil.IsInvalidForm(set.Form);
            if (!badForm)
                return false;

            changedSet = new ShowdownSet(set.Text.Replace($"-{set.Form}", string.Empty));
            return true;
        }

        /// <summary>
        /// Set IV Values for the pokemon
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="set"></param>
        /// <param name="method"></param>
        /// <param name="hpType"></param>
        /// <param name="original"></param>
        private static void SetIVsPID(PKM pk, ShowdownSet set, PIDType method, int hpType, PKM original)
        {
            // Useful Values for computation
            int Species = pk.Species;
            int Nature = pk.Nature;
            int Gender = pk.Gender;
            int AbilityNumber = pk.AbilityNumber; // 1,2,4 (HA)

            // Find the encounter
            var li = EncounterFinder.FindVerifiedEncounter(original);
            // TODO: Something about the gen 5 events. Maybe check for nature and shiny val and not touch the PID in that case?
            // Also need to figure out hidden power handling in that case.. for PIDType 0 that may isn't even be possible.

            if (pk.GenNumber > 4 || pk.VC)
            {
                pk.IVs = set.IVs;
                if (Species == 658 && pk.AltForm == 1)
                    pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
                if (method != PIDType.G5MGShiny) pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, (uint)(AbilityNumber * 0x10001));
            }
            else
            {
                pk.IVs = set.IVs;
                if (li.EncounterMatch is PCD)
                    return;
                FindPIDIV(pk, method, hpType);
                ValidateGender(pk);
            }
        }

        /// <summary>
        /// Method to set PID, IV while validating nature.
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="Method">Given Method</param>
        /// <param name="HPType">HPType INT for preserving Hidden powers</param>
        private static void FindPIDIV(PKM pk, PIDType Method, int HPType)
        {
            if (Method == PIDType.None)
            {
                Method = FindLikelyPIDType(pk);
                if (pk.Version == 15) Method = PIDType.CXD;
                if (Method == PIDType.None) pk.SetPIDGender(pk.Gender);
            }
            var iterPKM = pk.Clone();
            while (true)
            {
                uint seed = Util.Rand32();
                PIDGenerator.SetValuesFromSeed(pk, Method, seed);
                if (!(pk.Ability == iterPKM.Ability && pk.AbilityNumber == iterPKM.AbilityNumber && pk.Nature == iterPKM.Nature))
                    continue;
                if (pk.PID % 25 == iterPKM.Nature && pk.HPType == HPType) // Util.Rand32 is the way to go
                    break;
                pk = iterPKM;
            }
        }

        /// <summary>
        /// Secondary fallback if PIDType.None to slot the PKM into its most likely type
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <returns>PIDType that is likely used</returns>
        private static PIDType FindLikelyPIDType(PKM pk)
        {
            if (BruteForce.UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.BACD_R))
                return PIDType.BACD_R;
            if (BruteForce.UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
                return PIDType.Method_2;
            if (pk.Species == 490 && pk.Gen4)
            {
                pk.Egg_Location = 2002; // todo: really shouldn't be doing this, don't modify pkm
                return PIDType.Method_1;
            }
            switch (pk.GenNumber)
            {
                case 3:
                    switch (EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch)
                    {
                        case WC3 g:
                            return g.Method;
                        case EncounterStatic _:
                            switch (pk.Version)
                            {
                                case (int)GameVersion.CXD: return PIDType.CXD;
                                case (int)GameVersion.E: return PIDType.Method_1;
                                case (int)GameVersion.FR:
                                case (int)GameVersion.LG:
                                    return PIDType.Method_1; // roamer glitch
                                default:
                                    return PIDType.Method_1;
                            }
                        case EncounterSlot _:
                            if (pk.Version == 15)
                                return PIDType.PokeSpot;
                            return pk.Species == 201 ? PIDType.Method_1_Unown : PIDType.Method_1;
                        default:
                            return PIDType.None;
                    }
                case 4:
                    switch (EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch)
                    {
                        case EncounterStatic s:
                            if (s.Location == 233 && s.Gift) // Pokewalker
                                return PIDType.Pokewalker;
                            if (s.Shiny == Shiny.Always)
                                return PIDType.ChainShiny;
                            return PIDType.Method_1;
                        case PGT _:
                            return PIDType.Method_1;
                        default:
                            return PIDType.None;
                    }
                default:
                    return PIDType.None;
            }
        }

        /// <summary>
        /// Colosseum/XD pokemon need to be fixed.
        /// </summary>
        /// <param name="pk">PKM to apply the fix to</param>
        private static void ColosseumFixes(PKM pk)
        {
            if (pk.Version != (int)GameVersion.CXD)
                return;

            // wipe all ribbons
            pk.ClearAllRibbons();

            // set national ribbon
            if (pk is IRibbonSetEvent3 c3)
                c3.RibbonNational = true;
            pk.Ball = 4;
            pk.FatefulEncounter = true;
            pk.OT_Gender = 0;
        }
    }
}
