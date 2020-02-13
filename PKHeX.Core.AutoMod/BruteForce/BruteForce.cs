using System;
using System.Linq;

using static PKHeX.Core.LegalityCheckStrings;
using System.Diagnostics;
using RNGReporter;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Logic for forcing an existing <see cref="PKM"/> to be legal.
    /// </summary>
    public static class BruteForce
    {
        private static readonly ITrainerInfo DefaultTrainer = new SimpleTrainerInfo();

        /// <summary>
        /// Try to generate every a legal PKM from a showdown set using bruteforce. This should generally never be needed.
        /// </summary>
        /// <param name="pk">Rough PKM Set</param>
        /// <param name="set">Showdown Set</param>
        /// <param name="resetForm">boolean to reset form back to base form</param>
        /// <param name="trainer">Trainer details to apply (optional)</param>
        /// <returns>PKM legalized via bruteforce</returns>
        public static PKM ApplyDetails(PKM pk, ShowdownSet set, bool resetForm = false, ITrainerInfo? trainer = null)
        {
            if (trainer == null)
                trainer = DefaultTrainer;
            int abilitynum = pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0;
            if (resetForm)
            {
                pk.AltForm = 0;
                pk.RefreshAbility((uint)pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
            }
            if (pk.Species == 774 && pk.AltForm == 0)
                pk.AltForm = 7; // Minior has to be C-Red and not M-Red outside of battle
            bool shiny = pk.IsShiny;

            bool legendary = BruteTables.Legendaries.Contains(pk.Species);
            bool eventMon = BruteTables.EventSpecies.Contains(pk.Species);

            // Egg based pokemon
            if (!legendary && !eventMon)
            {
                if (BruteForceEgg(pk, set, trainer, shiny))
                    return pk;
            }

            if (!new LegalityAnalysis(pk).Valid && !eventMon)
            {
                if (BruteForceNonBreed(pk, set, trainer, shiny, abilitynum))
                    return pk;
            }
            return pk;
        }

        private static bool BruteForceEgg(PKM pk, ShowdownSet set, ITrainerInfo trainer, bool shiny)
        {
            foreach (var game in BruteTables.GameVersionList)
            {
                if (pk.DebutGeneration > game.GetGeneration())
                    continue;
                pk.Version = (int)game;
                pk.RestoreIVs(set.IVs); // Restore IVs to template, and HT to false
                pk.Language = 2;
                pk.OT_Name = trainer.OT;
                pk.TID = trainer.TID;
                pk.SID = trainer.SID;
                pk.OT_Gender = trainer.Gender;
                pk.MetDate = DateTime.Today;
                pk.EggMetDate = DateTime.Today;
                pk.Egg_Location = pk.Version < (int)GameVersion.W ? 2002 : 60002;

                pk.Met_Level = 1;
                pk.ConsoleRegion = 2;

                if (pk.Version == (int)GameVersion.RD || pk.Version == (int)GameVersion.BU || pk.Version == (int)GameVersion.YW || pk.Version == (int)GameVersion.GN)
                {
                    pk.SID = 0;
                    pk.Met_Location = 30013;
                    pk.Met_Level = 100;
                }
                if (pk.Version == (int)GameVersion.CXD)
                {
                    pk.Met_Location = 30001;
                    pk.Met_Level = 100;
                }
                else
                {
                    try
                    {
                        pk.SetSuggestedMetLocation();
                    }
                    catch { }
                }
                if (pk.GenNumber > 4)
                    pk.Met_Level = 1;

                pk.SetMarkings();
                pk.CurrentHandler = 1;
                pk.HT_Name = "Archit";
                try
                {
                    pk.SetSuggestedRelearnMoves();
                }
                catch { }
                pk.SetPIDNature(pk.Nature);
                if (shiny)
                    pk.SetShiny();
                if (pk.PID == 0)
                {
                    pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                    if (shiny)
                        pk.SetShiny();
                }
                pk.SetSuggestedMemories();
                if (pk.GenNumber < 6)
                    pk.EncryptionConstant = pk.PID;
                if (CommonErrorHandling2(pk))
                {
                    pk.HyperTrain();
                    if (shiny && !pk.IsShiny)
                        pk.SetShiny();
                    {
                        return true;
                    }
                }
                pk.HyperTrain();
                bool legalized = new LegalityAnalysis(pk).Valid;
                if (pk.GenNumber < 6 && !legalized)
                    pk.EncryptionConstant = pk.PID;
                if (new LegalityAnalysis(pk).Valid && pk.Format >= pk.GenNumber)
                {
                    pk.SetHappiness(new LegalityAnalysis(pk).EncounterMatch);
                    pk.SetBelugaValues();
                    if (shiny && !pk.IsShiny)
                        pk.SetShinySID();
                    {
                        return true;
                    }
                }
                else
                {
                    var la = new LegalityAnalysis(pk);
                    Debug.WriteLine(la.Report());
                }
            }
            return false;
        }

        private static bool BruteForceNonBreed(PKM pk, ShowdownSet set, ITrainerInfo trainer, bool shiny, int abilitynum)
        {
            foreach (GameVersion game in BruteTables.GameVersionList)
            {
                if (pk.DebutGeneration > game.GetGeneration())
                    continue;
                if (pk.Met_Level == 100)
                    pk.Met_Level = 0;
                pk.SetBelugaValues();
                pk.EggMetDate = null;
                pk.Egg_Location = 0;
                pk.Version = (int)game;
                pk.RestoreIVs(set.IVs); // Restore IVs to template, and HT to false
                pk.Language = 2;
                pk.ConsoleRegion = 2;
                pk.OT_Name = trainer.OT;
                pk.TID = trainer.TID;
                pk.SID = trainer.SID;
                pk.OT_Gender = trainer.Gender;

                if (BruteTables.UltraBeastBall.Contains(pk.Species))
                    pk.Ball = (int)Ball.Beast;

                if (game.GetGeneration() <= 2)
                {
                    pk.SID = 0;
                    if (pk.OT_Name.Length > 6)
                        pk.OT_Name = "ARCH";
                }
                pk.MetDate = DateTime.Today;
                pk.SetMarkings();

                var result = InnerBruteForce(pk, game, shiny, abilitynum, set);
                if (result)
                    return true;
            }
            return false;
        }

        private static bool InnerBruteForce(PKM pk, GameVersion game, bool shiny, int abilitynum, ShowdownSet set)
        {
            pk.ClearRelearnMoves();
            switch (game)
            {
                case GameVersion.RD:
                case GameVersion.BU:
                case GameVersion.YW:
                case GameVersion.GN:
                    pk.Met_Location = 30013;
                    pk.Met_Level = 100;
                    break;
                case GameVersion.GD:
                case GameVersion.SV:
                case GameVersion.C:
                    pk.Met_Location = 30017;
                    pk.Met_Level = 100;
                    break;
                case GameVersion.CXD:
                    pk.Met_Location = 30001;
                    pk.Met_Level = 100;
                    break;
                default:
                    try
                    {
                        pk.SetSuggestedMetLocation();
                    }
                    catch { }
                    break;
            }
            try
            {
                pk.SetSuggestedRelearnMoves();
            }
            catch { }
            pk.CurrentHandler = 1;
            pk.HT_Name = "Archit";
            pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
            if (shiny)
                pk.SetShiny();
            if (pk.PID == 0)
            {
                pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (shiny)
                    pk.SetShiny();
            }

            pk.RefreshAbility(abilitynum);
            pk.SetSuggestedMemories();
            if (pk.GenNumber < 6)
                pk.EncryptionConstant = pk.PID;

            if (CommonErrorHandling2(pk))
            {
                pk.HyperTrain();
                if (shiny)
                    pk.SetShiny();
                return true;
            }

            pk.HyperTrain();
            bool legalized = new LegalityAnalysis(pk).Valid;

            AlternateAbilityRefresh(pk);
            if (pk.GenNumber < 6 && !legalized)
                pk.EncryptionConstant = pk.PID;

            if (new LegalityAnalysis(pk).Valid && pk.Format >= pk.GenNumber)
            {
                pk.SetHappiness(new LegalityAnalysis(pk).EncounterMatch);
                if (shiny && pk.IsShiny)
                    return true;
                if (!set.Shiny || pk.IsShiny)
                    return true;

                pk.SetShinySID();
                if (new LegalityAnalysis(pk).Valid)
                    return true;

                pk.SetShiny();
                if (new LegalityAnalysis(pk).Valid)
                    return true;
            }
            else
            {
                var edge = EncounterMovesetGenerator.GenerateEncounters(pk).OfType<EncounterStatic>();
                foreach (EncounterStatic el in edge)
                {
                    ApplyEncounterAttributes(pk, set, el);
                    var la = new LegalityAnalysis(pk);
                    if (la.Valid)
                        return true;
                    Debug.WriteLine(la.Report());
                }
            }

            return false;
        }

        private static void ApplyEncounterAttributes(PKM pk, ShowdownSet set, EncounterStatic el)
        {
            pk.Met_Location = el.Location;
            pk.Met_Level = el.Level;
            pk.CurrentLevel = 100;
            pk.FatefulEncounter = el.Fateful;
            if (el.RibbonWishing && pk is IRibbonSetEvent4 e4)
                e4.RibbonWishing = true;
            pk.SetRelearnMoves(el.Relearn);

            if (set.Shiny && (el.Shiny == Shiny.Always || el.Shiny == Shiny.Random))
                pk.SetShiny();
            else if (el.Shiny == Shiny.Never && pk.IsShiny)
                pk.PID ^= 0x10000000;
            else
                pk.SetPIDGender(pk.Gender);
        }

        private static void AlternateAbilityRefresh(PKM pk)
        {
            int abilityID = pk.Ability;
            int finalabilitynum = pk.AbilityNumber;
            int[] abilityNumList = { 1, 2, 4 };
            for (int i = 0; i < 3; i++)
            {
                pk.AbilityNumber = abilityNumList[i];
                pk.RefreshAbility((uint)pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
                if (pk.Ability == abilityID)
                {
                    var updatedReport = GetReport(pk);
                    if (!updatedReport.Contains(LAbilityMismatch))
                    {
                        finalabilitynum = pk.AbilityNumber;
                        break;
                    }
                }
            }
            pk.AbilityNumber = finalabilitynum;
            pk.RefreshAbility((uint)pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
        }

        private static bool CommonErrorHandling2(PKM pk)
        {
            var report = GetReport(pk);

            // fucking M2
            if (GameVersion.FRLG.Contains(pk.Version) && UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
            {
                M2EventFix(pk, pk.IsShiny);
                report = GetReport(pk);
            }

            if (UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.BACD_R) && pk.Version == (int)GameVersion.R)
            {
                BACD_REventFix(pk, pk.IsShiny);
                report = GetReport(pk);
            }

            if (report.Contains(LNickMatchLanguageFail))
            {
                pk.Nickname = SpeciesName.GetSpeciesNameGeneration(pk.Species, pk.Language, pk.Format); // failsafe to reset nick
                report = GetReport(pk);
            }
            if (report.Contains(LStatIncorrectCP))
            {
                ((PB7)pk).ResetCP();
                report = GetReport(pk);
            }
            if (report.Contains(LAbilityMismatch)) //V223 = Ability mismatch for encounter.
            {
                pk.RefreshAbility((uint)pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
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
                pk.Nickname = SpeciesName.GetSpeciesNameGeneration(pk.Species, pk.Language, 3);
                pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (shiny)
                    pk.SetShinySID();
                report = GetReport(pk);
            }
            if (report.Contains(LPIDEqualsEC)) //V208 = Encryption Constant matches PID.
            {
                pk.SetRandomEC();
                report = GetReport(pk);
            }
            if (report.Contains(LTransferPIDECEquals)) //V216 = PID should be equal to EC!
            {
                pk.EncryptionConstant = pk.PID;
                report = GetReport(pk);
            }
            if (report.Contains(LTransferPIDECBitFlip)) //V215 = PID should be equal to EC [with top bit flipped]!
            {
                pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                if (pk.IsShiny)
                    pk.SetShiny();
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
                if (pk is IRibbonSetEvent3 e3)
                    e3.RibbonNational = true;
                report = GetReport(pk);
            }
            if (report.Contains(string.Format(LRibbonFInvalid_0, "National"))) //V601 = Invalid Ribbons: {0} (National in this case)
            {
                if (pk is IRibbonSetEvent3 e3)
                    e3.RibbonNational = false;
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
                var g = (IGeoTrack)pk;
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
            if (report.Contains(LGeoMemoryMissing)) //V137 = GeoLocation Memory: Memories should be present.
            {
                var g = (IGeoTrack)pk;
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
                if (pk.Species == 718 && pk.Ability == 211)
                    pk.AltForm = 3; // Zygarde Edge case
                else
                    pk.AltForm = 0;
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
                var match = new LegalityAnalysis(pk).Info.EncounterMatch;
                var type = GetRequiredEncounterType(pk, match);

                if (!type.Contains(pk.EncounterType))
                    pk.EncounterType = Convert.ToInt32(Math.Log((int)type, 2));
                else
                    Debug.WriteLine("This should never happen");
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
                pk.ClearHyperTrainedPerfectIVs();
            }

            return false;
        }

        private static EncounterType GetRequiredEncounterType(PKM pk, IEncounterable match)
        {
            // Encounter type data is only stored for gen 4 encounters
            // All eggs have encounter type none, even if they are from static encounters
            if (!pk.Gen4 || pk.WasEgg)
                return EncounterType.None;

            // If there is more than one slot, the get wild encounter have filter for the pkm type encounter like safari/sport ball
            return match switch
            {
                EncounterSlot w => w.TypeEncounter,
                EncounterStaticTyped s => s.TypeEncounter,
                _ => EncounterType.None
            };
        }

        private static string GetReport(PKM pk)
        {
            var la = new LegalityAnalysis(pk);
            return la.Report();
        }

        private static void SetPIDSID(PKM pk, bool shiny, bool XD = false)
        {
            uint hp = (uint)pk.IV_HP;
            uint atk = (uint)pk.IV_ATK;
            uint def = (uint)pk.IV_DEF;
            uint spa = (uint)pk.IV_SPA;
            uint spd = (uint)pk.IV_SPD;
            uint spe = (uint)pk.IV_SPE;

            void LoadOldIVs()
            {
                pk.IV_HP = (int)hp;
                pk.IV_ATK = (int)atk;
                pk.IV_DEF = (int)def;
                pk.IV_SPA = (int)spa;
                pk.IV_SPD = (int)spd;
                pk.IV_SPE = (int)spe;
            }

            uint nature = (uint)pk.Nature;
            var type = XD ? RNGReporter.FrameType.ColoXD : RNGReporter.FrameType.Method1;
            var pidsid = IVtoPIDGenerator.Generate(hp, atk, def, spa, spd, spe, nature, 0, type);

            if (pk.Species == 490 && pk.Gen4)
            {
                pk.Egg_Location = 2002;
                pk.FatefulEncounter = true;
            }
            pk.PID = pidsid[0];
            if (pk.GenNumber < 5)
                pk.EncryptionConstant = pk.PID;
            pk.SID = Convert.ToInt32(pidsid[1]);
            if (shiny)
                pk.SetShinySID();
            var recheckLA = new LegalityAnalysis(pk);
            string updatedReport = recheckLA.Report();
            Debug.WriteLine(updatedReport);
            if (!updatedReport.Contains(LPIDTypeMismatch))
                return;

            var ivp = IVtoPIDGenerator.GetIVPID(nature, pk.HPType, XD);
            Debug.WriteLine(XD);
            if (ivp != null)
            {
                pk.PID = ivp.PID;
                SetIVs(pk, ivp);
            }
            if (pk.GenNumber < 5)
                pk.EncryptionConstant = pk.PID;

            if (shiny)
                pk.SetShinySID();

            recheckLA = new LegalityAnalysis(pk);
            updatedReport = recheckLA.Report();

            bool pidsidmethod = updatedReport.Contains(LPIDTypeMismatch);
            if (pidsid[0] == 0 && pidsid[1] == 0 && pidsidmethod)
            {
                pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, (uint)(pk.AbilityNumber * 0x10001));
                LoadOldIVs();
            }
            if (shiny)
                pk.SetShinySID();

            recheckLA = new LegalityAnalysis(pk);
            updatedReport = recheckLA.Report();
            if (updatedReport.Contains(LPIDGenderMismatch))
            {
                pk.Gender = pk.Gender == 0 ? 1 : 0;
                var recheckLA2 = new LegalityAnalysis(pk);
                updatedReport = recheckLA2.Report();
            }
            if (updatedReport.Contains(LHyperBelow100))
            {
                pk.CurrentLevel = 100;
                var recheckLA2 = new LegalityAnalysis(pk);
                updatedReport = recheckLA2.Report();
            }
            var la = new LegalityAnalysis(pk);
            if (la.Valid)
                return;
            // Fix Moves if a slot is empty
            pk.FixMoves();

            pk.RefreshAbility((uint)pk.AbilityNumber < 6 ? pk.AbilityNumber >> 1 : 0);
            if (updatedReport.Contains(LPIDTypeMismatch) || UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
            {
                if (pk.GenNumber == 3 || UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
                {
                    M2EventFix(pk, shiny);
                    if (!new LegalityAnalysis(pk).Report().Contains(LPIDTypeMismatch) || UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
                        return;
                }
                LoadOldIVs();
            }
        }

        private static void SetIVs(PKM pk, IVPID ivp)
        {
            pk.IV_HP = (int)ivp.HP;
            pk.IV_ATK = (int)ivp.ATK;
            pk.IV_DEF = (int)ivp.DEF;
            pk.IV_SPA = (int)ivp.SPA;
            pk.IV_SPD = (int)ivp.SPD;
            pk.IV_SPE = (int)ivp.SPE;
        }

        private static void M2EventFix(PKM pk, bool shiny)
        {
            int eggloc = pk.Egg_Location;
            bool feFlag = pk.FatefulEncounter;
            pk.Egg_Location = 0;
            pk.FatefulEncounter = true;
            var ivp = IVtoPIDGenerator.GetIVPID((uint)pk.Nature, pk.HPType, false, PIDType.Method_2);
            if (ivp != null)
            {
                pk.PID = ivp.PID;
                SetIVs(pk, ivp);
            }
            if (pk.GenNumber < 5)
                pk.EncryptionConstant = pk.PID;
            if (shiny)
                pk.SetShinySID();
            var recheckLA = new LegalityAnalysis(pk);
            string updatedReport = recheckLA.Report();
            if (updatedReport.Contains(LPIDGenderMismatch))
            {
                pk.Gender = pk.Gender == 0 ? 1 : 0;
                updatedReport = new LegalityAnalysis(pk).Report();
            }

            if (!updatedReport.Contains(LPIDTypeMismatch) || UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
                return;
            Debug.WriteLine(GetReport(pk));
            pk.FatefulEncounter = feFlag;
            pk.Egg_Location = eggloc;
        }

        private static void BACD_REventFix(PKM pk, bool shiny)
        {
            int eggloc = pk.Egg_Location;
            bool feFlag = pk.FatefulEncounter;
            pk.Egg_Location = 0;
            pk.FatefulEncounter = false;
            var ivp = IVtoPIDGenerator.GetIVPID((uint)pk.Nature, pk.HPType, false, PIDType.BACD_R);
            if (ivp != null)
            {
                pk.PID = ivp.PID;
                SetIVs(pk, ivp);
            }
            if (pk.GenNumber < 5)
                pk.EncryptionConstant = pk.PID;
            if (shiny)
                pk.SetShinySID();
            var recheckLA = new LegalityAnalysis(pk);
            string updatedReport = recheckLA.Report();
            if (updatedReport.Contains(LPIDGenderMismatch))
            {
                pk.Gender = pk.Gender == 0 ? 1 : 0;
                updatedReport = new LegalityAnalysis(pk).Report();
            }
            if (!updatedReport.Contains(LPIDTypeMismatch) || UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.BACD_R))
                return;
            Debug.WriteLine(GetReport(pk));
            pk.FatefulEncounter = feFlag;
            pk.Egg_Location = eggloc;
        }

        public static bool UsesEventBasedMethod(int Species, int[] Moves, PIDType method)
        {
            var list = BruteTables.GetRNGList(method);
            return list.TryGetValue(Species, out var moves) && Moves.Any(i => moves.Contains(i));
        }
    }
}