using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using PKHeX.Core;
using static PKHeX.Core.LegalityCheckStrings;
using System.IO;

namespace AutoLegalityMod
{
    public partial class BruteForce
    {
        PKM backup;
        bool requestedShiny = false;
        public event EventHandler LegalityChanged;
        public SaveFile SAV;
        bool legalized = false;

        /// <summary>
        /// Try to generate every a legal PKM from a showdown set using bruteforce. This should generally never be needed.
        /// </summary>
        /// <param name="Set">Rough PKM Set</param>
        /// <param name="SSet">Showdown Set</param>
        /// <param name="resetForm">boolean to reset form back to base form</param>
        /// <param name="TID">optional TID</param>
        /// <param name="SID">optional SID</param>
        /// <param name="OT">optional OT Name</param>
        /// <param name="gender">optional Gender</param>
        /// <returns>PKM legalized via bruteforce</returns>
        public PKM LoadShowdownSetModded_PKSM(PKM Set, ShowdownSet SSet, bool resetForm = false, int TID = -1, int SID = -1, string OT = "", int gender = 0)
        {
            backup = Set;
            bool trainerinfo = TID > 0;
            List<List<string>> evoChart = generateEvoLists2();
            int abilitynum = Set.AbilityNumber < 6 ? Set.AbilityNumber >> 1 : 0;
            if (resetForm)
            {
                Set.AltForm = 0;
                Set.RefreshAbility(Set.AbilityNumber < 6 ? Set.AbilityNumber >> 1 : 0);
            }
            if (Set.Species == 774 && Set.AltForm == 0) Set.AltForm = 7; // Minior has to be C-Red and not M-Red outside of battle
            bool shiny = Set.IsShiny;
            requestedShiny = SSet.Shiny;
            bool legendary = false;
            bool eventMon = false;
            int[] legendaryList = new int[] { 144, 145, 146, 150, 151, 243, 244, 245, 249, 250, 251, 377, 378, 379, 380, 381, 382, 383, 384, 385,
                                              386, 480, 481, 482, 483, 484, 485, 486, 487, 488, 491, 492, 493, 494, 638, 639, 640, 642, 641, 643,
                                              644, 645, 646, 647, 648, 649, 716, 717, 718, 719, 720, 721, 785, 786, 787, 788, 789, 790, 791, 792,
                                              793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807, 132 };

            int[] eventList = new int[] { 719, 649, 720, 385, 647, 648, 721, 801, 802, 807 };

            int[] GameVersionList = new int[] { (int)GameVersion.UM, (int)GameVersion.US, (int)GameVersion.MN, (int)GameVersion.SN, (int)GameVersion.AS,
                                                (int)GameVersion.OR, (int)GameVersion.X, (int)GameVersion.Y, (int)GameVersion.B, (int)GameVersion.B2,
                                                (int)GameVersion.W, (int)GameVersion.W2, (int)GameVersion.D, (int)GameVersion.P, (int)GameVersion.Pt,
                                                (int)GameVersion.HG, (int)GameVersion.SS, (int)GameVersion.R, (int)GameVersion.S, (int)GameVersion.E,
                                                (int)GameVersion.FR, (int)GameVersion.LG, (int)GameVersion.CXD, (int)GameVersion.RD, (int)GameVersion.GN,
                                                (int)GameVersion.BU, (int)GameVersion.YW, (int)GameVersion.GD, (int)GameVersion.SV, (int)GameVersion.C };

            foreach (int mon in legendaryList)
            {
                if (Set.Species == mon)
                {
                    legendary = true;
                }
            }

            foreach (int mon in eventList)
            {
                if (Set.Species == mon)
                {
                    eventMon = true;
                }
            }

            // Egg based pokemon
            if (!legendary && !eventMon)
            {
                for (int i = 0; i < GameVersionList.Length; i++)
                {
                    if (Set.DebutGeneration > ((GameVersion)GameVersionList[i]).GetGeneration()) continue;
                    Set.Version = GameVersionList[i];
                    RestoreIVs(Set, SSet); // Restore IVs to SSet and HT to false
                    Set.Language = 2;
                    if (trainerinfo)
                    {
                        Set.OT_Name = OT;
                        Set.TID = TID;
                        Set.SID = SID;
                        Set.OT_Gender = gender;
                    }
                    else
                    {
                        Set.OT_Name = "Archit (TCD)";
                        Set.TID = 24521;
                        Set.SID = 42312;
                    }
                    Set.MetDate = DateTime.Today;
                    if (Set.Version == (int)GameVersion.RD || Set.Version == (int)GameVersion.BU || Set.Version == (int)GameVersion.YW || Set.Version == (int)GameVersion.GN) Set.SID = 0;
                    Set.EggMetDate = DateTime.Today;
                    if (Set.Version < (int)GameVersion.W) Set.Egg_Location = 2002;
                    else Set.Egg_Location = 60002;
                    if (Set.Version == (int)GameVersion.D || Set.Version == (int)GameVersion.P || Set.Version == (int)GameVersion.Pt) Set.Egg_Location = 2002;
                    Set.Met_Level = 1;
                    Set.ConsoleRegion = 2;
                    if (Set.Version == (int)GameVersion.RD || Set.Version == (int)GameVersion.BU || Set.Version == (int)GameVersion.YW || Set.Version == (int)GameVersion.GN)
                    {
                        Set.Met_Location = 30013;
                        Set.Met_Level = 100;
                    }
                    if (Set.Version == (int)GameVersion.CXD)
                    {
                        Set.Met_Location = 30001;
                        Set.Met_Level = 100;
                    }
                    else { Set = clickMetLocationModPKSM(Set); }
                    if (Set.GenNumber > 4) Set.Met_Level = 1;
                    setMarkings(Set);
                    try
                    {
                        Set.CurrentHandler = 1;
                        Set.HT_Name = "Archit";
                        Set = SetSuggestedRelearnMoves_PKSM(Set);
                        Set.SetPIDNature(Set.Nature);
                        if (shiny) Set.SetShinyPID();
                        if (Set.PID == 0)
                        {
                            Set.PID = PKX.GetRandomPID(Set.Species, Set.Gender, Set.Version, Set.Nature, Set.Format, (uint)(Set.AbilityNumber * 0x10001));
                            if (shiny) Set.SetShinyPID();
                        }
                        Set = FixMemoriesPKM(Set);
                        if (Set.GenNumber < 6) Set.EncryptionConstant = Set.PID;
                        if (CommonErrorHandling2(Set))
                        {
                            HyperTrain(Set);
                            if (shiny && !Set.IsShiny) Set.SetShinyPID();
                            return Set;
                        }
                        HyperTrain(Set);
                        if (new LegalityAnalysis(Set).Valid) legalized = true;
                        if (Set.GenNumber < 6 && !legalized) Set.EncryptionConstant = Set.PID;
                        if (new LegalityAnalysis(Set).Valid && SAV.Generation >= Set.GenNumber)
                        {
                            setHappiness(Set);
                            if (shiny && !Set.IsShiny) Set.SetShinySID();
                            return Set;
                        }
                        else
                        {
                            LegalityAnalysis la = new LegalityAnalysis(Set);
                            Console.WriteLine(la.Report(false));
                        }
                    }
                    catch { continue; }
                }
            }
            
            if (!new LegalityAnalysis(Set).Valid && !eventMon)
            {
                for (int i = 0; i < GameVersionList.Length; i++)
                {
                    if (Set.DebutGeneration > ((GameVersion)GameVersionList[i]).GetGeneration()) continue;
                    if (Set.Met_Level == 100) Set.Met_Level = 0;
                    Set.WasEgg = false;
                    Set.EggMetDate = null;
                    Set.Egg_Location = 0;
                    Set.Version = GameVersionList[i];
                    RestoreIVs(Set, SSet); // Restore IVs to SSet and HT to false
                    Set.Language = 2;
                    Set.ConsoleRegion = 2;
                    if (trainerinfo)
                    {
                        Set.OT_Name = OT;
                        Set.TID = TID;
                        Set.SID = SID;
                        Set.OT_Gender = gender;
                    }
                    else
                    {
                        Set.OT_Name = "Archit (TCD)";
                        Set.TID = 24521;
                        Set.SID = 42312;
                    }
                    if (Set.Species == 793 || Set.Species == 794 || Set.Species == 795 || Set.Species == 796 || Set.Species == 797 || Set.Species == 798 || Set.Species == 799 || Set.Species == 805 || Set.Species == 806) Set.Ball = 26;
                    if (Set.Version == (int)GameVersion.RD || Set.Version == (int)GameVersion.BU || Set.Version == (int)GameVersion.YW || Set.Version == (int)GameVersion.GN || Set.Version == (int)GameVersion.GD || Set.Version == (int)GameVersion.SV || Set.Version == (int)GameVersion.C)
                    {
                        Set.SID = 0;
                        if (OT.Length > 6)
                        {
                            Set.OT_Name = "ARCH";
                        }
                    }
                    Set.MetDate = DateTime.Today;
                    setMarkings(Set);
                    try
                    {
                        Set.RelearnMove1 = 0;
                        Set.RelearnMove2 = 0;
                        Set.RelearnMove3 = 0;
                        Set.RelearnMove4 = 0;
                        if (Set.Version == (int)GameVersion.RD || Set.Version == (int)GameVersion.BU || Set.Version == (int)GameVersion.YW || Set.Version == (int)GameVersion.GN)
                        {
                            Set.Met_Location = 30013;
                            Set.Met_Level = 100;
                        }
                        else if (Set.Version == (int)GameVersion.GD || Set.Version == (int)GameVersion.SV || Set.Version == (int)GameVersion.C)
                        {
                            Set.Met_Location = 30017;
                            Set.Met_Level = 100;
                        }
                        else if (Set.Version == (int)GameVersion.CXD)
                        {
                            Set.Met_Location = 30001;
                            Set.Met_Level = 100;
                        }
                        else
                        {
                            clickMetLocationModPKSM(Set);
                        }
                        Set = SetSuggestedRelearnMoves_PKSM(Set);
                        Set.CurrentHandler = 1;
                        Set.HT_Name = "Archit";
                        Set.PID = PKX.GetRandomPID(Set.Species, Set.Gender, Set.Version, Set.Nature, Set.Format, (uint)(Set.AbilityNumber * 0x10001));
                        if (shiny) Set.SetShinyPID();
                        if (Set.PID == 0)
                        {
                            Set.PID = PKX.GetRandomPID(Set.Species, Set.Gender, Set.Version, Set.Nature, Set.Format, (uint)(Set.AbilityNumber * 0x10001));
                            if (shiny) Set.SetShinyPID();
                        }
                        Set.RefreshAbility(abilitynum);
                        Set = FixMemoriesPKM(Set);
                        if (Set.GenNumber < 6) Set.EncryptionConstant = Set.PID;
                        if (CommonErrorHandling2(Set))
                        {
                            HyperTrain(Set);
                            if (shiny) Set.SetShinyPID();
                            return Set;
                        }
                        HyperTrain(Set);
                        if (new LegalityAnalysis(Set).Valid) legalized = true;
                        AlternateAbilityRefresh(Set);
                        if (Set.GenNumber < 6 && !legalized) Set.EncryptionConstant = Set.PID;
                        if (new LegalityAnalysis(Set).Valid && SAV.Generation >= Set.GenNumber)
                        {
                            setHappiness(Set);
                            PKM returnval = Set;
                            if (shiny && Set.IsShiny) return Set;
                            if (shiny && !Set.IsShiny)
                            {
                                Set.SetShinySID();
                                if (new LegalityAnalysis(Set).Valid) return Set;
                                Set = returnval;
                                Set.SetShinyPID();
                                if (new LegalityAnalysis(Set).Valid) return Set;
                            }
                            else return returnval;
                        }
                        else
                        {
                            List<EncounterStatic> edgeLegality = edgeMons(Set.Version, Set);
                            foreach (EncounterStatic el in edgeLegality)
                            {
                                Set.Met_Location = el.Location;
                                Set.Met_Level = el.Level;
                                Set.CurrentLevel = 100;
                                Set.FatefulEncounter = el.Fateful;
                                if (el.RibbonWishing) ReflectUtil.SetValue(Set, "RibbonWishing", -1);
                                Set.RelearnMoves = el.Relearn;
                                if (SSet.Shiny && (el.Shiny == Shiny.Always || el.Shiny == Shiny.Random)) Set.SetShinyPID();
                                else if (el.Shiny == Shiny.Never && Set.IsShiny) Set.PID ^= 0x10000000;
                                else Set.SetPIDGender(Set.Gender);
                            }
                            LegalityAnalysis la = new LegalityAnalysis(Set);
                            if (la.Valid) return Set;
                            Console.WriteLine(la.Report(false));
                        }
                    }
                    catch { continue; }
                }
            }
            //return Set;

            if (!new LegalityAnalysis(Set).Valid)
            {
                string fpath = Path.Combine(Directory.GetCurrentDirectory(), "mgdb");
                List<string> fileList = new List<string>();
                string[] PKMNList = Util.GetSpeciesList("en");
                List<string> chain = new List<string>();
                if (!legendary)
                {
                    foreach (List<string> a in evoChart)
                    {
                        foreach (string b in a)
                        {
                            if (b == PKMNList[Set.Species] && Set.Species != 0)
                            {
                                chain = a;
                            }
                        }
                    }
                }
                if (chain.Count == 0 && Set.Species != 0) chain.Add(PKMNList[Set.Species]);
                foreach (string file in System.IO.Directory.GetFiles(fpath, "*.*", System.IO.SearchOption.AllDirectories))
                {
                    foreach (string mon in chain)
                    {
                        if (file.ToLower().Contains(mon.ToLower()) || Path.GetExtension(file) == ".pl6")
                        {
                            fileList.Add(file);
                            Console.WriteLine(file);
                        }
                    }
                }
                PKM prevevent = new PK7();
                foreach (string file in fileList)
                {
                    PKM eventpk = Set;
                    int PIDType = -1;
                    int Generation = 0;
                    int AbilityType = -1;
                    uint fixedPID = 0;
                    int form = Set.AltForm;
                    if (System.IO.Path.GetExtension(file) == ".wc7" || System.IO.Path.GetExtension(file) == ".wc7full")
                    {
                        var mg = (WC7)MysteryGift.GetMysteryGift(System.IO.File.ReadAllBytes(file), System.IO.Path.GetExtension(file));
                        PIDType = (int)mg.PIDType;
                        AbilityType = mg.AbilityType;
                        Generation = 7;
                        fixedPID = mg.PID;
                        if (!ValidShiny((int)mg.PIDType, shiny)) continue;
                        var temp = mg.ConvertToPKM(SAV);
                        eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out string c);
                    }
                    else if (System.IO.Path.GetExtension(file) == ".wc6" || System.IO.Path.GetExtension(file) == ".wc6full")
                    {
                        var mg = (WC6)MysteryGift.GetMysteryGift(System.IO.File.ReadAllBytes(file), System.IO.Path.GetExtension(file));
                        PIDType = (int)mg.PIDType;
                        AbilityType = mg.AbilityType;
                        Generation = 6;
                        fixedPID = mg.PID;
                        if (!ValidShiny((int)mg.PIDType, shiny)) continue;
                        var temp = mg.ConvertToPKM(SAV);
                        eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out string c);
                    }
                    else if (System.IO.Path.GetExtension(file) == ".pl6") // Pokemon Link
                    {
                        PL6_PKM[] LinkPokemon = new PL6(File.ReadAllBytes(file)).Pokes;
                        bool ExistsEligible = false;
                        PL6_PKM Eligible = new PL6_PKM();
                        foreach (PL6_PKM i in LinkPokemon)
                        {
                            if (i.Species != Set.Species) continue;
                            else
                            {
                                Eligible = i;
                                ExistsEligible = true;
                                PIDType = i.PIDType;
                                AbilityType = i.AbilityType;
                                Generation = 6;
                                fixedPID = i.PID;
                                break;
                            }
                        }
                        if (ExistsEligible) eventpk = PKMConverter.ConvertToType(ConvertPL6ToPKM(Eligible), SAV.PKMType, out string c);
                    }
                    else if (System.IO.Path.GetExtension(file) == ".pgf")
                    {
                        var mg = (PGF)MysteryGift.GetMysteryGift(System.IO.File.ReadAllBytes(file), System.IO.Path.GetExtension(file));
                        PIDType = mg.PIDType;
                        AbilityType = mg.AbilityType;
                        Generation = 5;
                        fixedPID = mg.PID;
                        if (!ValidShiny(mg.PIDType, shiny)) continue;
                        var temp = mg.ConvertToPKM(SAV);
                        eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out string c);
                    }
                    else if (System.IO.Path.GetExtension(file) == ".pgt" || System.IO.Path.GetExtension(file) == ".pcd" || System.IO.Path.GetExtension(file) == ".wc4")
                    {
                        try
                        {
                            var mg = (PCD)MysteryGift.GetMysteryGift(System.IO.File.ReadAllBytes(file), System.IO.Path.GetExtension(file));
                            Generation = 4;
                            if (shiny != mg.IsShiny) continue;
                            var temp = mg.ConvertToPKM(SAV);
                            eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out string c);
                            fixedPID = eventpk.PID;
                        }
                        catch
                        {
                            var mg = (PGT)MysteryGift.GetMysteryGift(System.IO.File.ReadAllBytes(file), System.IO.Path.GetExtension(file));
                            Generation = 4;
                            if (shiny != mg.IsShiny) continue;
                            var temp = mg.ConvertToPKM(SAV);
                            eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out string c);
                            fixedPID = eventpk.PID;
                        }
                    }
                    else if (System.IO.Path.GetExtension(file) == ".pk3")
                    {
                        Generation = 3;
                        var pk = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(file), prefer: Path.GetExtension(file).Length > 0 ? (Path.GetExtension(file).Last() - '0') & 0xF : SAV.Generation);
                        if (pk == null) break;
                        eventpk = PKMConverter.ConvertToType(pk, SAV.PKMType, out string c);
                    }

                    if (SSet.Form != null)
                    {
                        if (SSet.Form.Contains("Mega") || SSet.Form == "Primal" || SSet.Form == "Busted")
                        {
                            resetForm = true;
                            if (resetForm)
                            {
                                eventpk.AltForm = 0;
                                eventpk.RefreshAbility(eventpk.AbilityNumber < 6 ? eventpk.AbilityNumber >> 1 : 0);
                            }
                        }
                    }
                    try
                    {
                        if ((PIDType == 0 && eventpk.IsShiny && shiny == false && Generation > 4) || (PIDType == 0 && !eventpk.IsShiny && shiny == true && Generation > 4)) continue;
                        if (shiny == true && !eventpk.IsShiny && Generation > 4)
                        {
                            if (PIDType == 1) eventpk.SetShinyPID();
                            else if (PIDType == 3) continue;
                        }
                        if (shiny == false && eventpk.IsShiny && Generation > 4)
                        {
                            if (PIDType == 1) eventpk.PID ^= 0x10000000;
                            else if (PIDType == 2) continue;
                        }
                        eventpk.Species = Set.Species;
                        eventpk.AltForm = form;
                        eventpk.Nickname = eventpk.IsNicknamed ? eventpk.Nickname : PKX.GetSpeciesNameGeneration(Set.Species, eventpk.Language, SAV.Generation);
                        eventpk.HeldItem = SSet.HeldItem < 0 ? 0 : SSet.HeldItem;
                        eventpk.Nature = SSet.Nature < 0 ? 0 : Set.Nature;
                        eventpk.Ability = SSet.Ability;

                        // Set IVs
                        eventpk.IV_HP = SSet.IVs[0];
                        eventpk.IV_ATK = SSet.IVs[1];
                        eventpk.IV_DEF = SSet.IVs[2];
                        eventpk.IV_SPA = SSet.IVs[4];
                        eventpk.IV_SPD = SSet.IVs[5];
                        eventpk.IV_SPE = SSet.IVs[3];

                        // Set EVs
                        eventpk.EV_HP = Set.EVs[0];
                        eventpk.EV_ATK = Set.EVs[1];
                        eventpk.EV_DEF = Set.EVs[2];
                        eventpk.EV_SPA = Set.EVs[4];
                        eventpk.EV_SPD = Set.EVs[5];
                        eventpk.EV_SPE = Set.EVs[3];


                        eventpk.CurrentLevel = 100;
                        eventpk.Move1 = SSet.Moves[0];
                        eventpk.Move2 = SSet.Moves[1];
                        eventpk.Move3 = SSet.Moves[2];
                        eventpk.Move4 = SSet.Moves[3];

                        // PP Ups!
                        eventpk.Move1_PPUps = SSet.Moves[0] != 0 ? 3 : 0;
                        eventpk.Move2_PPUps = SSet.Moves[1] != 0 ? 3 : 0;
                        eventpk.Move3_PPUps = SSet.Moves[2] != 0 ? 3 : 0;
                        eventpk.Move4_PPUps = SSet.Moves[3] != 0 ? 3 : 0;

                        eventpk.Move1_PP = eventpk.GetMovePP(eventpk.Move1, eventpk.Move1_PPUps);
                        eventpk.Move2_PP = eventpk.GetMovePP(eventpk.Move2, eventpk.Move2_PPUps);
                        eventpk.Move3_PP = eventpk.GetMovePP(eventpk.Move3, eventpk.Move3_PPUps);
                        eventpk.Move4_PP = eventpk.GetMovePP(eventpk.Move4, eventpk.Move4_PPUps);

                        setMarkings(eventpk);
                        setHappiness(eventpk);
                        HyperTrain(eventpk);

                        if (new LegalityAnalysis(eventpk).Valid) return eventpk;

                        eventpk = SetWCXPID(eventpk, PIDType, Generation, AbilityType, shiny);
                        LegalityAnalysis la2 = new LegalityAnalysis(eventpk);
                        if (!la2.Valid)
                        {
                            Console.WriteLine(la2.Report(false));
                            AlternateAbilityRefresh(eventpk);
                            if (new LegalityAnalysis(eventpk).Valid) return eventpk;
                            if (eventErrorHandling(eventpk, PIDType, AbilityType, Generation, fixedPID)) return eventpk;
                            prevevent = eventpk;
                            continue;
                        }
                        else
                        {
                            return eventpk;
                        }
                    }
                    catch { continue; }
                }
                Set = prevevent;
            }
            return Set;
        }

        private void setMarkings(PKM pk)
        {
            if (pk.IV_HP == 31) pk.MarkCircle = 1;
            if (pk.IV_ATK == 31) pk.MarkTriangle = 1;
            if (pk.IV_DEF == 31) pk.MarkSquare = 1;
            if (pk.IV_SPA == 31) pk.MarkHeart = 1;
            if (pk.IV_SPD == 31) pk.MarkStar = 1;
            if (pk.IV_SPE == 31) pk.MarkDiamond = 1;
            if (pk.IV_HP == 30 || pk.IV_HP == 29) pk.MarkCircle = 2;
            if (pk.IV_ATK == 30 || pk.IV_ATK == 29) pk.MarkTriangle = 2;
            if (pk.IV_DEF == 30 || pk.IV_DEF == 29) pk.MarkSquare = 2;
            if (pk.IV_SPA == 30 || pk.IV_SPA == 29) pk.MarkHeart = 2;
            if (pk.IV_SPD == 30 || pk.IV_SPD == 29) pk.MarkStar = 2;
            if (pk.IV_SPE == 30 || pk.IV_SPE == 29) pk.MarkDiamond = 2;
            if (pk.IV_HP < 29) pk.MarkCircle = 1;
            if (pk.IV_ATK < 29) pk.MarkTriangle = 1;
            if (pk.IV_DEF < 29) pk.MarkSquare = 1;
            if (pk.IV_SPA < 29) pk.MarkHeart = 1;
            if (pk.IV_SPD < 29) pk.MarkStar = 1;
            if (pk.IV_SPE < 29) pk.MarkDiamond = 1;
        }

        private void setHappiness(PKM pk)
        {
            if (pk.Moves.Contains(218)) pk.CurrentFriendship = 0;
            else pk.CurrentFriendship = 255;
        }

        private List<EncounterStatic> edgeMons(int Game, PKM pk)
        {
            List<EncounterStatic> edgecase = new List<EncounterStatic>();
            EdgeCaseLegality el = new EdgeCaseLegality();
            var edgecasearray = new EncounterStatic[] { };
            if (Game == (int)GameVersion.B || Game == (int)GameVersion.W)
            {
                edgecasearray = el.BWEntreeForest;
            }
            else if (Game == (int)GameVersion.B2 || Game == (int)GameVersion.W2)
            {
                edgecasearray = el.B2W2EntreeForest;
            }
            else if (Game == (int)GameVersion.US || Game == (int)GameVersion.UM)
            {
                edgecasearray = el.USUMEdgeEnc;
            }
            foreach (EncounterStatic e in edgecasearray) {
                if (e.Species == pk.Species)
                {
                    edgecase.Add(e);
                }
            }
            return edgecase;
        }

        private bool eventErrorHandling(PKM pk, int PIDType, int AbilityType, int Generation, uint fixedPID)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            var report = la.Report(false);
            Console.WriteLine(fixedPID);
            if (pk.Species == 658 && pk.Ability == 210) // Ash-Greninja Fix
            {
                pk.Version = (int)GameVersion.SN;
                pk.IVs = new int[] { 20, 31, 20, 31, 31, 20 };
            }
            if (report.Contains(string.Format(V255, "OT")) || report.Contains(string.Format(V255, "HT"))) //V255 = {0} Memory: Invalid Feeling (0 = OT/HT)
            {
                pk.HT_Memory = 3;
                pk.HT_TextVar = 9;
                pk.HT_Intensity = 1;
                pk.HT_Feeling = Memories.GetRandomFeeling(pk.HT_Memory);
                pk.HT_Friendship = pk.OT_Friendship;
                pk.OT_Memory = 3;
                pk.OT_TextVar = 9;
                pk.OT_Intensity = 1;
                pk.OT_Feeling = Memories.GetRandomFeeling(pk.OT_Memory);
                report = UpdateReport(pk);
            }
            if (report.Contains(V20)) // V20: Nickname does not match species name
            {
                pk.IsNicknamed = false;
                pk.Nickname = PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, Generation);
                report = UpdateReport(pk);
            }
            if (report.Contains(V410)) // V410 = Mystery Gift fixed PID mismatch.
            {
                pk.PID = fixedPID;
                report = UpdateReport(pk);
            }
            if (report.Contains(V411)) // V411 = Encounter type PID mismatch
            {
                if ((usesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.FR) || (usesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.LG))
                {
                    bool shiny = pk.IsShiny;
                    pk = M2EventFix(pk, shiny);
                    if (requestedShiny && !pk.IsShiny) pk.SetShinySID();
                    report = UpdateReport(pk);
                }

                if (usesEventBasedMethod(pk.Species, pk.Moves, "BACD_R") && pk.Version == (int)GameVersion.R)
                {
                    bool shiny = pk.IsShiny;
                    pk = BACD_REventFix(pk, pk.IsShiny);
                    if (requestedShiny && !pk.IsShiny) pk.SetShinySID(); // Make wrong requests fail
                    report = UpdateReport(pk);
                }
            }
            if (new LegalityAnalysis(pk).Valid) return true;
            Console.WriteLine(report);
            return false;
        }

        private PKM SetWCXPID(PKM pk, int PIDType, int Generation, int AbilityType, bool shiny)
        {
            if (Generation == 6 || Generation == 7)
            {
                switch (PIDType)
                {
                    case 00: // Specified
                        pk.PID = pk.PID;
                        break;
                    case 01: // Random
                        pk.PID = Util.Rand32();
                        break;
                    case 02: // Random Shiny
                        pk.PID = Util.Rand32();
                        pk.PID = (uint)(((pk.TID ^ pk.SID ^ (pk.PID & 0xFFFF)) << 16) + (pk.PID & 0xFFFF));
                        break;
                    case 03: // Random Nonshiny
                        pk.PID = Util.Rand32();
                        if ((uint)(((pk.TID ^ pk.SID ^ (pk.PID & 0xFFFF)) << 16) + (pk.PID & 0xFFFF)) < 16) pk.PID ^= 0x10000000;
                        break;
                }
                return pk;
            }
            else if (Generation == 5)
            {
                int av = 0;
                switch (AbilityType)
                {
                    case 00: // 0 - 0
                    case 01: // 1 - 1
                    case 02: // 2 - H
                        av = AbilityType;
                        break;
                    case 03: // 0/1
                    case 04: // 0/1/H
                        av = (int)(Util.Rand32() % (AbilityType - 1));
                        break;
                }
                if (pk.PID != 0)
                    pk.PID = pk.PID;
                else
                {
                    pk.PID = Util.Rand32();

                    // Force Gender
                    do { pk.PID = (pk.PID & 0xFFFFFF00) | Util.Rand32() & 0xFF; } while (!pk.IsGenderValid());

                    // Force Ability
                    if (av == 1) pk.PID |= 0x10000; else pk.PID &= 0xFFFEFFFF;

                    if (PIDType == 2) // Force Shiny
                    {
                        uint gb = pk.PID & 0xFF;
                        pk.PID = PIDGenerator.GetMG5ShinyPID(gb, (uint)av, pk.TID, pk.SID);
                    }
                    else if (PIDType != 1) // Force Not Shiny
                    {
                        if (pk.IsShiny)
                            pk.PID ^= 0x10000000;
                    }
                }
                return pk;
            }
            else if (Generation == 4)
            {
                uint seed = Util.Rand32();
                if (pk.PID == 1) // Create Nonshiny
                {
                    uint pid1 = PKX.LCRNG(ref seed) >> 16;
                    uint pid2 = PKX.LCRNG(ref seed) >> 16;

                    while ((pid1 ^ pid2 ^ pk.TID ^ pk.SID) < 8)
                    {
                        uint testPID = pid1 | pid2 << 16;

                        // Call the ARNG to change the PID
                        testPID = RNG.ARNG.Next(testPID);

                        pid1 = testPID & 0xFFFF;
                        pid2 = testPID >> 16;
                    }
                    pk.PID = pid1 | (pid2 << 16);
                }
                // Handle PKX events that are not IV locked to PID
                pk.SetPIDGender(pk.Gender);
                if (new LegalityAnalysis(pk).Valid) return pk;
                else Console.WriteLine(new LegalityAnalysis(pk).Report(false));
                PK4 pk4 = new PK4();
                // Generate IVs
                if (pk4.IV32 == 0)
                {
                    uint iv1 = (PKX.LCRNG(ref seed) >> 16) & 0x7FFF;
                    uint iv2 = (PKX.LCRNG(ref seed) >> 16) & 0x7FFF;
                    pk4.IV32 = iv1 | iv2 << 15;
                }
                pk.IV_HP = pk4.IV_HP;
                pk.IV_ATK = pk4.IV_ATK;
                pk.IV_DEF = pk4.IV_DEF;
                pk.IV_SPA = pk4.IV_SPA;
                pk.IV_SPD = pk4.IV_SPD;
                pk.IV_SPE = pk4.IV_SPE;
                return pk;
            }
            else
            {
                return pk;
            }
        }

        private bool ValidAbility(int AbilityNumber, int AbilityType)
        {
            Console.WriteLine("AbilityNumber = " + AbilityNumber + " AbilityType = " + AbilityType);
            if ((AbilityNumber == 1 && AbilityType == 0) || (AbilityNumber == 1 && AbilityType == 3) || (AbilityNumber == 1 && AbilityType == 4)) return true;
            if ((AbilityNumber == 2 && AbilityType == 1) || (AbilityNumber == 2 && AbilityType == 3) || (AbilityNumber == 2 && AbilityType == 4)) return true;
            if ((AbilityNumber == 4 && AbilityType == 2) || (AbilityNumber == 4 && AbilityType == 4)) return true;
            return false;
        }

        private bool ValidShiny(int PIDType, bool shiny)
        {
            if ((PIDType == 0 && shiny == true) || (PIDType == 1 && shiny == true) || (PIDType == 2 && shiny == true)) return true;
            if ((PIDType == 0 && shiny == false) || (PIDType == 1 && shiny == false) || (PIDType == 3 && shiny == false)) return true;
            return false;
        }

        private PKM SetSuggestedRelearnMoves_PKSM(PKM Set)
        {
            Set.RelearnMove1 = 0;
            Set.RelearnMove2 = 0;
            Set.RelearnMove3 = 0;
            Set.RelearnMove4 = 0;
            LegalityAnalysis Legality = new LegalityAnalysis(Set);
            if (Set.Format < 6)
                return Set;

            int[] m = Legality.GetSuggestedRelearn();
            if (m.All(z => z == 0))
                if (!Set.WasEgg && !Set.WasEvent && !Set.WasEventEgg && !Set.WasLink)
                {
                    if (Set.Version != (int)GameVersion.CXD)
                    {
                        var encounter = Legality.GetSuggestedMetInfo();
                        if (encounter != null)
                            m = encounter.Relearn;
                    }
                }

            if (Set.RelearnMoves.SequenceEqual(m))
                return Set;

            Set.RelearnMove1 = m[0];
            Set.RelearnMove2 = m[1];
            Set.RelearnMove3 = m[2];
            Set.RelearnMove4 = m[3];
            return Set;
        }

        private void AlternateAbilityRefresh(PKM pk)
        {
            int abilityID = pk.Ability;
            int abilityNum = pk.AbilityNumber;
            int finalabilitynum = abilityNum;
            int[] abilityNumList = new int[] { 1, 2, 4 };
            for (int i = 0; i < 3; i++)
            {
                pk.AbilityNumber = abilityNumList[i];
                pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                if (pk.Ability == abilityID)
                {
                    LegalityAnalysis recheckLA = new LegalityAnalysis(pk);
                    var updatedReport = recheckLA.Report(false);
                    if (!updatedReport.Contains("Ability mismatch for encounter"))
                    {
                        finalabilitynum = pk.AbilityNumber;
                        break;
                    }
                }
            }
            pk.AbilityNumber = finalabilitynum;
            pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
        }
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
        public bool CommonErrorHandling2(PKM pk)
        {
            string hp = pk.IV_HP.ToString();
            string atk = pk.IV_ATK.ToString();
            string def = pk.IV_DEF.ToString();
            string spa = pk.IV_SPA.ToString();
            string spd = pk.IV_SPD.ToString();
            string spe = pk.IV_SPE.ToString();
            LegalityAnalysis la = new LegalityAnalysis(pk);
            var report = la.Report(false);

            // fucking M2
            if ((usesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.FR) || (usesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.LG))
            {
                pk = M2EventFix(pk, pk.IsShiny);
                report = UpdateReport(pk);
            }

            if (usesEventBasedMethod(pk.Species, pk.Moves, "BACD_R") && pk.Version == (int)GameVersion.R)
            {
                pk = BACD_REventFix(pk, pk.IsShiny);
                report = UpdateReport(pk);
            }

            if (report.Contains(V223)) //V223 = Ability mismatch for encounter.
            {
                pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                report = UpdateReport(pk);
                if (report.Contains(V223)) //V223 = Ability mismatch for encounter.
                {
                    AlternateAbilityRefresh(pk);
                }
                report = UpdateReport(pk);
            }
            if (report.Contains(V61)) //V61 = Invalid Met Location, expected Transporter.
            {
                pk.Met_Location = 30001;
                report = UpdateReport(pk);
            }
            if (report.Contains(V118)) //V118 = Can't have ball for encounter type.
            {
                if (pk.B2W2)
                {
                    pk.Ball = 25; //Dream Ball
                    report = UpdateReport(pk);
                }
                else
                {
                    pk.Ball = 0;
                    report = UpdateReport(pk);
                }
            }
            if (report.Contains(V353)) //V353 = Non japanese Mew from Faraway Island. Unreleased event.
            {
                bool shiny = pk.IsShiny;
                pk.Language = 1;
                pk.FatefulEncounter = true;
                pk.Nickname = PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, 3);
                pk.PID = PKX.GetRandomPID(pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (shiny) pk.SetShinySID();
                report = UpdateReport(pk);
            }
            if (report.Contains(V208)) //V208 = Encryption Constant matches PID.
            {
                int wIndex = Array.IndexOf(Legal.WurmpleEvolutions, pk.Species);
                uint EC = wIndex < 0 ? Util.Rand32() : PKX.GetWurmpleEC(wIndex / 2);
                pk.EncryptionConstant = EC;
                report = UpdateReport(pk);
            }
            if (report.Contains(V216)) //V216 = PID should be equal to EC!
            {
                pk.EncryptionConstant = pk.PID;
                report = UpdateReport(pk);
            }
            if (report.Contains(V215)) //V215 = PID should be equal to EC [with top bit flipped]!
            {
                pk.PID = PKX.GetRandomPID(pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (pk.IsShiny) pk.SetShinyPID();
                report = UpdateReport(pk);
            }
            if (report.Contains(V251)) //V251 = PID-Gender mismatch.
            {
                if (pk.Gender == 0)
                {
                    pk.Gender = 1;
                }
                else
                {
                    pk.Gender = 0;
                }
                report = UpdateReport(pk);
            }
            if (report.Contains(V407) || report.Contains(V408)) //V407 = OT from Colosseum/XD cannot be female. V408 = Female OT from Generation 1 / 2 is invalid.
            {
                pk.OT_Gender = 0;
                report = UpdateReport(pk);
            }
            if (report.Contains(V85)) //V85 = Current level is below met level.
            {
                pk.CurrentLevel = 100;
                report = UpdateReport(pk);
            }
            if (report.Contains(string.Format(V600, "National"))) //V600 = Missing Ribbons: {0} (National in this case)
            {
                ReflectUtil.SetValue(pk, "RibbonNational", -1);
                report = UpdateReport(pk);
            }
            if (report.Contains(string.Format(V601, "National"))) //V601 = Invalid Ribbons: {0} (National in this case)
            {
                ReflectUtil.SetValue(pk, "RibbonNational", 0);
                report = UpdateReport(pk);
            }
            if (report.Contains(V38)) //V38 = OT Name too long.
            {
                pk.OT_Name = "ARCH";
                report = UpdateReport(pk);
            }
            if (report.Contains(V421)) //V421 = OT from Generation 1/2 uses unavailable characters.
            {
                pk.OT_Name = "ARCH";
                report = UpdateReport(pk);
            }
            if (report.Contains(V146))
            {
                pk.Geo1_Country = 1; // Prev residence
                report = UpdateReport(pk);
            }
            if (report.Contains(V150)) //V150 = Memory: Handling Trainer Memory missing.
            {
                pk.HT_Memory = 3;
                pk.HT_TextVar = 9;
                pk.HT_Intensity = 1;
                pk.HT_Feeling = Util.Rand.Next(0, 10); // 0-9
                pk.HT_Friendship = pk.OT_Friendship;
                report = UpdateReport(pk);
            }
            if (report.Contains(V152)) //V152 = Memory: Original Trainer Memory missing.
            {
                pk.OT_Memory = 3;
                pk.OT_TextVar = 9;
                pk.OT_Intensity = 1;
                pk.OT_Feeling = Util.Rand.Next(0, 10); // 0-9
                report = UpdateReport(pk);
            }
            if (report.Contains(string.Format(V255, "OT")) || report.Contains(string.Format(V255, "HT"))) //V255 = {0} Memory: Invalid Feeling (0 = OT/HT)
            {
                pk.HT_Memory = 3;
                pk.HT_TextVar = 9;
                pk.HT_Intensity = 1;
                pk.HT_Feeling = Memories.GetRandomFeeling(pk.HT_Memory);
                pk.HT_Friendship = pk.OT_Friendship;
                pk.OT_Memory = 3;
                pk.OT_TextVar = 9;
                pk.OT_Intensity = 1;
                pk.OT_Feeling = Memories.GetRandomFeeling(pk.OT_Memory);
                report = UpdateReport(pk);
            }
            if (report.Contains(V130)) //V130 = Can't have any OT Memory.
            {
                pk.OT_Memory = 0;
                pk.OT_TextVar = 0;
                pk.OT_Intensity = 0;
                pk.OT_Feeling = 0;
                report = UpdateReport(pk);
            }
            if (report.Contains(V137)) //V137 = GeoLocation Memory: Memories should be present.
            {
                pk.Geo1_Country = 1;
                report = UpdateReport(pk);
            }
            if (report.Contains(V118)) //V118 = Can't have ball for encounter type.
            {
                pk.Ball = 4;
                report = UpdateReport(pk);
            }
            if (report.Contains(V302)) //Geolocation: Country is not in 3DS region.
            {
                pk.Country = 0;
                pk.Region = 0;
                pk.ConsoleRegion = 2;
                report = UpdateReport(pk);
            }
            if (report.Contains(V310)) //V310 = Form cannot exist outside of a battle.
            {
                if (pk.Species == 718 && pk.Ability == 211) pk.AltForm = 3; // Zygarde Edge case
                else pk.AltForm = 0;
                report = UpdateReport(pk);
            }
            if (report.Contains(V324)) //V324 = Special ingame Fateful Encounter flag missing.
            {
                pk.FatefulEncounter = true;
                report = UpdateReport(pk);
            }
            if (report.Contains(V325)) //V325 = Fateful Encounter should not be checked.

            {
                pk.FatefulEncounter = false;
                report = UpdateReport(pk);
            }
            if (report.Contains(V381)) //V381 = Encounter Type does not match encounter.
            {
                IEncounterable EncounterMatch = new LegalityAnalysis(pk).Info.EncounterMatch;
                EncounterType type = EncounterType.None;
                // Encounter type data is only stored for gen 4 encounters
                // All eggs have encounter type none, even if they are from static encounters
                if (pk.Gen4 && !pk.WasEgg)
                {
                    if (EncounterMatch is EncounterSlot w)
                        // If there is more than one slot, the get wild encounter have filter for the pkm type encounter like safari/sports ball
                        type = w.TypeEncounter;
                    if (EncounterMatch is EncounterStaticTyped s)
                        type = s.TypeEncounter;
                }

                if (!type.Contains(pk.EncounterType))
                    pk.EncounterType = Convert.ToInt32(Math.Log((int)type, 2));
                else
                    Console.WriteLine("This should never happen");
                report = UpdateReport(pk);
            }
            if (report.Contains(V86)) //V86 = Evolution not valid (or level/trade evolution unsatisfied).
            {
                pk.Met_Level = pk.Met_Level - 1;
                report = UpdateReport(pk);
            }
            if (report.Contains(V411)) //V411 = Encounter Type PID mismatch.
            {
                if (pk.Version == (int)GameVersion.CXD)
                { pk = setPIDSID(pk, pk.IsShiny, true); }
                else pk = setPIDSID(pk, pk.IsShiny);
                if (new LegalityAnalysis(pk).Valid)
                {
                    return false;
                }
                report = UpdateReport(pk);
                if (report.Equals(V411)) // V411 = Encounter Type PID mismatch.
                {
                    return true;
                }
                else if (report.Contains(V251)) // V251 = PID-Gender mismatch.
                {
                    if (pk.Gender == 0)
                    {
                        pk.Gender = 1;
                    }
                    else
                    {
                        pk.Gender = 0;
                    }
                    report = UpdateReport(pk);
                    if (new LegalityAnalysis(pk).Valid)
                    {
                        return false;
                    }
                }
            }
            if (report.Contains(V41)) // V41 = Can't Hyper Train a Pokémon with perfect IVs.
            {
                if (pk is IHyperTrain h)
                {
                    h.HT_HP = false;
                    h.HT_ATK = false;
                    h.HT_DEF = false;
                    h.HT_SPA = false;
                    h.HT_SPD = false;
                    h.HT_SPE = false;
                }
                report = UpdateReport(pk);
            }
            if (report.Contains(V42)) // V42 = Can't Hyper Train a perfect IV.
            {
                if (pk is IHyperTrain h)
                {
                    if (pk.IV_HP == 31) h.HT_HP = false;
                    if (pk.IV_ATK == 31) h.HT_ATK = false;
                    if (pk.IV_DEF == 31) h.HT_DEF = false;
                    if (pk.IV_SPA == 31) h.HT_SPA = false;
                    if (pk.IV_SPD == 31) h.HT_SPD = false;
                    if (pk.IV_SPE == 31) h.HT_SPE = false;
                }
                report = UpdateReport(pk);
            }

            /* Uncomment to automatically override specified IVs.
             * Default Behaviour would be to ignore this fix if IVs are specified to be of such values
             * 
             * 
            if (report.Contains(string.Format(V28, 3))) //V28 = Should have at least {0} IVs = 31.
            {
                PKM temp = pk;
                pk.IV_HP = 31;
                pk.IV_ATK = 31;
                pk.IV_DEF = 31;
                pk.IV_SPA = 31;
                pk.IV_SPD = 31;
                pk.IV_SPE = 31;
                report = UpdateReport(pk);
                if (new LegalityAnalysis(pk).Valid)
                {
                    return false;
                }
                else
                {
                    pk = temp;
                }
            }
            */

            return false;
        }

        private string UpdateReport(PKM pk)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            string report = la.Report(false);
            return report;
        }

        private PKM setPIDSID(PKM pk, bool shiny, bool XD = false)
        {
            uint hp = (uint)pk.IV_HP;
            uint atk = (uint)pk.IV_ATK;
            uint def = (uint)pk.IV_DEF;
            uint spa = (uint)pk.IV_SPA;
            uint spd = (uint)pk.IV_SPD;
            uint spe = (uint)pk.IV_SPE;
            uint nature = (uint)pk.Nature;
            bool pidsidmethod = true;
            string[] pidsid = { "", "" };
            if (XD)
            {
                pidsid = Misc.IVtoPIDGenerator.XDPID(hp, atk, def, spa, spd, spe, nature, 0);
            }
            else { pidsid = Misc.IVtoPIDGenerator.M1PID(hp, atk, def, spa, spd, spe, nature, 0); }
            if (pk.Species == 490 && pk.Gen4)
            {
                pk.WasEgg = true;
                pk.Egg_Location = 2002;
                pk.FatefulEncounter = true;
            }
            pk.PID = Util.GetHexValue(pidsid[0]);
            if (pk.GenNumber < 5) pk.EncryptionConstant = pk.PID;
            pk.SID = Convert.ToInt32(pidsid[1]);
            if (shiny) pk.SetShinySID();
            LegalityAnalysis recheckLA = new LegalityAnalysis(pk);
            string updatedReport = recheckLA.Report(false);
            Console.WriteLine(updatedReport);
            if (updatedReport.Contains("Invalid: Encounter Type PID mismatch."))
            {
                string[] hpower = { "fighting", "flying", "poison", "ground", "rock", "bug", "ghost", "steel", "fire", "water", "grass", "electric", "psychic", "ice", "dragon", "dark" };
                string hiddenpower = hpower[pk.HPType];
                string[] NatureHPIVs = Misc.IVtoPIDGenerator.getIVPID(nature, hiddenpower, XD);
                Console.WriteLine(XD);
                pk.PID = Util.GetHexValue(NatureHPIVs[0]);
                if (pk.GenNumber < 5) pk.EncryptionConstant = pk.PID;
                Console.WriteLine(NatureHPIVs[0]);
                pk.IV_HP = Convert.ToInt32(NatureHPIVs[1]);
                pk.IV_ATK = Convert.ToInt32(NatureHPIVs[2]);
                pk.IV_DEF = Convert.ToInt32(NatureHPIVs[3]);
                pk.IV_SPA = Convert.ToInt32(NatureHPIVs[4]);
                pk.IV_SPD = Convert.ToInt32(NatureHPIVs[5]);
                pk.IV_SPE = Convert.ToInt32(NatureHPIVs[6]);
                if (shiny) pk.SetShinySID();
                recheckLA = new LegalityAnalysis(pk);
                updatedReport = recheckLA.Report(false);
                if (!updatedReport.Contains("Invalid: Encounter Type PID mismatch.")) pidsidmethod = false;
                if (pidsid[0] == "0" && pidsid[1] == "0" && pidsidmethod)
                {
                    pk.PID = PKX.GetRandomPID(pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                    pk.IV_HP = (int)hp;
                    pk.IV_ATK = (int)atk;
                    pk.IV_DEF = (int)def;
                    pk.IV_SPA = (int)spa;
                    pk.IV_SPD = (int)spd;
                    pk.IV_SPE = (int)spe;
                }
                if (shiny) pk.SetShinySID();
                recheckLA = new LegalityAnalysis(pk);
                updatedReport = recheckLA.Report(false);
                if (updatedReport.Contains("PID-Gender mismatch."))
                {
                    if (pk.Gender == 0)
                    {
                        pk.Gender = 1;
                    }
                    else
                    {
                        pk.Gender = 0;
                    }
                    LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                    updatedReport = recheckLA2.Report(false);
                }
                if (updatedReport.Contains("Can't Hyper Train a Pokémon that isn't level 100."))
                {
                    pk.CurrentLevel = 100;
                    LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                    updatedReport = recheckLA2.Report(false);
                }
                LegalityAnalysis Legality = new LegalityAnalysis(pk);
                if (Legality.Valid) return pk;
                // Fix Moves if a slot is empty 
                pk.FixMoves();

                // PKX is now filled
                pk.RefreshChecksum();
                pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                if (updatedReport.Contains("Invalid: Encounter Type PID mismatch.") || usesEventBasedMethod(pk.Species, pk.Moves, "M2"))
                {
                    if (pk.GenNumber == 3 || usesEventBasedMethod(pk.Species, pk.Moves, "M2"))
                    {
                        pk = M2EventFix(pk, shiny);
                        if (!new LegalityAnalysis(pk).Report(false).Contains("PID mismatch") || usesEventBasedMethod(pk.Species, pk.Moves, "M2")) return pk;
                    }
                    pk.IV_HP = (int)hp;
                    pk.IV_ATK = (int)atk;
                    pk.IV_DEF = (int)def;
                    pk.IV_SPA = (int)spa;
                    pk.IV_SPD = (int)spd;
                    pk.IV_SPE = (int)spe;
                }
            }
            return pk;
        }

        private PKM M2EventFix(PKM pk, bool shiny)
        {
            int eggloc = pk.Egg_Location;
            bool feFlag = pk.FatefulEncounter;
            pk.Egg_Location = 0;
            pk.FatefulEncounter = true;
            string[] hpower = { "fighting", "flying", "poison", "ground", "rock", "bug", "ghost", "steel", "fire", "water", "grass", "electric", "psychic", "ice", "dragon", "dark" };
            string hiddenpower = hpower[pk.HPType];
            string[] NatureHPIVs = Misc.IVtoPIDGenerator.getIVPID((uint)pk.Nature, hiddenpower, false, "M2");
            pk.PID = Util.GetHexValue(NatureHPIVs[0]);
            if (pk.GenNumber < 5) pk.EncryptionConstant = pk.PID;
            Console.WriteLine(NatureHPIVs[0]);
            pk.IV_HP = Convert.ToInt32(NatureHPIVs[1]);
            pk.IV_ATK = Convert.ToInt32(NatureHPIVs[2]);
            pk.IV_DEF = Convert.ToInt32(NatureHPIVs[3]);
            pk.IV_SPA = Convert.ToInt32(NatureHPIVs[4]);
            pk.IV_SPD = Convert.ToInt32(NatureHPIVs[5]);
            pk.IV_SPE = Convert.ToInt32(NatureHPIVs[6]);
            if (shiny) pk.SetShinySID();
            LegalityAnalysis recheckLA = new LegalityAnalysis(pk);
            string updatedReport = recheckLA.Report(false);
            if (updatedReport.Contains("PID-Gender mismatch"))
            {
                if (pk.Gender == 0)
                {
                    pk.Gender = 1;
                }
                else
                {
                    pk.Gender = 0;
                }
                LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                updatedReport = recheckLA2.Report(false);
            }
            if (!updatedReport.Contains("PID mismatch") || usesEventBasedMethod(pk.Species, pk.Moves, "M2")) return pk;
            Console.WriteLine(UpdateReport(pk));
            pk.FatefulEncounter = feFlag;
            pk.Egg_Location = eggloc;
            return pk;
        }

        private PKM BACD_REventFix(PKM pk, bool shiny)
        {
            int eggloc = pk.Egg_Location;
            bool feFlag = pk.FatefulEncounter;
            pk.Egg_Location = 0;
            pk.FatefulEncounter = false;
            string[] hpower = { "fighting", "flying", "poison", "ground", "rock", "bug", "ghost", "steel", "fire", "water", "grass", "electric", "psychic", "ice", "dragon", "dark" };
            string hiddenpower = hpower[pk.HPType];
            string[] NatureHPIVs = Misc.IVtoPIDGenerator.getIVPID((uint)pk.Nature, hiddenpower, false, "BACD_R");
            pk.PID = Util.GetHexValue(NatureHPIVs[0]);
            if (pk.GenNumber < 5) pk.EncryptionConstant = pk.PID;
            Console.WriteLine(NatureHPIVs[0]);
            pk.IV_HP = Convert.ToInt32(NatureHPIVs[1]);
            pk.IV_ATK = Convert.ToInt32(NatureHPIVs[2]);
            pk.IV_DEF = Convert.ToInt32(NatureHPIVs[3]);
            pk.IV_SPA = Convert.ToInt32(NatureHPIVs[4]);
            pk.IV_SPD = Convert.ToInt32(NatureHPIVs[5]);
            pk.IV_SPE = Convert.ToInt32(NatureHPIVs[6]);
            if (shiny) pk.SetShinySID();
            LegalityAnalysis recheckLA = new LegalityAnalysis(pk);
            string updatedReport = recheckLA.Report(false);
            if (updatedReport.Contains("PID-Gender mismatch"))
            {
                if (pk.Gender == 0)
                {
                    pk.Gender = 1;
                }
                else
                {
                    pk.Gender = 0;
                }
                LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                updatedReport = recheckLA2.Report(false);
            }
            if (!updatedReport.Contains("PID mismatch") || usesEventBasedMethod(pk.Species, pk.Moves, "BACD_R")) return pk;
            Console.WriteLine(UpdateReport(pk));
            pk.FatefulEncounter = feFlag;
            pk.Egg_Location = eggloc;
            return pk;
        }

        public bool usesEventBasedMethod(int Species, int[] Moves, string method)
        {
            Dictionary<int, int[]> RNGList = new Dictionary<int, int[]>();
            if (getRNGListIndex(method) != -1) RNGList = WC3RNGList[getRNGListIndex(method)];
            else return false;
            if (!RNGList.Keys.Contains(Species)) return false;
            foreach (int i in Moves)
            {
                if (RNGList[Species].Contains(i)) return true;
            }
            return false;
        }

        int getRNGListIndex(string Method)
        {
            if (Method == "M2") return 0;
            else if (Method == "BACD_R") return 1;
            else return -1;
        }

        private PKM clickMetLocationModPKSM(PKM p)
        {
            LegalityAnalysis Legality = new LegalityAnalysis(p);

            var encounter = Legality.GetSuggestedMetInfo();
            if (encounter == null || (p.Format >= 3 && encounter.Location < 0))
            {
                return p;
            }

            int level = encounter.Level;
            int location = encounter.Location;
            int minlvl = Legal.GetLowestLevel(p, encounter.Species);
            if (minlvl == 0)
                minlvl = level;

            if (p.CurrentLevel >= minlvl && p.Met_Level == level && p.Met_Location == location)
                return p;
            if (minlvl < level)
                minlvl = level;
            p.Met_Location = location;
            p.Met_Level = level;
            return p;
        }

        private List<List<string>> generateEvoLists2()
        {
            int counter = 0;
            string line;
            List<List<string>> evoList = new List<List<string>>();
            List<string> blankList = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "AutoLegalityMod.Resources.txt.evolutions.txt";
            System.IO.Stream stream = assembly.GetManifestResourceStream(resourceName);
            System.IO.StreamReader file = new System.IO.StreamReader(stream);
            while ((line = file.ReadLine()) != null)
            {
                if (line.Trim() == "")
                {
                    evoList.Add(blankList);
                    blankList = new List<string>();
                }
                else
                {
                    blankList.Add(line.Trim());
                }
                counter++;
            }
            file.Close();
            return evoList;
        }

        private void RestoreIVs(PKM pk, ShowdownSet SSet)
        {
            pk.IVs = SSet.IVs;
            if (pk is IHyperTrain h)
            {
                h.HT_HP = false;
                h.HT_ATK = false;
                h.HT_DEF = false;
                h.HT_SPA = false;
                h.HT_SPD = false;
                h.HT_SPE = false;
            }
        }

        private bool NeedsHyperTraining(PKM pk)
        {
            int flawless = 0;
            int minIVs = 0;
            foreach(int i in pk.IVs)
            {
                if (i == 31) flawless++;
                if (i == 0 || i == 1) minIVs++; //ignore IV value = 0/1 for intentional IV values (1 for hidden power cases)
            }
            if (flawless + minIVs == 6) return false;
            return true;
        }

        private void HyperTrain(PKM pk)
        {
            if (SAV.Generation < 7 || !NeedsHyperTraining(pk)) return;
            if (pk.CurrentLevel != 100) pk.CurrentLevel = 100; // Set level for HT before doing HT
            if (pk is IHyperTrain h)
            {
                if (pk.IV_HP != 0 && pk.IV_HP != 1 && pk.IV_HP != 31) h.HT_HP = true;
                if (pk.IV_ATK != 0 && pk.IV_ATK != 1 && pk.IV_ATK != 31) h.HT_ATK = true;
                if (pk.IV_DEF != 0 && pk.IV_DEF != 1 && pk.IV_DEF != 31) h.HT_DEF = true;
                if (pk.IV_SPA != 0 && pk.IV_SPA != 1 && pk.IV_SPA != 31) h.HT_SPA = true;
                if (pk.IV_SPD != 0 && pk.IV_SPD != 1 && pk.IV_SPD != 31) h.HT_SPD = true;
                if (pk.IV_SPE != 0 && pk.IV_SPE != 1 && pk.IV_SPE != 31) h.HT_SPE = true;
            }
        }

        private void UpdateLegality(PKM pkm, bool skipMoveRepop = false)
        {
            LegalityAnalysis Legality = new LegalityAnalysis(pkm);
            Console.WriteLine(Legality.Report(true));
            // Refresh Move Legality
            bool[] validmoves = new bool[] { false, false, false, false };
            for (int i = 0; i < 4; i++)
                validmoves[i] = !Legality.Info?.Moves[i].Valid ?? false;

            bool[] validrelearn = new bool[] { false, false, false, false };
            if (pkm.Format >= 6)
                for (int i = 0; i < 4; i++)
                    validrelearn[i] = !Legality.Info?.Relearn[i].Valid ?? false;

            if (skipMoveRepop)
                return;
            // Resort moves
            bool fieldsLoaded = true;
            bool tmp = fieldsLoaded;
            fieldsLoaded = false;
            var cb = new[] { pkm.Move1, pkm.Move2, pkm.Move3, pkm.Move4 };
            var moves = Legality.AllSuggestedMovesAndRelearn;
            var moveList = GameInfo.MoveDataSource.OrderByDescending(m => moves.Contains(m.Value)).ToArray();

            fieldsLoaded |= tmp;
            LegalityChanged?.Invoke(Legality.Valid, null);
        }

        private PKM ConvertPL6ToPKM(PL6_PKM pk)
        {
            var pi = PersonalTable.AO.GetFormeEntry(pk.Species, pk.Form);
            PK6 eventpk = new PK6
            {
                Species = pk.Species,
                HeldItem = pk.HeldItem,
                TID = pk.TID,
                SID = pk.SID,
                Met_Level = pk.MetLevel,
                Nature = pk.Nature != 0xFF ? pk.Nature : (int)(Util.Rand32() % 25),
                Gender = pk.Gender != 3 ? pk.Gender : pi.RandomGender,
                AltForm = pk.Form,
                EncryptionConstant = pk.EncryptionConstant != 0 ? pk.EncryptionConstant : Util.Rand32(),
                Version = pk.OriginGame != 0 ? pk.OriginGame : (int)GameVersion.OR,
                Language = pk.Language != 0 ? pk.Language : SAV.Language,
                Ball = pk.Pokéball,
                Country = SAV.Country,
                Region = SAV.SubRegion,
                ConsoleRegion = SAV.ConsoleRegion,
                Move1 = pk.Move1,
                Move2 = pk.Move2,
                Move3 = pk.Move3,
                Move4 = pk.Move4,
                RelearnMove1 = pk.RelearnMove1,
                RelearnMove2 = pk.RelearnMove2,
                RelearnMove3 = pk.RelearnMove3,
                RelearnMove4 = pk.RelearnMove4,
                Met_Location = 30011,
                Egg_Location = pk.EggLocation,
                CNT_Cool = pk.CNT_Cool,
                CNT_Beauty = pk.CNT_Beauty,
                CNT_Cute = pk.CNT_Cute,
                CNT_Smart = pk.CNT_Smart,
                CNT_Tough = pk.CNT_Tough,
                CNT_Sheen = pk.CNT_Sheen,

                OT_Name = pk.OT.Length > 0 ? pk.OT : SAV.OT,
                OT_Gender = pk.OTGender != 3 ? pk.OTGender % 2 : SAV.Gender,
                HT_Name = pk.OT.Length > 0 ? SAV.OT : string.Empty,
                HT_Gender = pk.OT.Length > 0 ? SAV.Gender : 0,
                CurrentHandler = pk.OT.Length > 0 ? 1 : 0,

                EXP = PKX.GetEXP(pk.Level, pk.Species),

                IV_HP = pk.IV_HP,
                IV_ATK = pk.IV_ATK,
                IV_DEF = pk.IV_DEF,
                IV_SPA = pk.IV_SPA,
                IV_SPD = pk.IV_SPD,
                IV_SPE = pk.IV_SPE,
                
                // Ribbons
                RibbonCountry = pk.RibbonCountry,
                RibbonNational = pk.RibbonNational,

                RibbonEarth = pk.RibbonEarth,
                RibbonWorld = pk.RibbonWorld,
                RibbonClassic = pk.RibbonClassic,
                RibbonPremier = pk.RibbonPremier,
                RibbonEvent = pk.RibbonEvent,
                RibbonBirthday = pk.RibbonBirthday,
                RibbonSpecial = pk.RibbonSpecial,
                RibbonSouvenir = pk.RibbonSouvenir,

                RibbonWishing = pk.RibbonWishing,
                RibbonChampionBattle = pk.RibbonChampionBattle,
                RibbonChampionRegional = pk.RibbonChampionRegional,
                RibbonChampionNational = pk.RibbonChampionNational,
                RibbonChampionWorld = pk.RibbonChampionWorld,

                OT_Friendship = pi.BaseFriendship,
                OT_Intensity = pk.OT_Intensity,
                OT_Memory = pk.OT_Memory,
                OT_TextVar = pk.OT_TextVar,
                OT_Feeling = pk.OT_Feeling,
                FatefulEncounter = false,
            };
            eventpk.CurrentHandler = 1;
            eventpk.HT_Name = "Archit";
            eventpk.Move1_PP = eventpk.GetMovePP(pk.Move1, 0);
            eventpk.Move2_PP = eventpk.GetMovePP(pk.Move2, 0);
            eventpk.Move3_PP = eventpk.GetMovePP(pk.Move3, 0);
            eventpk.Move4_PP = eventpk.GetMovePP(pk.Move4, 0);

            eventpk.MetDate = DateTime.Now;

            if (SAV.Generation > 6 && pk.OriginGame == 0) // Gen7
            {
                eventpk.Version = (int)GameVersion.OR;
            }

            int av = 0;
            switch (pk.AbilityType)
            {
                case 00: // 0 - 0
                case 01: // 1 - 1
                case 02: // 2 - H
                    av = pk.AbilityType;
                    break;
                case 03: // 0/1
                case 04: // 0/1/H
                    av = (int)(Util.Rand32() % (pk.AbilityType - 1));
                    break;
            }
            switch (pk.PIDType)
            {
                case 00: // Specified
                    eventpk.PID = pk.PID;
                    break;
                case 01: // Random
                    eventpk.PID = Util.Rand32();
                    break;
                case 02: // Random Shiny
                    eventpk.PID = Util.Rand32();
                    eventpk.PID = (uint)(((pk.TID ^ pk.SID ^ (eventpk.PID & 0xFFFF)) << 16) + (eventpk.PID & 0xFFFF));
                    break;
                case 03: // Random Nonshiny
                    eventpk.PID = Util.Rand32();
                    if ((uint)(((pk.TID ^ pk.SID ^ (eventpk.PID & 0xFFFF)) << 16) + (eventpk.PID & 0xFFFF)) < 16) eventpk.PID ^= 0x10000000;
                    break;
            }
            eventpk.Ability = pi.Abilities[av];
            eventpk.AbilityNumber = 1 << av;

            if (!pk.IsEgg)
                if (eventpk.CurrentHandler == 0) // OT
                {
                    eventpk.OT_Memory = 3;
                    eventpk.OT_TextVar = 9;
                    eventpk.OT_Intensity = 1;
                    eventpk.OT_Feeling = Util.Rand.Next(0, 10); // 0-9
                }
                else
                {
                    eventpk.HT_Memory = 3;
                    eventpk.HT_TextVar = 9;
                    eventpk.HT_Intensity = 1;
                    eventpk.HT_Feeling = Util.Rand.Next(0, 10); // 0-9
                    eventpk.HT_Friendship = eventpk.OT_Friendship;
                }
            eventpk.IsNicknamed = false;
            eventpk.Nickname = eventpk.IsNicknamed ? eventpk.Nickname : PKX.GetSpeciesNameGeneration(pk.Species, eventpk.Language, SAV.Generation);
            eventpk.CurrentFriendship = eventpk.IsEgg ? pi.HatchCycles : pi.BaseFriendship;

            eventpk.RefreshChecksum();
            return eventpk;
        }

        public static Dictionary<int, int[]>[] WC3RNGList = new Dictionary<int, int[]>[] {
            new Dictionary<int, int[]>()
            { // M2
                {043, new[]{073}}, // Oddish with Leech Seed
                {044, new[]{073}}, // Gloom
                {045, new[]{073}}, // Vileplume
                {182, new[]{073}}, // Belossom
                {052, new[]{080}}, // Meowth with Petal Dance
                {053, new[]{080}}, //Persian
                {060, new[]{186}}, // Poliwag with Sweet Kiss
                {061, new[]{186}},
                {062, new[]{186}},
                {186, new[]{186}},
                {069, new[]{298}}, // Bellsprout with Teeter Dance
                {070, new[]{298}},
                {071, new[]{298}},
                {083, new[]{273, 281}}, // Farfetch'd with Wish & Yawn
                {096, new[]{273, 187}}, // Drowzee with Wish & Belly Drum
                {097, new[]{273, 187}},
                {102, new[]{273, 230}}, // Exeggcute with Wish & Sweet Scent
                {103, new[]{273, 230}},
                {108, new[]{273, 215}}, // Lickitung with Wish & Heal Bell
                {463, new[]{273, 215}},
                {113, new[]{273, 230}}, // Chansey with Wish & Sweet Scent
                {242, new[]{273, 230}}, 
                {115, new[]{273, 281}}, // Kangaskhan with Wish & Yawn
                {054, new[]{300}}, // Psyduck with Mud Sport
                {055, new[]{300}},
                {172, new[]{266, 058}}, // Pichu with Follow me
                {025, new[]{266, 058}},
                {026, new[]{266, 058}},
                {174, new[]{321}}, // Igglybuff with Tickle
                {039, new[]{321}},
                {040, new[]{321}},
                {222, new[]{300}}, // Corsola with Mud Sport
                {276, new[]{297}}, // Taillow with Feather Dance
                {277, new[]{297}},
                {283, new[]{300}}, // Surskit with Mud Sport
                {284, new[]{300}},
                {293, new[]{298}}, // Whismur with Teeter Dance
                {294, new[]{298}},
                {295, new[]{298}},
                {300, new[]{205, 006}}, // Skitty with Rollout or Payday
                {301, new[]{205, 006}},
                {311, new[]{346}}, // Plusle with Water Sport
                {312, new[]{300}}, // Minun with Mud Sport
                {325, new[]{253}}, // Spoink with Uproar
                {326, new[]{253}},
                {327, new[]{047}}, // Spinda with Sing
                {331, new[]{227}}, // Cacnea with Encore
                {332, new[]{227}},
                {341, new[]{346}}, // Corphish with Water Sport
                {342, new[]{346}},
                {360, new[]{321}}, // Wynaut with Tickle
                {202, new[]{321}},
                // Pokemon Box Events (M2)
                {263, new[]{245}}, // Zigzagoon with Extreme Speed
                {264, new[]{245}},
                {333, new[]{206}}, // False Swipe Swablu
                {334, new[]{206}},
                // Pay Day Skitty and evolutions (Accounted for with Rollout Skitty)
                // Surf Pichu and evolutions (Accounted for with Follow Me Pichu)
            },
            new Dictionary<int, int[]>()
            { // BACD_R
                {172, new[]{298, 273} }, // Pichu with Teeter Dance
                {025, new[]{298, 273} },
                {026, new[]{298, 273} },
                {280, new[]{204, 273} }, // Ralts with Charm
                {281, new[]{204, 273} },
                {282, new[]{204, 273} },
                {475, new[]{204, 273} }, 
                {359, new[]{180, 273} }, // Absol with Spite
                {371, new[]{334, 273} }, // Bagon with Iron Defense
                {372, new[]{334, 273} },
                {373, new[]{334, 273} },
                {385, new[]{034, 273} }
            }
        };
    }
}