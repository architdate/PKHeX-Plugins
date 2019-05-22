using System;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class SimpleEdits
    {
        static SimpleEdits()
        {
            // Make PKHeX use our own marking method
            CommonEdits.MarkingMethod = FlagIVsAutoMod;
        }

        private static Func<int, int, int> FlagIVsAutoMod(PKM pk)
        {
            if (pk.Format < 7)
                return GetSimpleMarking;
            return GetComplexMarking;

            // value, index
            int GetSimpleMarking(int val, int _) => val == 31 ? 1 : 0;
            int GetComplexMarking(int val, int _)
            {
                if (val == 31)
                    return 1;
                if (val == 1 || val == 0)
                    return 2;
                return 0;
            }
        }

        /// <summary>
        /// Set Encryption Constant based on PKM GenNumber
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        public static void SetEncryptionConstant(this PKM pk)
        {
            if (pk.Species == 658 && pk.AltForm == 1) // Ash-Greninja
                return;
            pk.SetRandomEC();
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

        public static void ClearRelearnMoves(this PKM pk)
        {
            pk.RelearnMove1 = 0;
            pk.RelearnMove2 = 0;
            pk.RelearnMove3 = 0;
            pk.RelearnMove4 = 0;
        }

        public static void ClearHyperTraining(this PKM pk)
        {
            if (pk is IHyperTrain h)
                h.HyperTrainClear();
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
            for (int i = 0; i < 6; i++)
            {
                var iv = pk.GetIV(i);
                if (iv == 31 || iv <= 1) // ignore IV value = 0/1 for intentional IV values (1 for hidden power cases)
                    continue;
                return true; // flawed IV present
            }
            return false;
        }

        public static void HyperTrain(this PKM pk)
        {
            if (!(pk is IHyperTrain) || !NeedsHyperTraining(pk))
                return;

            pk.CurrentLevel = 100; // Set level for HT before doing HT
            pk.SetSuggestedHyperTrainingData();
        }

        public static void ClearHyperTrainedPerfectIVs(this PKM pk)
        {
            if (!(pk is IHyperTrain h))
                return;
            if (pk.IV_HP == 31)
                h.HT_HP = false;
            if (pk.IV_ATK == 31)
                h.HT_ATK = false;
            if (pk.IV_DEF == 31)
                h.HT_DEF = false;
            if (pk.IV_SPA == 31)
                h.HT_SPA = false;
            if (pk.IV_SPD == 31)
                h.HT_SPD = false;
            if (pk.IV_SPE == 31)
                h.HT_SPE = false;
        }

        public static void SetSuggestedMemories(this PKM pk)
        {
            switch (pk)
            {
                case PK7 pk7 when !pk.IsUntraded:
                    pk7.TradeMemory(true);
                    break;
                case PK6 pk6 when !pk.IsUntraded:
                    pk6.TradeMemory(true);
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
        public static void SetTrainerData(this PKM pk, ITrainerInfo trainer)
        {
            pk.TID = trainer.TID;
            pk.SID = pk.GenNumber >= 3 ? trainer.SID : 0;
            pk.OT_Name = trainer.OT;
        }

        /// <summary>
        /// Set Handling Trainer data for a given PKM
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="trainer">Trainer to handle the <see cref="pk"/></param>
        public static void SetHandlerandMemory(this PKM pk, ITrainerInfo trainer)
        {
            pk.CurrentHandler = 1;
            pk.HT_Name = trainer.OT;
            pk.HT_Gender = trainer.Gender;
            pk.SetSuggestedMemories();
        }

        /// <summary>
        /// Set trainer data for a legal PKM
        /// </summary>
        /// <param name="pk">Legal PKM for setting the data</param>
        /// <param name="trainer"></param>
        /// <returns>PKM with the necessary values modified to reflect trainerdata changes</returns>
        public static void SetAllTrainerData(this PKM pk, ITrainerInfo trainer)
        {
            pk.SetBelugaValues(); // trainer details changed?
            pk.ConsoleRegion = trainer.ConsoleRegion;
            pk.Country = trainer.Country;
            pk.Region = trainer.SubRegion;
        }

        /// <summary>
        /// Sets a moveset which is suggested based on calculated legality.
        /// </summary>
        /// <param name="pk">Legal PKM for setting the data</param>
        /// <param name="random">True for Random assortment of legal moves, false if current moves only.</param>
        /// <param name="la">Current legality report (calculated if not provided)</param>
        public static void SetSuggestedMoves(this PKM pk, bool random = false, LegalityAnalysis la = null)
        {
            int[] m = pk.GetMoveSet(random, la);
            if (m?.Any(z => z != 0) != true)
                return;

            if (pk.Moves.SequenceEqual(m))
                return;

            pk.SetMoves(m);
        }
    }
}