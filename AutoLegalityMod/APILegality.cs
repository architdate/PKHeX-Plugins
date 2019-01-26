using System;
using System.Collections.Generic;
using System.Linq;

using PKHeX.Core;
using static PKHeX.Core.LegalityCheckStrings;

namespace AutoLegalityMod
{
    public static class API
    {
        public static SaveFile SAV;

        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <param name="roughPK">rough pkm that has all the SSet values entered</param>
        /// <param name="SSet">Showdown set object</param>
        /// <param name="satisfied">If the final result is satisfactory, otherwise use current auto legality functionality</param>
        public static PKM APILegality(PKM roughPK, ShowdownSet SSet, out bool satisfied)
        {
            int Form = roughPK.AltForm;
            if (SSet.Form != null && FixFormes(SSet, out SSet))
            {
                Form = SSet.FormIndex;
                roughPK.ApplySetDetails(SSet);
            }
            int HPType = roughPK.HPType;

            var destType = SAV.PKMType;

            // List of candidate PKM files
            var f = GeneratePKMs(roughPK, SAV, SSet.Moves);
            foreach (PKM pkmn in f)
            {
                var pk = PKMConverter.ConvertToType(pkmn, destType, out _); // All Possible PKM files

                var info = new LegalInfo(pk);
                var pidiv = info.PIDIV ?? MethodFinder.Analyze(pk);
                var Method = pidiv?.Type ?? PIDType.None;

                SetVersion(pk, pkmn); // PreEmptive Version setting
                pk.SetSpeciesLevel(SSet, Form);
                pk.SetMovesEVsItems(SSet);
                pk.SetTrainerDataAndMemories();
                pk.SetNatureAbility(SSet);
                SetIVsPID(pk, SSet, Method, HPType, pkmn);

                PrintLegality(pk);

                ColosseumFixes(pk);
                pk.SetSuggestedHyperTrainingData(pk.IVs); // Hypertrain
                pk.SetEncryptionConstant();
                pk.SetShinyBoolean(SSet.Shiny);
                CheckAndSetFateful(pk);
                pk.FixGender(SSet);
                pk.FixRibbons();
                pk.FixMemoriesPKM();
                pk.SetSpeciesBall();
                pk.SetHappiness();
                pk.SetBelugaValues();

                satisfied = true;
                var la = new LegalityAnalysis(pk);
                if (la.Valid)
                    return pk;

                Console.WriteLine(la.Report());
                return pk;
            }
            satisfied = false;
            return roughPK;
        }

        /// <summary>
        /// Debugging tool
        /// </summary>
        /// <param name="pk">PKM whose legality must be printed</param>
        public static void PrintLegality(PKM pk) => Console.WriteLine(new LegalityAnalysis(pk).Report());

        /// <summary>
        /// Validate and Set the gender if needed
        /// </summary>
        /// <param name="pkm">PKM to modify</param>
        public static void ValidateGender(PKM pkm)
        {
            bool genderValid = pkm.IsGenderValid();
            if (!genderValid)
            {
                if (pkm.Format == 4 && pkm.Species == 292) // Shedinja glitch
                {
                    // should match original gender
                    var gender = PKX.GetGenderFromPIDAndRatio(pkm.PID, 0x7F); // 50-50
                    if (gender == pkm.Gender)
                        genderValid = true;
                }
                else if (pkm.Format > 5 && (pkm.Species == 183 || pkm.Species == 184))
                {
                    var gv = pkm.PID & 0xFF;
                    if (gv > 63 && pkm.Gender == 1) // evolved from azurill after transferring to keep gender
                        genderValid = true;
                }
            }
            else
            {
                // check for mixed->fixed gender incompatibility by checking the gender of the original species
                if (BruteTables.FixedGenderFromBiGender.Contains(pkm.Species) && pkm.Gender != 2) // shedinja
                {
                    var gender = PKX.GetGenderFromPID(new LegalInfo(pkm).EncounterMatch.Species, pkm.EncryptionConstant);
                    pkm.Gender = gender;
                    // genderValid = true; already true if we reach here
                }
            }

            if (genderValid)
                return;

            switch (pkm.Gender)
            {
                case 0: pkm.Gender = 1; break;
                case 1: pkm.Gender = 0; break;
                default: pkm.GetSaneGender(); break;
            }
        }

        /// <summary>
        /// Set Version override for GSC and RBY games
        /// </summary>
        /// <param name="pk">Return PKM</param>
        /// <param name="pkmn">Generated PKM</param>
        public static void SetVersion(PKM pk, PKM pkmn)
        {
            switch (pkmn.Version)
            {
                case (int)GameVersion.RBY:
                    pk.Version = (int)GameVersion.RD;
                    break;
                case (int)GameVersion.GSC:
                    pk.Version = (int)GameVersion.C;
                    break;
                case (int)GameVersion.UM:
                case (int)GameVersion.US:
                    if (pkmn.Species == 658 && pkmn.AltForm == 1)
                        pk.Version = (int)GameVersion.SN; // Ash-Greninja
                    else
                        pk.Version = pkmn.Version;
                    break;
                default:
                    pk.Version = pkmn.Version;
                    break;
            }
        }

        public static void CheckAndSetFateful(PKM pk)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            string Report = la.Report();
            if (Report.Contains(LFatefulMysteryMissing) || Report.Contains(LFatefulMissing))
                pk.FatefulEncounter = true;
            else if (Report.Contains(LFatefulInvalid))
                pk.FatefulEncounter = false;
        }

