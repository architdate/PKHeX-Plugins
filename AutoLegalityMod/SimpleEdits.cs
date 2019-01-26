using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;
using static PKHeX.Core.LegalityCheckStrings;

namespace AutoLegalityMod
{
    public static class SimpleEdits
    {
        /// <summary>
        /// Set Encryption Constant based on PKM GenNumber
        /// </summary>
        /// <param name="pk"></param>
        public static void SetEncryptionConstant(this PKM pk)
        {
            if (pk.GenNumber > 5 || pk.VC)
            {
                int wIndex = Array.IndexOf(Legal.WurmpleEvolutions, pk.Species);
                uint EC = wIndex < 0 ? Util.Rand32() : PKX.GetWurmpleEC(wIndex / 2);
                if (!(pk.Species == 658 && pk.AltForm == 1))
                    pk.EncryptionConstant = EC;
            }
            else
            {
                pk.EncryptionConstant = pk.PID; // Generations 3 to 5
            }
        }

        /// <summary>
        /// Sets shiny value to whatever boolean is specified
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="isShiny">Shiny value that needs to be set</param>
        public static void SetShinyBoolean(this PKM pk, bool isShiny)
        {
            if (!isShiny)
            {
                pk.SetUnshiny();
            }
            else
            {
                if (pk.GenNumber > 5)
                    pk.SetShiny();
                else if (pk.VC)
                    pk.SetIsShiny(true);
                else
                    pk.SetShinySID();
            }
        }

        /// <summary>
        /// Set a valid Pokeball incase of an incorrect ball issue arising with GeneratePKM
        /// </summary>
        /// <param name="pk"></param>
        public static void SetSpeciesBall(this PKM pk)
        {
            if (!new LegalityAnalysis(pk).Report().Contains(LBallEncMismatch))
                return;
            if (pk.GenNumber == 5 && pk.Met_Location == 75)
                pk.Ball = (int)Ball.Dream;
            else
                pk.Ball = 4;
        }

        public static void ClearRelearnMoves(this PKM Set)
        {
            Set.RelearnMove1 = 0;
            Set.RelearnMove2 = 0;
            Set.RelearnMove3 = 0;
            Set.RelearnMove4 = 0;
        }

        public static void SetMarkings(this PKM pk)
        {
            if (pk.Format >= 7)
            {
                if (pk.IV_HP == 30 || pk.IV_HP == 29) pk.MarkCircle = 2;
                if (pk.IV_ATK == 30 || pk.IV_ATK == 29) pk.MarkTriangle = 2;
                if (pk.IV_DEF == 30 || pk.IV_DEF == 29) pk.MarkSquare = 2;
                if (pk.IV_SPA == 30 || pk.IV_SPA == 29) pk.MarkHeart = 2;
                if (pk.IV_SPD == 30 || pk.IV_SPD == 29) pk.MarkStar = 2;
                if (pk.IV_SPE == 30 || pk.IV_SPE == 29) pk.MarkDiamond = 2;
            }
            if (pk.IV_HP == 31) pk.MarkCircle = 1;
            if (pk.IV_ATK == 31) pk.MarkTriangle = 1;
            if (pk.IV_DEF == 31) pk.MarkSquare = 1;
            if (pk.IV_SPA == 31) pk.MarkHeart = 1;
            if (pk.IV_SPD == 31) pk.MarkStar = 1;
            if (pk.IV_SPE == 31) pk.MarkDiamond = 1;
        }

        public static void ClearHyperTraining(this PKM pk)
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
        }

        public static void SetHappiness(this PKM pk)
        {
            pk.CurrentFriendship = pk.Moves.Contains(218) ? 0 : 255;
        }

        public static void SetBelugaValues(this PKM pk)
        {
            if (pk is PB7 pb7)
                pb7.ResetCalculatedValues();
        }

        public static void RestoreIVs(this PKM pk, int[] IVs)
        {
            pk.IVs = IVs;
            pk.ClearHyperTraining();
        }

        public static bool NeedsHyperTraining(this PKM pk)
        {
            int flawless = 0;
            int minIVs = 0;
            foreach (int i in pk.IVs)
            {
                if (i == 31) flawless++;
                if (i == 0 || i == 1) minIVs++; //ignore IV value = 0/1 for intentional IV values (1 for hidden power cases)
            }
            return flawless + minIVs != 6;
        }

