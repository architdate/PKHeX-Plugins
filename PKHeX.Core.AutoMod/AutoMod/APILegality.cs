using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static PKHeX.Core.LegalityCheckStrings;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="ShowdownSet"/>.
    /// </summary>
    public static class APILegality
    {
        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <remarks>Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="ShowdownSet"/>.</remarks>
        /// <param name="dest">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is satisfactory, otherwise use current auto legality functionality</param>
        public static PKM GetLegalFromTemplate(this ITrainerInfo dest, PKM template, ShowdownSet set, out bool satisfied)
        {
            var Form = SanityCheckForm(template, ref set);
            template.ApplySetDetails(set);
            var destType = template.GetType();

            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves);
            foreach (var enc in encounters)
            {
                var ver = enc is IVersion v ? v.Version : (GameVersion)dest.Game;
                var tr = TrainerSettings.GetSavedTrainerData(ver);
                var raw = enc.ConvertToPKM(tr);
                var pk = PKMConverter.ConvertToType(raw, destType, out _);
                ApplySetDetails(pk, set, Form, raw, dest);

                var la = new LegalityAnalysis(pk);
                if (la.Valid)
                {
                    satisfied = true;
                    return pk;
                }
                Debug.WriteLine(la.Report());
            }
            satisfied = false;
            return template;
        }

        private static int SanityCheckForm(PKM template, ref ShowdownSet set)
        {
            int Form = template.AltForm;
            if (set.Form != null && FixFormes(set, out set))
                Form = set.FormIndex;
            return Form;
        }

        /// <summary>
        /// Modifies the provided <see cref="pk"/> to the specifications required by <see cref="set"/>.
        /// </summary>
        /// <param name="pk">Converted final pkm to apply details to</param>
        /// <param name="set">Set details required</param>
        /// <param name="Form">Alternate form required</param>
        /// <param name="HPType">Hidden Power type requirement</param>
        /// <param name="unconverted">Original pkm data</param>
        private static void ApplySetDetails(PKM pk, ShowdownSet set, int Form, PKM unconverted, ITrainerInfo handler)
        {
            var pidiv = MethodFinder.Analyze(pk);

            pk.SetVersion(unconverted); // PreEmptive Version setting
            pk.SetSpeciesLevel(set, Form);
            pk.SetMovesEVsItems(set);
            pk.SetHandlerandMemory(handler);
            pk.SetNatureAbility(set);
            pk.SetIVsPID(set, pidiv.Type, set.HiddenPowerType, unconverted);

            Debug.WriteLine(new LegalityAnalysis(pk).Report());

            pk.SetSuggestedHyperTrainingData(pk.IVs); // Hypertrain
            pk.SetEncryptionConstant();
            pk.SetShinyBoolean(set.Shiny);
            pk.FixGender(set);
            pk.SetSuggestedRibbons();
            pk.SetSuggestedMemories();
            pk.SetSuggestedBall();
            pk.SetHappiness();
            pk.SetBelugaValues();
        }

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
        private static void SetVersion(this PKM pk, PKM original)
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
        private static void SetIVsPID(this PKM pk, ShowdownSet set, PIDType method, int hpType, PKM original)
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
                if (method != PIDType.G5MGShiny)
                    pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, (uint)(AbilityNumber * 0x10001));
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
                if (pk.Version == 15)
                    Method = PIDType.CXD;
                if (Method == PIDType.None)
                    pk.SetPIDGender(pk.Gender);
            }
            var iterPKM = pk.Clone();
            while (true)
            {
                uint seed = Util.Rand32();
                PIDGenerator.SetValuesFromSeed(pk, Method, seed);
                if (!(pk.Ability == iterPKM.Ability && pk.AbilityNumber == iterPKM.AbilityNumber && pk.Nature == iterPKM.Nature))
                    continue;
                if (HPType >= 0 && pk.HPType != HPType)
                    continue;
                if (pk.PID % 25 != iterPKM.Nature) // Util.Rand32 is the way to go
                    continue;
                break;
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
    }
}