        /// <summary>
        /// Fix Formes that are illegal outside of battle
        /// </summary>
        /// <param name="SSet">Original Showdown Set</param>
        /// <param name="changedSet">Edited Showdown Set</param>
        /// <returns>boolen that checks if a form is fixed or not</returns>
        public static bool FixFormes(ShowdownSet SSet, out ShowdownSet changedSet)
        {
            changedSet = SSet;
            var badForm = SSet.Form.Contains("Mega") || SSet.Form == "Primal" || SSet.Form == "Busted";
            if (!badForm)
                return false;

            changedSet = new ShowdownSet(SSet.Text.Replace("-" + SSet.Form, ""));
            return true;
        }

        /// <summary>
        /// Set IV Values for the pokemon
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="SSet"></param>
        /// <param name="Method"></param>
        /// <param name="HPType"></param>
        /// <param name="originalPKMN"></param>
        public static void SetIVsPID(PKM pk, ShowdownSet SSet, PIDType Method, int HPType, PKM originalPKMN)
        {
            // Useful Values for computation
            int Species = pk.Species;
            int Nature = pk.Nature;
            int Gender = pk.Gender;
            int AbilityNumber = pk.AbilityNumber; // 1,2,4 (HA)

            // Find the encounter
            LegalInfo li = EncounterFinder.FindVerifiedEncounter(originalPKMN);
            // TODO: Something about the gen 5 events. Maybe check for nature and shiny val and not touch the PID in that case?
            // Also need to figure out hidden power handling in that case.. for PIDType 0 that may isn't even be possible.

            if (pk.GenNumber > 4 || pk.VC)
            {
                pk.IVs = SSet.IVs;
                if (Species == 658 && pk.AltForm == 1)
                    pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
                if (Method != PIDType.G5MGShiny) pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, (uint)(AbilityNumber * 0x10001));
            }
            else
            {
                pk.IVs = SSet.IVs;
                if (li.EncounterMatch is PCD)
                    return;
                FindPIDIV(pk, Method, HPType);
                ValidateGender(pk);
            }
        }

        /// <summary>
        /// Method to set PID, IV while validating nature.
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="Method">Given Method</param>
        /// <param name="HPType">HPType INT for preserving Hidden powers</param>
        public static void FindPIDIV(PKM pk, PIDType Method, int HPType)
        {
            if (Method == PIDType.None)
            {
                Method = FindLikelyPIDType(pk);
                if (pk.Version == 15) Method = PIDType.CXD;
                if (Method == PIDType.None) pk.SetPIDGender(pk.Gender);
            }
            PKM iterPKM = pk;
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
        public static PIDType FindLikelyPIDType(PKM pk)
        {
            BruteForce b = new BruteForce();
            if (b.UsesEventBasedMethod(pk.Species, pk.Moves, "BACD_R"))
                return PIDType.BACD_R;
            if (b.UsesEventBasedMethod(pk.Species, pk.Moves, "M2")) return PIDType.Method_2;
            if (pk.Species == 490 && pk.Gen4)
            {
                pk.Egg_Location = 2002;
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
        /// Colosseum/XD pokemon need to be fixed. Fix Gender further in logic using <see cref="FixGender"/>
        /// </summary>
        /// <param name="pkm">PKM to apply the fix to</param>
        public static void ColosseumFixes(PKM pkm)
        {
            if (pkm.Version == (int)GameVersion.CXD)
            {
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pkm.GetType(), "Ribbon").Distinct();
                foreach (var RibbonName in RibbonNames)
                {
                    if (RibbonName == "RibbonCountMemoryBattle" || RibbonName == "RibbonCountMemoryContest") ReflectUtil.SetValue(pkm, RibbonName, 0);
                    else ReflectUtil.SetValue(pkm, RibbonName, false);
                }
                ReflectUtil.SetValue(pkm, "RibbonNational", true);
                pkm.Ball = 4;
                pkm.FatefulEncounter = true;
                pkm.OT_Gender = 0;
            }
        }

        /// <summary>
        /// Temporary Reimplementation of Kaphotics's Generator without the exception being thrown to avoid relying on the bruteforce mechanism
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="info">Trainer Info for TID</param>
        /// <param name="moves">INT list of moves for the pkm</param>
        /// <param name="versions">Versions to iterate over (All in our case)</param>
        public static IEnumerable<PKM> GeneratePKMs(PKM pk, ITrainerInfo info, int[] moves = null, params GameVersion[] versions)
        {
            GameVersion[] Versions = ((GameVersion[])Enum.GetValues(typeof(GameVersion))).Where(z => z < GameVersion.RB && z > 0).OrderBy(x => x.GetGeneration()).Reverse().ToArray();
            pk.TID = info.TID;
            var m = moves ?? pk.Moves;
            var vers = versions?.Length >= 1 ? versions : Versions.Where(z => z <= (GameVersion)pk.MaxGameID);
            if (pk.Gen3) vers = vers.Concat(new[] { (GameVersion)15 });
            foreach (var ver in vers)
            {
                var encs = EncounterMovesetGenerator.GenerateVersionEncounters(pk, m, ver);
                foreach (var enc in encs)
                {
                    var result = enc.ConvertToPKM(info);
                    if (result.Version != (int)ver) continue;
                    yield return result;
                }
            }
        }
    }
}
