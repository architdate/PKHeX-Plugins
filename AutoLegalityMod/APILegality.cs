using static PKHeX.Core.LegalityCheckStrings;

using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoLegalityMod
{
    public partial class AutoLegalityMod
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
            bool changedForm = false;
            if(SSet.Form != null) changedForm = FixFormes(SSet, out SSet);
            satisfied = false; // true when all features of the PKM are satisfied
            int Form = roughPK.AltForm;
            if (changedForm)
            {
                Form = SSet.FormIndex;
                roughPK.ApplySetDetails(SSet);
            }
            int HPType = roughPK.HPType;

            // List of candidate PKM files

            int[] moves = SSet.Moves;
            var f = GeneratePKMs(roughPK, SAV, moves);
            foreach (PKM pkmn in f)
            {
                if (pkmn == null)
                    continue;
                try
                {
                    PKM pk = PKMConverter.ConvertToType(pkmn, SAV.PKMType, out _); // All Possible PKM files
                    LegalInfo info = new LegalInfo(pk);
                    var pidiv = info.PIDIV ?? MethodFinder.Analyze(pk);
                    PIDType Method = PIDType.None;
                    if (pidiv != null) Method = pidiv.Type;
                    SetVersion(pk, pkmn); // PreEmptive Version setting
                    SetSpeciesLevel(pk, SSet, Form);
                    SetMovesEVsItems(pk, SSet);
                    SetTrainerDataAndMemories(pk);
                    pk.SetNatureAbility(SSet);
                    SetIVsPID(pk, SSet, Method, HPType, pkmn);
                    PrintLegality(pk);
                    ColosseumFixes(pk);
                    pk.SetSuggestedHyperTrainingData(pk.IVs); // Hypertrain
                    SetEncryptionConstant(pk);
                    pk.SetShinyBoolean(SSet.Shiny);
                    CheckAndSetFateful(pk);
                    FixGender(pk, SSet);
                    FixRibbons(pk);
                    pk.FixMemoriesPKM();
                    pk.SetSpeciesBall();
                    pk.SetHappiness();
                    pk.SetBelugaValues();
                    LegalityAnalysis la = new LegalityAnalysis(pk);
                    if (la.Valid)
                        return pk;
                    Console.WriteLine(la.Report());
                    satisfied = true;
                    return pk;
                }
                catch
                {
                }
            }
            return roughPK;
        }

        /// <summary>
        /// Debugging tool
        /// </summary>
        /// <param name="pk">PKM whose legality must be printed</param>
        public static void PrintLegality(PKM pk)
        {
            Console.WriteLine(new LegalityAnalysis(pk).Report());
        }

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

            SSet = new ShowdownSet(SSet.Text.Replace("-" + SSet.Form, ""));
            changedSet = SSet;
            return true;
        }

        /// <summary>
        /// Set Species and Level with nickname (Helps with PreEvos)
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="SSet">SSet to modify</param>
        /// <param name="Form">Form to modify</param>
        public static void SetSpeciesLevel(PKM pk, ShowdownSet SSet, int Form)
        {
            pk.Species = SSet.Species;
            if (SSet.Gender != null) pk.Gender = (SSet.Gender == "M") ? 0 : 1;
            else pk.Gender = pk.GetSaneGender();
            pk.SetAltForm(Form);
            pk.IsNicknamed = (SSet.Nickname != null);
            pk.Nickname = SSet.Nickname ?? PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, SAV.Generation);
            pk.CurrentLevel = SSet.Level;
            if (pk.CurrentLevel == 50) pk.CurrentLevel = 100; // VGC Override
        }

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="SSet">Showdown Set to refer</param>
        public static void SetMovesEVsItems(PKM pk, ShowdownSet SSet)
        {
            pk.SetMoves(SSet.Moves, true);
            pk.CurrentFriendship = SSet.Friendship;
            pk.SetBelugaValues();
            if (pk is IAwakened pb7)
            {
                pb7.SetSuggestedAwakenedValues(pk);
            }
            else
            {
                pk.EVs = SSet.EVs;
                pk.ApplyHeldItem(SSet.HeldItem, SSet.Format);
                var legal = new LegalityAnalysis(pk);
                if (legal.Parsed && !pk.WasEvent)
                    pk.RelearnMoves = pk.GetSuggestedRelearnMoves(legal);
            }
        }

        /// <summary>
        /// Check for invalid relearn moves
        /// </summary>
        /// <param name="RelearnInfo">CheckResult List of relearn moves</param>
        /// <returns>If an invalid relearn move exists</returns>
        public static bool CheckInvalidRelearn(CheckResult[] RelearnInfo) => RelearnInfo.All(r => r.Valid);

        /// <summary>
        /// Set Trainer data (TID, SID, OT) for a given PKM
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        public static void SetTrainerDataAndMemories(PKM pk)
        {
            if (pk.WasEvent || pk.WasIngameTrade)
                return;

            // Hardcoded a generic one for now, trainerdata.json implementation here later
            pk.CurrentHandler = 1;
            pk.HT_Name = "ARCH";
            pk.HT_Gender = 0; // Male for Colo/XD Cases
            pk.TID = 34567;
            pk.SID = 0;
            pk.OT_Name = "TCD";
            pk.FixMemoriesPKM();
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
                if(Method != PIDType.G5MGShiny) pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, (uint)(AbilityNumber * 0x10001));
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
        /// Function that will instantly return a PKM at any point in the main function. Usage is as follows: pk = DebugReturn(pk, out satisfied, [optionalgameint]); if (satisfied) return pk;
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="satisfied"></param>
        /// <param name="OptionalGame"></param>
        /// <returns></returns>
        public static PKM DebugReturn(PKM pk, out bool satisfied, int OptionalGame = -1)
        {
            satisfied = false;
            if (OptionalGame > 0)
            {
                if (pk.Version == OptionalGame)
                    satisfied = true;
                return pk;
            }
            satisfied = true;
            return pk;
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
        /// Quick Gender Toggle
        /// </summary>
        /// <param name="pk">PKM whose gender needs to be toggled</param>
        /// <param name="SSet">Showdown Set for Gender reference</param>
        public static void FixGender(PKM pk, ShowdownSet SSet)
        {
            pk.SetGender(SSet.Gender);
            var la = new LegalityAnalysis(pk);
            string Report = la.Report();

            if (Report.Contains(LMemoryFeelInvalid))
                pk.Gender = pk.Gender == 0 ? 1 : 0;

            if (pk.Gender != 0 && pk.Gender != 1)
                pk.Gender = pk.GetSaneGender();
        }

        /// <summary>
        /// Set Encryption Constant based on PKM GenNumber
        /// </summary>
        /// <param name="pk"></param>
        public static void SetEncryptionConstant(PKM pk)
        {
            if (pk.GenNumber > 5 || pk.VC)
            {
                int wIndex = Array.IndexOf(Legal.WurmpleEvolutions, pk.Species);
                uint EC = wIndex < 0 ? Util.Rand32() : PKX.GetWurmpleEC(wIndex / 2);
                if (!(pk.Species == 658 && pk.AltForm == 1)) pk.EncryptionConstant = EC;
            }
            else
            {
                pk.EncryptionConstant = pk.PID; // Generations 3 to 5
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
        /// Fix invalid and missing ribbons. (V600 and V601)
        /// </summary>
        /// <param name="pk">PKM whose ribbons need to be fixed</param>
        public static void FixRibbons(PKM pk)
        {
            string Report = new LegalityAnalysis(pk).Report();
            if (Report.Contains(string.Format(LRibbonFMissing_0, "")))
            {
                string[] ribbonList = Report.Split(new[] { string.Format(LRibbonFMissing_0, "") }, StringSplitOptions.None)[1].Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(new[] { ", " }, StringSplitOptions.None);
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();
                List<string> missingRibbons = new List<string>();
                foreach (var RibbonName in RibbonNames)
                {
                    string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                    if (ribbonList.Contains(v)) missingRibbons.Add(RibbonName);
                }
                foreach (string missing in missingRibbons)
                {
                    if (missing == nameof(PK6.RibbonCountMemoryBattle) || missing == nameof(PK6.RibbonCountMemoryContest)) ReflectUtil.SetValue(pk, missing, 0);
                    else ReflectUtil.SetValue(pk, missing, true);
                }
            }
            if (Report.Contains(string.Format(LRibbonFInvalid_0, "")))
            {
                string[] ribbonList = Report.Split(new[] { string.Format(LRibbonFInvalid_0, "") }, StringSplitOptions.None)[1].Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(new[] { ", " }, StringSplitOptions.None);
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();

                var invalidRibbons = new List<string>();
                foreach (var RibbonName in RibbonNames)
                {
                    string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                    if (ribbonList.Contains(v))
                        invalidRibbons.Add(RibbonName);
                }
                foreach(string invalid in invalidRibbons)
                {
                    if (invalid == nameof(PK6.RibbonCountMemoryBattle) || invalid == nameof(PK6.RibbonCountMemoryContest))
                        ReflectUtil.SetValue(pk, invalid, 0);
                    else
                        ReflectUtil.SetValue(pk, invalid, false);
                }
            }
        }

        /// <summary>
        /// Temporary Reimplementation of Kaphotics's Generator without the exception being thrown to avoid relying on the bruteforce mechanism
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="info">Trainer Info for TID</param>
        /// <param name="moves">INT list of moves for the pkm</param>
        /// <param name="versions">Versions to iterate over (All in our case)</param>
        /// <returns></returns>
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
