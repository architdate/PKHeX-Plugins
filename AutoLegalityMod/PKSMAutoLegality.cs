using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

using PKHeX.Core;
using static PKHeX.Core.LegalityCheckStrings;
using System.IO;

namespace AutoLegalityMod
{
    public class BruteForce
    {
        private bool requestedShiny;
        public SaveFile SAV;
        private bool legalized;

        private static readonly SimpleTrainerInfo DefaultTrainer = new SimpleTrainerInfo();

        /// <summary>
        /// Try to generate every a legal PKM from a showdown set using bruteforce. This should generally never be needed.
        /// </summary>
        /// <param name="Set">Rough PKM Set</param>
        /// <param name="SSet">Showdown Set</param>
        /// <param name="resetForm">boolean to reset form back to base form</param>
        /// <returns>PKM legalized via bruteforce</returns>
        public PKM LoadShowdownSetModded_PKSM(PKM Set, ShowdownSet SSet, bool resetForm = false, SimpleTrainerInfo trainer = null)
        {
            if (trainer == null)
                trainer = DefaultTrainer;
            List<List<string>> evoChart = GenerateEvoLists2();
            int abilitynum = Set.AbilityNumber < 6 ? Set.AbilityNumber >> 1 : 0;
            if (resetForm)
            {
                Set.AltForm = 0;
                Set.RefreshAbility(Set.AbilityNumber < 6 ? Set.AbilityNumber >> 1 : 0);
            }
            if (Set.Species == 774 && Set.AltForm == 0) Set.AltForm = 7; // Minior has to be C-Red and not M-Red outside of battle
            bool shiny = Set.IsShiny;
            requestedShiny = SSet.Shiny;

            bool legendary = BruteTables.Legendaries.Contains(Set.Species);
            bool eventMon = BruteTables.EventSpecies.Contains(Set.Species);

            // Egg based pokemon
            if (!legendary && !eventMon)
            {
                foreach (var game in BruteTables.GameVersionList)
                {
                    if (Set.DebutGeneration > game.GetGeneration())
                        continue;
                    Set.Version = (int)game;
                    Set.RestoreIVs(SSet.IVs); // Restore IVs to SSet and HT to false
                    Set.Language = 2;
                    Set.OT_Name = trainer.OT;
                    Set.TID = trainer.TID;
                    Set.SID = trainer.SID;
                    Set.OT_Gender = trainer.Gender;
                    Set.MetDate = DateTime.Today;
                    Set.EggMetDate = DateTime.Today;
                    Set.Egg_Location = Set.Version < (int)GameVersion.W ? 2002 : 60002;

                    Set.Met_Level = 1;
                    Set.ConsoleRegion = 2;

                    if (Set.Version == (int)GameVersion.RD || Set.Version == (int)GameVersion.BU || Set.Version == (int)GameVersion.YW || Set.Version == (int)GameVersion.GN)
                    {
                        Set.SID = 0;
                        Set.Met_Location = 30013;
                        Set.Met_Level = 100;
                    }
                    if (Set.Version == (int)GameVersion.CXD)
                    {
                        Set.Met_Location = 30001;
                        Set.Met_Level = 100;
                    }
                    else { Set = ClickMetLocationModPKSM(Set); }
                    if (Set.GenNumber > 4) Set.Met_Level = 1;
                    Set.SetMarkings();
                    try
                    {
                        Set.CurrentHandler = 1;
                        Set.HT_Name = "Archit";
                        Set = SetSuggestedRelearnMoves_PKSM(Set);
                        Set.SetPIDNature(Set.Nature);
                        if (shiny) Set.SetShiny();
                        if (Set.PID == 0)
                        {
                            Set.PID = PKX.GetRandomPID(Set.Species, Set.Gender, Set.Version, Set.Nature, Set.Format, (uint)(Set.AbilityNumber * 0x10001));
                            if (shiny) Set.SetShiny();
                        }
                        Set.FixMemoriesPKM();
                        if (Set.GenNumber < 6) Set.EncryptionConstant = Set.PID;
                        if (CommonErrorHandling2(Set))
                        {
                            Set.HyperTrain();
                            if (shiny && !Set.IsShiny) Set.SetShiny();
                            return Set;
                        }
                        Set.HyperTrain();
                        if (new LegalityAnalysis(Set).Valid) legalized = true;
                        if (Set.GenNumber < 6 && !legalized) Set.EncryptionConstant = Set.PID;
                        if (new LegalityAnalysis(Set).Valid && SAV.Generation >= Set.GenNumber)
                        {
                            Set.SetHappiness();
                            Set.SetBelugaValues();
                            if (shiny && !Set.IsShiny) Set.SetShinySID();
                            return Set;
                        }
                        else
                        {
                            LegalityAnalysis la = new LegalityAnalysis(Set);
                            Console.WriteLine(la.Report());
                        }
                    }
                    catch { }
                }
            }

            if (!new LegalityAnalysis(Set).Valid && !eventMon)
            {
                foreach (GameVersion game in BruteTables.GameVersionList)
                {
                    if (Set.DebutGeneration > game.GetGeneration()) continue;
                    if (Set.Met_Level == 100) Set.Met_Level = 0;
                    Set.SetBelugaValues();
                    Set.WasEgg = false;
                    Set.EggMetDate = null;
                    Set.Egg_Location = 0;
                    Set.Version = (int)game;
                    Set.RestoreIVs(SSet.IVs); // Restore IVs to SSet and HT to false
                    Set.Language = 2;
                    Set.ConsoleRegion = 2;
                    Set.OT_Name = trainer.OT;
                    Set.TID = trainer.TID;
                    Set.SID = trainer.SID;
                    Set.OT_Gender = trainer.Gender;

                    if (BruteTables.UltraBeastBall.Contains(Set.Species))
                        Set.Ball = (int)Ball.Beast;

                    if (game == GameVersion.RD || game == GameVersion.BU || game == GameVersion.YW || game == GameVersion.GN || game == GameVersion.GD || game == GameVersion.SV || game == GameVersion.C)
                    {
                        Set.SID = 0;
                        if (Set.OT_Name.Length > 6)
                            Set.OT_Name = "ARCH";
                    }
                    Set.MetDate = DateTime.Today;
                    Set.SetMarkings();
                    try
                    {
                        Set.ClearRelearnMoves();
                        switch (game)
                        {
                            case GameVersion.RD:
                            case GameVersion.BU:
                            case GameVersion.YW:
                            case GameVersion.GN:
                                Set.Met_Location = 30013;
                                Set.Met_Level = 100;
                                break;
                            case GameVersion.GD:
                            case GameVersion.SV:
                            case GameVersion.C:
                                Set.Met_Location = 30017;
                                Set.Met_Level = 100;
                                break;
                            case GameVersion.CXD:
                                Set.Met_Location = 30001;
                                Set.Met_Level = 100;
                                break;
                            default:
                                ClickMetLocationModPKSM(Set);
                                break;
                        }
                        Set = SetSuggestedRelearnMoves_PKSM(Set);
                        Set.CurrentHandler = 1;
                        Set.HT_Name = "Archit";
                        Set.PID = PKX.GetRandomPID(Set.Species, Set.Gender, Set.Version, Set.Nature, Set.Format, (uint)(Set.AbilityNumber * 0x10001));
                        if (shiny) Set.SetShiny();
                        if (Set.PID == 0)
                        {
                            Set.PID = PKX.GetRandomPID(Set.Species, Set.Gender, Set.Version, Set.Nature, Set.Format, (uint)(Set.AbilityNumber * 0x10001));
                            if (shiny)
                                Set.SetShiny();
                        }

                        Set.RefreshAbility(abilitynum);
                        Set.FixMemoriesPKM();
                        if (Set.GenNumber < 6)
                            Set.EncryptionConstant = Set.PID;

                        if (CommonErrorHandling2(Set))
                        {
                            Set.HyperTrain();
                            if (shiny) Set.SetShiny();
                            return Set;
                        }

                        Set.HyperTrain();
                        if (new LegalityAnalysis(Set).Valid)
                            legalized = true;

                        AlternateAbilityRefresh(Set);
                        if (Set.GenNumber < 6 && !legalized)
                            Set.EncryptionConstant = Set.PID;

                        if (new LegalityAnalysis(Set).Valid && SAV.Generation >= Set.GenNumber)
                        {
                            Set.SetHappiness();
                            PKM returnval = Set;
                            if (shiny && Set.IsShiny)
                                return Set;
                            if (!requestedShiny || Set.IsShiny)
                                return returnval;

                            Set.SetShinySID();
                            if (new LegalityAnalysis(Set).Valid)
                                return Set;

                            Set = returnval;
                            Set.SetShiny();

                            if (new LegalityAnalysis(Set).Valid)
                                return Set;
                        }
                        else
                        {
                            var edgeLegality = EdgeMons(game, Set);
                            foreach (EncounterStatic el in edgeLegality)
                            {
                                Set.Met_Location = el.Location;
                                Set.Met_Level = el.Level;
                                Set.CurrentLevel = 100;
                                Set.FatefulEncounter = el.Fateful;
                                if (el.RibbonWishing)
                                    ReflectUtil.SetValue(Set, "RibbonWishing", -1);
                                Set.RelearnMoves = el.Relearn;

                                if (SSet.Shiny && (el.Shiny == Shiny.Always || el.Shiny == Shiny.Random))
                                    Set.SetShiny();
                                else if (el.Shiny == Shiny.Never && Set.IsShiny)
                                    Set.PID ^= 0x10000000;
                                else
                                    Set.SetPIDGender(Set.Gender);
                            }

                            LegalityAnalysis la = new LegalityAnalysis(Set);
                            if (la.Valid)
                                return Set;
                            Console.WriteLine(la.Report());
                        }
                    }
                    catch { }
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
                foreach (string file in Directory.GetFiles(fpath, "*.*", SearchOption.AllDirectories))
                {
                    foreach (string mon in chain)
                    {
                        if (file.IndexOf(mon, StringComparison.OrdinalIgnoreCase) >= 0 || Path.GetExtension(file) == ".pl6")
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
                    if (Path.GetExtension(file) == ".wc7" || Path.GetExtension(file) == ".wc7full")
                    {
                        var mg = (WC7)MysteryGift.GetMysteryGift(File.ReadAllBytes(file), Path.GetExtension(file));
                        PIDType = (int)mg.PIDType;
                        AbilityType = mg.AbilityType;
                        Generation = 7;
                        fixedPID = mg.PID;
                        if (!ValidShiny((int)mg.PIDType, shiny)) continue;
                        var temp = mg.ConvertToPKM(SAV);
                        eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out _);
                    }
                    else if (Path.GetExtension(file) == ".wc6" || Path.GetExtension(file) == ".wc6full")
                    {
                        var mg = (WC6)MysteryGift.GetMysteryGift(File.ReadAllBytes(file), Path.GetExtension(file));
                        PIDType = (int)mg.PIDType;
                        AbilityType = mg.AbilityType;
                        Generation = 6;
                        fixedPID = mg.PID;
                        if (!ValidShiny((int)mg.PIDType, shiny)) continue;
                        var temp = mg.ConvertToPKM(SAV);
                        eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out _);
                    }
                    else if (Path.GetExtension(file) == ".pl6") // Pokemon Link
                    {
                        PL6_PKM[] LinkPokemon = new PL6(File.ReadAllBytes(file)).Pokes;
                        bool ExistsEligible = false;
                        PL6_PKM Eligible = new PL6_PKM();
                        foreach (PL6_PKM i in LinkPokemon)
                        {
                            if (i.Species != Set.Species)
                                continue;

                            Eligible = i;
                            ExistsEligible = true;
                            PIDType = i.PIDType;
                            AbilityType = i.AbilityType;
                            Generation = 6;
                            fixedPID = i.PID;

                            break;
                        }
                        if (ExistsEligible) eventpk = PKMConverter.ConvertToType(ConvertPL6ToPKM(Eligible), SAV.PKMType, out _);
                    }
                    else if (Path.GetExtension(file) == ".pgf")
                    {
                        var mg = (PGF)MysteryGift.GetMysteryGift(File.ReadAllBytes(file), Path.GetExtension(file));
                        PIDType = mg.PIDType;
                        AbilityType = mg.AbilityType;
                        Generation = 5;
                        fixedPID = mg.PID;
                        if (!ValidShiny(mg.PIDType, shiny))
                            continue;

                        var temp = mg.ConvertToPKM(SAV);
                        eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out _);
                    }
                    else if (Path.GetExtension(file) == ".pgt" || Path.GetExtension(file) == ".pcd" || Path.GetExtension(file) == ".wc4")
                    {
                        try
                        {
                            var mg = (PCD)MysteryGift.GetMysteryGift(File.ReadAllBytes(file), Path.GetExtension(file));
                            Generation = 4;
                            if (shiny != mg.IsShiny)
                                continue;

                            var temp = mg.ConvertToPKM(SAV);
                            eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out _);
                            fixedPID = eventpk.PID;
                        }
                        catch
                        {
                            var mg = (PGT)MysteryGift.GetMysteryGift(File.ReadAllBytes(file), Path.GetExtension(file));
                            Generation = 4;
                            if (shiny != mg.IsShiny)
                                continue;

                            var temp = mg.ConvertToPKM(SAV);
                            eventpk = PKMConverter.ConvertToType(temp, SAV.PKMType, out _);
                            fixedPID = eventpk.PID;
                        }
                    }
                    else if (Path.GetExtension(file) == ".pk3")
                    {
                        Generation = 3;
                        var pk = PKMConverter.GetPKMfromBytes(File.ReadAllBytes(file), prefer: Path.GetExtension(file).Length > 0 ? (Path.GetExtension(file).Last() - '0') & 0xF : SAV.Generation);
                        if (pk == null)
                            break;

                        eventpk = PKMConverter.ConvertToType(pk, SAV.PKMType, out _);
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
                        if ((PIDType == 0 && eventpk.IsShiny && !shiny && Generation > 4) || (PIDType == 0 && !eventpk.IsShiny && shiny && Generation > 4))
                            continue;

                        if (shiny && !eventpk.IsShiny && Generation > 4)
                        {
                            if (PIDType == 1)
                                eventpk.SetShiny();
                            else if (PIDType == 3)
                                continue;
                        }
                        if (!shiny && eventpk.IsShiny && Generation > 4)
                        {
                            if (PIDType == 1) eventpk.PID ^= 0x10000000;
                            else if (PIDType == 2)
                                continue;
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

                        eventpk.SetMarkings();
                        eventpk.SetHappiness();
                        eventpk.HyperTrain();

                        if (new LegalityAnalysis(eventpk).Valid) return eventpk;

                        SetWCXPID(eventpk, PIDType, Generation, AbilityType);
                        LegalityAnalysis la2 = new LegalityAnalysis(eventpk);
                        if (!la2.Valid)
                        {
                            Console.WriteLine(la2.Report());
                            AlternateAbilityRefresh(eventpk);

                            if (new LegalityAnalysis(eventpk).Valid)
                                return eventpk;
                            if (EventErrorHandling(eventpk, Generation, fixedPID))
                                return eventpk;

                            prevevent = eventpk;
                            continue;
                        }
                        return eventpk;
                    }
                    catch { }
                }
                Set = prevevent;
            }
            return Set;
        }

