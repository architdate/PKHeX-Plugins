using System.Windows.Forms;
using System.Reflection;
using static PKHeX.Core.LegalityCheckStrings;

using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.WinForms.Controls
{
    public partial class AutoLegalityMod : UserControl
    {
        public SaveFile SAV;

        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <param name="roughPK">rough pkm that has all the SSet values entered</param>
        /// <param name="SSet">Showdown set object</param>
        /// <param name="satisfied">If the final result is satisfactory, otherwise use current auto legality functionality</param>
        /// <returns></returns>
        public PKM APILegality(PKM roughPK, ShowdownSet SSet, out bool satisfied)
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
                if (pkmn != null)
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
                    SetNatureAbility(pk, SSet);
                    SetIVsPID(pk, SSet, Method, HPType, pkmn);
                    PrintLegality(pk);
                    ColosseumFixes(pk);
                    pk.SetSuggestedHyperTrainingData(pk.IVs); // Hypertrain
                    SetEncryptionConstant(pk);
                    CheckAndSetFateful(pk);
                    SetShinyBoolean(pk, SSet.Shiny);
                    FixGender(pk);
                    FixRibbons(pk);
                    FixMemoriesPKM(pk);
                    LegalityAnalysis la = new LegalityAnalysis(pk);
                    if (la.Valid) satisfied = true;
                    if (satisfied)
                        return pk;
                    else Console.WriteLine(la.Report());
                }
            }
            return roughPK;
        }

        /// <summary>
        /// Debugging tool
        /// </summary>
        /// <param name="pk">PKM whose legality must be printed</param>
        public void PrintLegality(PKM pk)
        {
            Console.WriteLine(new LegalityAnalysis(pk).Report());
        }

        /// <summary>
        /// Validate and Set the gender if needed
        /// </summary>
        /// <param name="pkm">PKM to modify</param>
        public void ValidateGender(PKM pkm)
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
                if (new HashSet<int>{290, 292, 412, 413, 414, 280, 475, 361, 478, 677, 678}.Contains(pkm.Species) && pkm.Gender != 2) // shedinja
                {
                    var gender = PKX.GetGenderFromPID(new LegalInfo(pkm).EncounterMatch.Species, pkm.EncryptionConstant);
                    pkm.Gender = gender;
                    genderValid = true;
                }
            }

            if (genderValid)
                return;
            else
            {
                if (pkm.Gender == 0) pkm.Gender = 1;
                else if (pkm.Gender == 1) pkm.Gender = 0;
                else pkm.GetSaneGender();
            }
        }

        /// <summary>
        /// Set Version override for GSC and RBY games
        /// </summary>
        /// <param name="pk">Return PKM</param>
        /// <param name="pkmn">Generated PKM</param>
        public void SetVersion(PKM pk, PKM pkmn)
        {
            if (pkmn.Version == (int)GameVersion.RBY) pk.Version = (int)GameVersion.RD;
            else if (pkmn.Version == (int)GameVersion.GSC) pk.Version = (int)GameVersion.C;
            else if (pkmn.Species == 658 && pkmn.AltForm == 1 && ((pkmn.Version == (int)GameVersion.UM) || (pkmn.Version == (int)GameVersion.US))) pk.Version = (int)GameVersion.SN; // Ash-Greninja
            else pk.Version = pkmn.Version;
        }

        public void CheckAndSetFateful(PKM pk)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            string Report = la.Report();
            if (Report.Contains(V322) || Report.Contains(V324)) pk.FatefulEncounter = true;
        }

        /// <summary>
        /// Fix Formes that are illegal outside of battle
        /// </summary>
        /// <param name="SSet">Original Showdown Set</param>
        /// <param name="changedSet">Edited Showdown Set</param>
        /// <returns>boolen that checks if a form is fixed or not</returns>
        public bool FixFormes(ShowdownSet SSet, out ShowdownSet changedSet)
        {
            changedSet = SSet;
            if (SSet.Form.Contains("Mega") || SSet.Form == "Primal" || SSet.Form == "Busted") {
                SSet = new ShowdownSet(SSet.Text.Replace("-" + SSet.Form, ""));
                changedSet = SSet;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set Species and Level with nickname (Helps with PreEvos)
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="SSet">SSet to modify</param>
        public void SetSpeciesLevel(PKM pk, ShowdownSet SSet, int Form)
        {
            pk.Species = SSet.Species;
            pk.SetAltForm(Form);
            pk.IsNicknamed = (SSet.Nickname != null);
            pk.Nickname = SSet.Nickname != null ? SSet.Nickname : PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, SAV.Generation);
            pk.CurrentLevel = SSet.Level;
            if (pk.CurrentLevel == 50) pk.CurrentLevel = 100; // VGC Override
        }

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="SSet">Showdown Set to refer</param>
        public void SetMovesEVsItems(PKM pk, ShowdownSet SSet)
        {
            pk.SetMoves(SSet.Moves, true);
            pk.EVs = SSet.EVs;
            pk.CurrentFriendship = SSet.Friendship;
            pk.ApplyHeldItem(SSet.HeldItem, SSet.Format);
            var legal = new LegalityAnalysis(pk);
            if (legal.Parsed && CheckInvalidRelearn(legal.Info.Relearn) && !pk.WasEvent)
                pk.RelearnMoves = pk.GetSuggestedRelearnMoves(legal);
        }

        /// <summary>
        /// Check for invalid relearn moves
        /// </summary>
        /// <param name="RelearnInfo">CheckResult List of relearn moves</param>
        /// <returns>If an invalid relearn move exists</returns>
        public bool CheckInvalidRelearn(CheckResult[] RelearnInfo)
        {
            foreach(CheckResult r in RelearnInfo)
            {
                if (!r.Valid) return false;
            }
            return true;
        }

        /// <summary>
        /// Set Trainer data (TID, SID, OT) for a given PKM
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        public void SetTrainerDataAndMemories(PKM pk)
        {
            if (!(pk.WasEvent || pk.WasIngameTrade))
            {
                // Hardcoded a generic one for now, trainerdata.json implementation here later
                pk.CurrentHandler = 1;
                pk.HT_Name = "ARCH";
                pk.HT_Gender = 0; // Male for Colo/XD Cases
                pk.TID = 34567;
                pk.SID = 0;
                pk.OT_Name = "TCD";
                pk = FixMemoriesPKM(pk);
            }
        }

        /// <summary>
        /// Memory fix if needed
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <returns>Modified PKM file</returns>
        private PKM FixMemoriesPKM(PKM pk)
        {
            if (SAV.PKMType == typeof(PK7))
            {
                ((PK7)pk).FixMemories();
                return pk;
            }
            else if (SAV.PKMType == typeof(PK6))
            {
                ((PK6)pk).FixMemories();
                return pk;
            }
            return pk;
        }

        /// <summary>
        /// Set Nature and Ability of the pokemon
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="SSet">Showdown Set to refer</param>
        public void SetNatureAbility(PKM pk, ShowdownSet SSet)
        {
            // Values that are must for showdown set to work, IVs should be adjusted to account for this
            pk.Nature = SSet.Nature;
            pk.SetAbility(SSet.Ability);
        }

        /// <summary>
        /// Sets shiny value to whatever boolean is specified
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="isShiny">Shiny value that needs to be set</param>
        public void SetShinyBoolean(PKM pk, bool isShiny)
        {
            if (!isShiny)
            {
                pk.SetUnshiny();
            }
            if (isShiny)
            {
                if (pk.GenNumber > 5) pk.SetShinyPID();
                else if (pk.VC) pk.SetShinyIVs();
                else pk.SetShinySID();
            }
        }

        /// <summary>
        /// Set IV Values for the pokemon
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="SSet"></param>
        public void SetIVsPID(PKM pk, ShowdownSet SSet, PIDType Method, int HPType, PKM originalPKMN)
        {
            // Useful Values for computation
            int Species = pk.Species;
            int Nature = pk.Nature;
            int Gender = pk.Gender;
            int AbilityNumber = pk.AbilityNumber; // 1,2,4 (HA)
            int Ability = pk.Ability;

            if (pk.GenNumber > 4 || pk.VC)
            {
                pk.IVs = SSet.IVs;
                if (Species == 658 && pk.AltForm == 1)
                    pk.IVs = new int[] { 20, 31, 20, 31, 31, 20 };
                pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, (uint)(AbilityNumber * 0x10001));
            }
            else
            {
                FindPIDIV(pk, Method, HPType, originalPKMN);
                ValidateGender(pk);
            }
        }

        /// <summary>
        /// Method to set PID, IV while validating nature.
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="Method">Given Method</param>
        /// <param name="HPType">HPType INT for preserving Hidden powers</param>
        public void FindPIDIV(PKM pk, PIDType Method, int HPType, PKM originalPKMN)
        {
            if (Method == PIDType.None)
            {
                Method = FindLikelyPIDType(pk, originalPKMN);
                if (pk.Version == 15) Method = PIDType.CXD;
                if (Method == PIDType.None) Method = PIDType.Method_2;
            }
            PKM iterPKM = pk;
            while (true)
            {
                uint seed = Util.Rand32();
                PIDGenerator.SetValuesFromSeed(pk, Method, seed);
                if (!(pk.Ability == iterPKM.Ability && pk.AbilityNumber == iterPKM.AbilityNumber && pk.Nature == iterPKM.Nature)) continue;
                if (pk.PID % 25 == iterPKM.Nature && pk.HPType == HPType) // Util.Rand32 is the way to go
                    break;
                pk = iterPKM;
            }
        }

        /// <summary>
        /// Secondary fallback if PIDType.None to slot the PKM into its most likely type
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="pkmn">Original PKM</param>
        /// <returns>PIDType that is likely used</returns>
        public PIDType FindLikelyPIDType(PKM pk, PKM pkmn)
        {
            Blah b = new Blah();
            if (b.usesEventBasedMethod(pk.Species, pk.Moves, "BACD_R"))
                return PIDType.BACD_R;
            if (b.usesEventBasedMethod(pk.Species, pk.Moves, "M2")) return PIDType.Method_2;
            if (pk.Species == 490 && pk.Gen4)
            {
                pk.Egg_Location = 2002;
                return PIDType.Method_1;
            }
            switch (pk.GenNumber)
            {
                case 3:
                    switch (pk.Version)
                    {
                        case (int)GameVersion.CXD: return PIDType.CXD;
                        case (int)GameVersion.E: return PIDType.Method_1;
                        case (int)GameVersion.FR:
                        case (int)GameVersion.LG:
                            return PIDType.Method_1;
                        default:
                            return PIDType.Method_1;
                    }
                case 4:
                    switch(new LegalInfo(pk).EncounterMatch)
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
        public void FixGender(PKM pk)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            string Report = la.Report();
            if (Report.Contains(V255))
            {
                if (pk.Gender == 0) pk.Gender = 1;
                else pk.Gender = 0;
            }
            if (pk.Gender != 0 && pk.Gender != 1) pk.Gender = pk.GetSaneGender();
        }

        /// <summary>
        /// Set Encryption Constant based on PKM GenNumber
        /// </summary>
        /// <param name="pk"></param>
        public void SetEncryptionConstant(PKM pk)
        {
            if (pk.GenNumber > 5 || pk.VC)
            {
                int wIndex = Array.IndexOf(Legal.WurmpleEvolutions, pk.Species);
                uint EC = wIndex < 0 ? Util.Rand32() : PKX.GetWurmpleEC(wIndex / 2);
                if (!(pk.Species == 658 && pk.AltForm == 1)) pk.EncryptionConstant = EC;
            }
            else pk.EncryptionConstant = pk.PID; // Generations 3 to 5
        }

        /// <summary>
        /// Colosseum/XD pokemon need to be fixed. Fix Gender further in logic using <see cref="FixGender(PKM)"/>
        /// </summary>
        /// <param name="pkm">PKM to apply the fix to</param>
        public void ColosseumFixes(PKM pkm)
        {
            if(pkm.Version == 15)
            {
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pkm.GetType(), "Ribbon").Distinct();
                foreach (var RibbonName in RibbonNames)
                {
                    ReflectUtil.SetValue(pkm, RibbonName, 0);
                }
                ReflectUtil.SetValue(pkm, "RibbonNational", -1);
                pkm.Ball = 4;
                pkm.FatefulEncounter = true;
                pkm.OT_Gender = 0;
            }
        }

        /// <summary>
        /// Fix invalid and missing ribbons. (V600 and V601)
        /// </summary>
        /// <param name="pk">PKM whose ribbons need to be fixed</param>
        public void FixRibbons(PKM pk)
        {
            string Report = new LegalityAnalysis(pk).Report();
            if (Report.Contains(String.Format(V600, "")))
            {
                string[] ribbonList = Report.Split(new string[] { String.Format(V600, "") }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None);
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();
                List<string> missingRibbons = new List<string>();
                foreach (var RibbonName in RibbonNames)
                {
                    string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                    if (ribbonList.Contains(v)) missingRibbons.Add(RibbonName);
                }
                foreach (string missing in missingRibbons)
                {
                    ReflectUtil.SetValue(pk, missing, -1);
                }
            }
            if (Report.Contains(String.Format(V601, "")))
            {
                string[] ribbonList = Report.Split(new string[] { String.Format(V601, "") }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None);
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();
                List<string> invalidRibbons = new List<string>();
                foreach (var RibbonName in RibbonNames)
                {
                    string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                    if (ribbonList.Contains(v)) invalidRibbons.Add(RibbonName);
                }
                foreach(string invalid in invalidRibbons)
                {
                    ReflectUtil.SetValue(pk, invalid, 0);
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
            int num = 0;
            GameVersion[] Versions = ((GameVersion[])Enum.GetValues(typeof(GameVersion))).Where(z => z < GameVersion.RB && z > 0).OrderBy(x => x.GetGeneration()).Reverse().ToArray();
            pk.TID = info.TID;
            var m = moves ?? pk.Moves;
            var vers = versions?.Length >= 1 ? versions : Versions.Where(z => z <= (GameVersion)pk.MaxGameID);
            foreach (var ver in vers)
            {
                var encs = EncounterMovesetGenerator.GenerateVersionEncounters(pk, m, ver);
                foreach (var enc in encs)
                {
                    num++;
                    Console.WriteLine(num);
                    var result = enc.ConvertToPKM(info);
                    yield return result;
                }
            }
        }
    }
}