        public static void HyperTrain(this PKM pk)
        {
            if (!(pk is IHyperTrain h) || !NeedsHyperTraining(pk))
                return;

            pk.CurrentLevel = 100; // Set level for HT before doing HT

            h.HT_HP = (pk.IV_HP != 0 && pk.IV_HP != 1 && pk.IV_HP != 31);
            h.HT_ATK = (pk.IV_ATK != 0 && pk.IV_ATK != 1 && pk.IV_ATK != 31);
            h.HT_DEF = (pk.IV_DEF != 0 && pk.IV_DEF != 1 && pk.IV_DEF != 31);
            h.HT_SPA = (pk.IV_SPA != 0 && pk.IV_SPA != 1 && pk.IV_SPA != 31);
            h.HT_SPD = (pk.IV_SPD != 0 && pk.IV_SPD != 1 && pk.IV_SPD != 31);
            h.HT_SPE = (pk.IV_SPE != 0 && pk.IV_SPE != 1 && pk.IV_SPE != 31);
        }

        public static void ClearHyperTrainedPerfectIVs(this PKM pk)
        {
            if (!(pk is IHyperTrain h))
                return;
            if (pk.IV_HP == 31) h.HT_HP = false;
            if (pk.IV_ATK == 31) h.HT_ATK = false;
            if (pk.IV_DEF == 31) h.HT_DEF = false;
            if (pk.IV_SPA == 31) h.HT_SPA = false;
            if (pk.IV_SPD == 31) h.HT_SPD = false;
            if (pk.IV_SPE == 31) h.HT_SPE = false;
        }

        public static void FixMemoriesPKM(this PKM pk)
        {
            switch (pk)
            {
                case PK7 pk7:
                    if (!pk.IsUntraded)
                        pk7.TradeMemory(true);
                    pk7.FixMemories();
                    break;
                case PK6 pk6:
                    if (!pk.IsUntraded)
                        pk6.TradeMemory(true);
                    pk6.FixMemories();
                    break;
            }
        }

        public static void ClearOTMemory(this PKM pk)
        {
            pk.OT_Memory = 0;
            pk.OT_TextVar = 0;
            pk.OT_Intensity = 0;
            pk.OT_Feeling = 0;
        }

        /// <summary>
        /// Set TID, SID and OT
        /// </summary>
        /// <param name="pk">PKM to set trainer data to</param>
        /// <param name="trainer">Trainer data</param>
        /// <param name="APILegalized">Was the <see cref="pk"/> legalized by the API</param>
        public static void SetTrainerData(this PKM pk, SimpleTrainerInfo trainer, bool APILegalized = false)
        {
            if (APILegalized)
            {
                if ((pk.TID == 12345 && pk.OT_Name == "PKHeX") || (pk.TID == 34567 && pk.SID == 0 && pk.OT_Name == "TCD"))
                {
                    bool Shiny = pk.IsShiny;
                    pk.TID = trainer.TID;
                    pk.SID = trainer.SID;
                    pk.OT_Name = trainer.OT;
                    pk.OT_Gender = trainer.Gender;
                    pk.SetShinyBoolean(Shiny);
                }
                return;
            }
            pk.TID = trainer.TID;
            pk.SID = trainer.SID;
            pk.OT_Name = trainer.OT;
        }

        /// <summary>
        /// Set Trainer data (TID, SID, OT) for a given PKM
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        public static void SetTrainerDataAndMemories(this PKM pk)
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
        /// Fix invalid and missing ribbons. (V600 and V601)
        /// </summary>
        /// <param name="pk">PKM whose ribbons need to be fixed</param>
        public static void FixRibbons(this PKM pk)
        {
            string Report = new LegalityAnalysis(pk).Report();
            if (Report.Contains(string.Format(LRibbonFMissing_0, "")))
            {
                var ribbonList = Report.Split(new[] { string.Format(LRibbonFMissing_0, "") }, StringSplitOptions.None)[1].Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(new[] { ", " }, StringSplitOptions.None);
                var RibbonNames = ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();

                var missingRibbons = new List<string>();
                foreach (var RibbonName in RibbonNames)
                {
                    string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                    if (ribbonList.Contains(v))
                        missingRibbons.Add(RibbonName);
                }
                foreach (string missing in missingRibbons)
                {
                    if (missing == nameof(PK6.RibbonCountMemoryBattle) || missing == nameof(PK6.RibbonCountMemoryContest))
                        ReflectUtil.SetValue(pk, missing, 0);
                    else
                        ReflectUtil.SetValue(pk, missing, true);
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
    }
}