        private static List<EncounterStatic> EdgeMons(GameVersion Game, PKM pk)
        {
            var edgecasearray = GetEdgeCaseArray(Game);
            return new List<EncounterStatic>(edgecasearray.Where(e => e.Species == pk.Species));
        }

        private static EncounterStatic[] GetEdgeCaseArray(GameVersion Game)
        {
            switch (Game)
            {
                case GameVersion.B:
                case GameVersion.W:
                    return EdgeCaseLegality.BWEntreeForest;
                case GameVersion.B2:
                case GameVersion.W2:
                    return EdgeCaseLegality.B2W2EntreeForest;
                case GameVersion.US:
                case GameVersion.UM:
                    return EdgeCaseLegality.USUMEdgeEnc;
            }

            return new EncounterStatic[] { };
        }

        private bool EventErrorHandling(PKM pk, int Generation, uint fixedPID)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            var report = la.Report();
            Console.WriteLine(fixedPID);
            if (pk.Species == 658 && pk.Ability == 210) // Ash-Greninja Fix
            {
                pk.Version = (int)GameVersion.SN;
                pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
            }
            if (report.Contains(string.Format(LMemoryFeelInvalid, "OT")) || report.Contains(string.Format(LMemoryFeelInvalid, "HT"))) //V255 = {0} Memory: Invalid Feeling (0 = OT/HT)
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
                report = GetReport(pk);
            }
            if (report.Contains(LNickMatchLanguageFail)) // V20: Nickname does not match species name
            {
                pk.IsNicknamed = false;
                pk.Nickname = PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, Generation);
                report = GetReport(pk);
            }
            if (report.Contains(LEncGiftPIDMismatch)) // V410 = Mystery Gift fixed PID mismatch.
            {
                pk.PID = fixedPID;
                report = GetReport(pk);
            }
            if (report.Contains(LPIDTypeMismatch)) // V411 = Encounter type PID mismatch
            {
                if ((UsesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.FR) || (UsesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.LG))
                {
                    bool shiny = pk.IsShiny;
                    pk = M2EventFix(pk, shiny);
                    if (requestedShiny && !pk.IsShiny)
                        pk.SetShinySID();
                    report = GetReport(pk);
                }

                if (UsesEventBasedMethod(pk.Species, pk.Moves, "BACD_R") && pk.Version == (int)GameVersion.R)
                {
                    pk = BACD_REventFix(pk, pk.IsShiny);
                    if (requestedShiny && !pk.IsShiny)
                        pk.SetShinySID(); // Make wrong requests fail
                    report = GetReport(pk);
                }
            }
            if (new LegalityAnalysis(pk).Valid) return true;
            Console.WriteLine(report);
            return false;
        }

        private static void SetWCXPID(PKM pk, int PIDType, int Generation, int AbilityType)
        {
            switch (Generation)
            {
                case 6:
                case 7:
                    SetWC6PID(pk, PIDType);
                    return;
                case 5:
                    SetWC5PID(pk, PIDType, AbilityType);
                    return;
                case 4:
                    SetWC4PID(pk);
                    return;
            }
        }

        private static void SetWC4PID(PKM pk)
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
            if (new LegalityAnalysis(pk).Valid)
                return;

            Console.WriteLine(new LegalityAnalysis(pk).Report());
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
        }

        private static void SetWC6PID(PKM pk, int PIDType)
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
        }

        private static void SetWC5PID(PKM pk, int PIDType, int AbilityType)
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
            {
                pk.PID = pk.PID;
                return;
            }

            pk.PID = Util.Rand32();

            // Force Gender
            do { pk.PID = (pk.PID & 0xFFFFFF00) | (Util.Rand32() & 0xFF); }
            while (!pk.IsGenderValid());

            // Force Ability
            if (av == 1)
                pk.PID |= 0x10000;
            else pk.PID &= 0xFFFEFFFF;

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

        private static bool ValidShiny(int PIDType, bool shiny)
        {
            if ((PIDType == 0 && shiny) || (PIDType == 1 && shiny) || (PIDType == 2 && shiny)) return true;
            if ((PIDType == 0 && !shiny) || (PIDType == 1 && !shiny) || (PIDType == 3 && !shiny)) return true;
            return false;
        }

        private static PKM SetSuggestedRelearnMoves_PKSM(PKM Set)
        {
            Set.ClearRelearnMoves();
            LegalityAnalysis Legality = new LegalityAnalysis(Set);
            if (Set.Format < 6)
                return Set;

            int[] m = Legality.GetSuggestedRelearn();
            if (m.All(z => z == 0))
            {
                if (!Set.WasEgg && !Set.WasEvent && !Set.WasEventEgg && !Set.WasLink)
                {
                    if (Set.Version != (int)GameVersion.CXD)
                    {
                        var encounter = Legality.GetSuggestedMetInfo();
                        if (encounter != null)
                            m = encounter.Relearn;
                    }
                }
            }

            if (Set.RelearnMoves.SequenceEqual(m))
                return Set;
            if (m.Length > 3)
                Set.RelearnMoves = m;
            return Set;
        }

        private static void AlternateAbilityRefresh(PKM pk)
        {
            int abilityID = pk.Ability;
            int finalabilitynum = pk.AbilityNumber;
            int[] abilityNumList = { 1, 2, 4 };
            for (int i = 0; i < 3; i++)
            {
                pk.AbilityNumber = abilityNumList[i];
                pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                if (pk.Ability == abilityID)
                {
                    LegalityAnalysis recheckLA = new LegalityAnalysis(pk);
                    var updatedReport = recheckLA.Report();
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

        public bool CommonErrorHandling2(PKM pk)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            var report = la.Report();

            // fucking M2
            if ((UsesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.FR) || (UsesEventBasedMethod(pk.Species, pk.Moves, "M2") && pk.Version == (int)GameVersion.LG))
            {
                pk = M2EventFix(pk, pk.IsShiny);
                report = GetReport(pk);
            }

            if (UsesEventBasedMethod(pk.Species, pk.Moves, "BACD_R") && pk.Version == (int)GameVersion.R)
            {
                pk = BACD_REventFix(pk, pk.IsShiny);
                report = GetReport(pk);
            }

            if (report.Contains(LNickMatchLanguageFail))
            {
                pk.Nickname = PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, SAV.Generation); // failsafe to reset nick
                report = GetReport(pk);
            }
            if (report.Contains(LStatIncorrectCP))
            {
                ((PB7)pk).ResetCP();
                report = GetReport(pk);
            }
            if (report.Contains(LAbilityMismatch)) //V223 = Ability mismatch for encounter.
            {
                pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                report = GetReport(pk);
                if (report.Contains(LAbilityMismatch)) //V223 = Ability mismatch for encounter.
                {
                    AlternateAbilityRefresh(pk);
                }
                report = GetReport(pk);
            }
            if (report.Contains(LTransferEggLocationTransporter)) //V61 = Invalid Met Location, expected Transporter.
            {
                pk.Met_Location = 30001;
                report = GetReport(pk);
            }
            if (report.Contains(LBallEncMismatch)) //V118 = Can't have ball for encounter type.
            {
                if (pk.B2W2)
                {
                    pk.Ball = 25; //Dream Ball
                    report = GetReport(pk);
                }
                else
                {
                    pk.Ball = 0;
                    report = GetReport(pk);
                }
            }
            if (report.Contains(LEncUnreleasedEMewJP)) //V353 = Non japanese Mew from Faraway Island. Unreleased event.
            {
                bool shiny = pk.IsShiny;
                pk.Language = 1;
                pk.FatefulEncounter = true;
                pk.Nickname = PKX.GetSpeciesNameGeneration(pk.Species, pk.Language, 3);
                pk.PID = PKX.GetRandomPID(pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (shiny) pk.SetShinySID();
                report = GetReport(pk);
            }
            if (report.Contains(LPIDEqualsEC)) //V208 = Encryption Constant matches PID.
            {
                int wIndex = Array.IndexOf(Legal.WurmpleEvolutions, pk.Species);
                uint EC = wIndex < 0 ? Util.Rand32() : PKX.GetWurmpleEC(wIndex / 2);
                pk.EncryptionConstant = EC;
                report = GetReport(pk);
            }
            if (report.Contains(LTransferPIDECEquals)) //V216 = PID should be equal to EC!
            {
                pk.EncryptionConstant = pk.PID;
                report = GetReport(pk);
            }
            if (report.Contains(LTransferPIDECBitFlip)) //V215 = PID should be equal to EC [with top bit flipped]!
            {
                pk.PID = PKX.GetRandomPID(pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (pk.IsShiny) pk.SetShiny();
                report = GetReport(pk);
            }
            if (report.Contains(LPIDGenderMismatch)) //V251 = PID-Gender mismatch.
            {
                pk.Gender = pk.Gender == 0 ? 1 : 0;
                report = GetReport(pk);
            }
            if (report.Contains(LG3OTGender) || report.Contains(LG1OTGender)) //V407 = OT from Colosseum/XD cannot be female. V408 = Female OT from Generation 1 / 2 is invalid.
            {
                pk.OT_Gender = 0;
                report = GetReport(pk);
            }
            if (report.Contains(LLevelMetBelow)) //V85 = Current level is below met level.
            {
                pk.CurrentLevel = 100;
                report = GetReport(pk);
            }
            if (report.Contains(string.Format(LRibbonFMissing_0, "National"))) //V600 = Missing Ribbons: {0} (National in this case)
            {
                ReflectUtil.SetValue(pk, "RibbonNational", -1);
                report = GetReport(pk);
            }
            if (report.Contains(string.Format(LRibbonFInvalid_0, "National"))) //V601 = Invalid Ribbons: {0} (National in this case)
            {
                ReflectUtil.SetValue(pk, "RibbonNational", 0);
                report = GetReport(pk);
            }
            if (report.Contains(LOTLong)) //V38 = OT Name too long.
            {
                pk.OT_Name = "ARCH";
                report = GetReport(pk);
            }
            if (report.Contains(LG1CharOT)) //V421 = OT from Generation 1/2 uses unavailable characters.
            {
                pk.OT_Name = "ARCH";
                report = GetReport(pk);
            }
            if (report.Contains(LGeoNoCountryHT))
            {
                var g = (IGeoTrack) pk;
                g.Geo1_Country = 1;
                report = GetReport(pk);
            }
            if (report.Contains(LMemoryMissingHT)) //V150 = Memory: Handling Trainer Memory missing.
            {
                pk.HT_Memory = 3;
                pk.HT_TextVar = 9;
                pk.HT_Intensity = 1;
                pk.HT_Feeling = Util.Rand.Next(0, 10); // 0-9
                pk.HT_Friendship = pk.OT_Friendship;
                report = GetReport(pk);
            }
            if (report.Contains(LMemoryMissingOT)) //V152 = Memory: Original Trainer Memory missing.
            {
                pk.OT_Memory = 3;
                pk.OT_TextVar = 9;
                pk.OT_Intensity = 1;
                pk.OT_Feeling = Util.Rand.Next(0, 10); // 0-9
                report = GetReport(pk);
            }
            if (report.Contains(string.Format(LMemoryFeelInvalid, "OT")) || report.Contains(string.Format(LMemoryFeelInvalid, "HT"))) //V255 = {0} Memory: Invalid Feeling (0 = OT/HT)
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
                report = GetReport(pk);
            }
            if (report.Contains(LMemoryIndexIDOT0)) //V130 = Can't have any OT Memory.
            {
                pk.OT_Memory = 0;
                pk.OT_TextVar = 0;
                pk.OT_Intensity = 0;
                pk.OT_Feeling = 0;
                report = GetReport(pk);
            }
            if (report.Contains(LGeoMemoryMissing)) //V137 = GeoLocation Memory: Memories should be present.
            {
                var g = (IGeoTrack) pk;
                g.Geo1_Country = 1;
                report = GetReport(pk);
            }
            if (report.Contains(LBallEncMismatch)) //V118 = Can't have ball for encounter type.
            {
                pk.Ball = 4;
                report = GetReport(pk);
            }
            if (report.Contains(LGeoHardwareInvalid)) //Geolocation: Country is not in 3DS region.
            {
                pk.Country = 0;
                pk.Region = 0;
                pk.ConsoleRegion = 2;
                report = GetReport(pk);
            }
            if (report.Contains(LFormBattle)) //V310 = Form cannot exist outside of a battle.
            {
                if (pk.Species == 718 && pk.Ability == 211) pk.AltForm = 3; // Zygarde Edge case
                else pk.AltForm = 0;
                report = GetReport(pk);
            }
            if (report.Contains(LFatefulMissing)) //V324 = Special ingame Fateful Encounter flag missing.
            {
                pk.FatefulEncounter = true;
                report = GetReport(pk);
            }
            if (report.Contains(LFatefulInvalid)) //V325 = Fateful Encounter should not be checked.

            {
                pk.FatefulEncounter = false;
                report = GetReport(pk);
            }
            if (report.Contains(LEncTypeMismatch)) //V381 = Encounter Type does not match encounter.
            {
                IEncounterable EncounterMatch = new LegalityAnalysis(pk).Info.EncounterMatch;
                EncounterType type = EncounterType.None;
                // Encounter type data is only stored for gen 4 encounters
                // All eggs have encounter type none, even if they are from static encounters
                if (pk.Gen4 && !pk.WasEgg)
                {
                    // If there is more than one slot, the get wild encounter have filter for the pkm type encounter like safari/sports ball
                    if (EncounterMatch is EncounterSlot w)
                        type = w.TypeEncounter;

                    if (EncounterMatch is EncounterStaticTyped s)
                        type = s.TypeEncounter;
                }

                if (!type.Contains(pk.EncounterType))
                    pk.EncounterType = Convert.ToInt32(Math.Log((int)type, 2));
                else
                    Console.WriteLine("This should never happen");
                report = GetReport(pk);
            }
            if (report.Contains(LEvoInvalid)) //V86 = Evolution not valid (or level/trade evolution unsatisfied).
            {
                pk.Met_Level--;
                report = GetReport(pk);
            }
            if (report.Contains(LPIDTypeMismatch)) //V411 = Encounter Type PID mismatch.
            {
                SetPIDSID(pk, pk.IsShiny, pk.Version == (int)GameVersion.CXD);

                if (new LegalityAnalysis(pk).Valid)
                    return false;

                report = GetReport(pk);
                if (report.Equals(LPIDTypeMismatch)) // V411 = Encounter Type PID mismatch.
                    return true;

                if (report.Contains(LPIDGenderMismatch)) // V251 = PID-Gender mismatch.
                {
                    pk.Gender = pk.Gender == 0 ? 1 : 0;
                    report = GetReport(pk);
                    if (new LegalityAnalysis(pk).Valid)
                        return false;
                }
            }
            if (report.Contains(LHyperPerfectAll)) // V41 = Can't Hyper Train a Pokémon with perfect IVs.
            {
                ((IHyperTrain)pk).HyperTrainClear();
                report = GetReport(pk);
            }
            if (report.Contains(LHyperPerfectOne)) // V42 = Can't Hyper Train a perfect IV.
            {
                if (!(pk is IHyperTrain h))
                    return false;
                if (pk.IV_HP == 31) h.HT_HP = false;
                if (pk.IV_ATK == 31) h.HT_ATK = false;
                if (pk.IV_DEF == 31) h.HT_DEF = false;
                if (pk.IV_SPA == 31) h.HT_SPA = false;
                if (pk.IV_SPD == 31) h.HT_SPD = false;
                if (pk.IV_SPE == 31) h.HT_SPE = false;
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

        private static string GetReport(PKM pk)
        {
            LegalityAnalysis la = new LegalityAnalysis(pk);
            return la.Report();
        }

        private void SetPIDSID(PKM pk, bool shiny, bool XD = false)
        {
            uint hp = (uint)pk.IV_HP;
            uint atk = (uint)pk.IV_ATK;
            uint def = (uint)pk.IV_DEF;
            uint spa = (uint)pk.IV_SPA;
            uint spd = (uint)pk.IV_SPD;
            uint spe = (uint)pk.IV_SPE;
            uint nature = (uint)pk.Nature;
            bool pidsidmethod = true;
            string[] pidsid;
            if (XD)
            {
                pidsid = Misc.IVtoPIDGenerator.XDPID(hp, atk, def, spa, spd, spe, nature, 0);
            }
            else
            {
                pidsid = Misc.IVtoPIDGenerator.M1PID(hp, atk, def, spa, spd, spe, nature, 0);
            }

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
            string updatedReport = recheckLA.Report();
            Console.WriteLine(updatedReport);
            if (updatedReport.Contains("Invalid: Encounter Type PID mismatch."))
            {
                string[] hpower = { "fighting", "flying", "poison", "ground", "rock", "bug", "ghost", "steel", "fire", "water", "grass", "electric", "psychic", "ice", "dragon", "dark" };
                string hiddenpower = hpower[pk.HPType];
                string[] NatureHPIVs = Misc.IVtoPIDGenerator.GetIVPID(nature, hiddenpower, XD);
                Console.WriteLine(XD);
                pk.PID = Util.GetHexValue(NatureHPIVs[0]);
                if (pk.GenNumber < 5)
                    pk.EncryptionConstant = pk.PID;

                Console.WriteLine(NatureHPIVs[0]);
                pk.IV_HP = Convert.ToInt32(NatureHPIVs[1]);
                pk.IV_ATK = Convert.ToInt32(NatureHPIVs[2]);
                pk.IV_DEF = Convert.ToInt32(NatureHPIVs[3]);
                pk.IV_SPA = Convert.ToInt32(NatureHPIVs[4]);
                pk.IV_SPD = Convert.ToInt32(NatureHPIVs[5]);
                pk.IV_SPE = Convert.ToInt32(NatureHPIVs[6]);
                if (shiny) pk.SetShinySID();
                recheckLA = new LegalityAnalysis(pk);
                updatedReport = recheckLA.Report();
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
                updatedReport = recheckLA.Report();
                if (updatedReport.Contains("PID-Gender mismatch."))
                {
                    pk.Gender = pk.Gender == 0 ? 1 : 0;
                    LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                    updatedReport = recheckLA2.Report();
                }
                if (updatedReport.Contains("Can't Hyper Train a Pokémon that isn't level 100."))
                {
                    pk.CurrentLevel = 100;
                    LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                    updatedReport = recheckLA2.Report();
                }
                LegalityAnalysis Legality = new LegalityAnalysis(pk);
                if (Legality.Valid)
                    return;
                // Fix Moves if a slot is empty
                pk.FixMoves();

                // PKX is now filled
                pk.RefreshChecksum();
                pk.RefreshAbility(pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                if (updatedReport.Contains("Invalid: Encounter Type PID mismatch.") || UsesEventBasedMethod(pk.Species, pk.Moves, "M2"))
                {
                    if (pk.GenNumber == 3 || UsesEventBasedMethod(pk.Species, pk.Moves, "M2"))
                    {
                        pk = M2EventFix(pk, shiny);
                        if (!new LegalityAnalysis(pk).Report().Contains("PID mismatch") || UsesEventBasedMethod(pk.Species, pk.Moves, "M2"))
                            return;
                    }
                    pk.IV_HP = (int)hp;
                    pk.IV_ATK = (int)atk;
                    pk.IV_DEF = (int)def;
                    pk.IV_SPA = (int)spa;
                    pk.IV_SPD = (int)spd;
                    pk.IV_SPE = (int)spe;
                }
            }
        }

        private PKM M2EventFix(PKM pk, bool shiny)
        {
            int eggloc = pk.Egg_Location;
            bool feFlag = pk.FatefulEncounter;
            pk.Egg_Location = 0;
            pk.FatefulEncounter = true;
            string[] hpower = { "fighting", "flying", "poison", "ground", "rock", "bug", "ghost", "steel", "fire", "water", "grass", "electric", "psychic", "ice", "dragon", "dark" };
            string hiddenpower = hpower[pk.HPType];
            string[] NatureHPIVs = Misc.IVtoPIDGenerator.GetIVPID((uint)pk.Nature, hiddenpower, false, "M2");
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
            string updatedReport = recheckLA.Report();
            if (updatedReport.Contains("PID-Gender mismatch"))
            {
                pk.Gender = pk.Gender == 0 ? 1 : 0;
                LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                updatedReport = recheckLA2.Report();
            }
            if (!updatedReport.Contains("PID mismatch") || UsesEventBasedMethod(pk.Species, pk.Moves, "M2")) return pk;
            Console.WriteLine(GetReport(pk));
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
            string[] NatureHPIVs = Misc.IVtoPIDGenerator.GetIVPID((uint)pk.Nature, hiddenpower, false, "BACD_R");
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
            string updatedReport = recheckLA.Report();
            if (updatedReport.Contains("PID-Gender mismatch"))
            {
                pk.Gender = pk.Gender == 0 ? 1 : 0;
                LegalityAnalysis recheckLA2 = new LegalityAnalysis(pk);
                updatedReport = recheckLA2.Report();
            }
            if (!updatedReport.Contains("PID mismatch") || UsesEventBasedMethod(pk.Species, pk.Moves, "BACD_R")) return pk;
            Console.WriteLine(GetReport(pk));
            pk.FatefulEncounter = feFlag;
            pk.Egg_Location = eggloc;
            return pk;
        }

        public bool UsesEventBasedMethod(int Species, int[] Moves, string method)
        {
            var index = GetRNGListIndex(method);
            if (index == -1)
                return false;

            var RNGList = BruteTables.WC3RNGList[index];
            if (!RNGList.Keys.Contains(Species))
                return false;

            return Moves.Any(i => RNGList[Species].Contains(i));
        }

        private static int GetRNGListIndex(string Method)
        {
            switch (Method)
            {
                case "M2":
                    return 0;
                case "BACD_R":
                    return 1;
                default:
                    return -1;
            }
        }

        private static PKM ClickMetLocationModPKSM(PKM p)
        {
            LegalityAnalysis Legality = new LegalityAnalysis(p);

            var encounter = Legality.GetSuggestedMetInfo();
            if (encounter == null || (p.Format >= 3 && encounter.Location < 0))
                return p;

            int level = encounter.Level;
            int location = encounter.Location;
            int minlvl = Legal.GetLowestLevel(p, encounter.Species);
            if (minlvl == 0)
                minlvl = level;

            if (p.CurrentLevel >= minlvl && p.Met_Level == level && p.Met_Location == location)
                return p;
            if (minlvl < level)
                level = minlvl;
            p.Met_Location = location;
            p.Met_Level = level;
            return p;
        }

        private static List<List<string>> GenerateEvoLists2()
        {
            int counter = 0;
            string line;
            List<List<string>> evoList = new List<List<string>>();
            List<string> blankList = new List<string>();
            var assembly = Assembly.GetExecutingAssembly();
            const string resourceName = "AutoLegalityMod.Resources.txt.evolutions.txt";
            var stream = assembly.GetManifestResourceStream(resourceName);
            StreamReader file = new StreamReader(stream);
            while ((line = file.ReadLine()) != null)
            {
                var trim = line.Trim();
                if (trim.Length == 0)
                {
                    evoList.Add(blankList);
                    blankList = new List<string>();
                }
                else
                {
                    blankList.Add(trim);
                }
                counter++;
            }
            file.Close();
            return evoList;
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

                EXP = PKX.GetEXP(pk.Level, pk.Species, pk.Form),

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

            if (eventpk.Format > 6 && pk.OriginGame == 0) // Gen7
                eventpk.Version = (int) GameVersion.OR;

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
            {
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
            }

            eventpk.IsNicknamed = false;
            eventpk.Nickname = eventpk.IsNicknamed ? eventpk.Nickname : PKX.GetSpeciesNameGeneration(pk.Species, eventpk.Language, eventpk.Format);
            eventpk.CurrentFriendship = eventpk.IsEgg ? pi.HatchCycles : pi.BaseFriendship;

            eventpk.RefreshChecksum();
            return eventpk;
        }
    }
}