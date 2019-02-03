using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class Legalizer
    {
        public static PKM Legalize(this SaveFile sav, PKM pk)
        {
            var set = new ShowdownSet(ShowdownSet.GetShowdownText(pk));
            var legal = sav.GetLegalFromTemplate(pk, set, out var satisfied);
            var trainer = pk.GetRoughTrainerData();
            if (!satisfied)
            {
                var resetForm = ShowdownUtil.IsInvalidForm(set.Form);
                legal = BruteForce.ApplyDetails(pk, set, resetForm, trainer);
            }
            legal.SetTrainerData(trainer, satisfied);
            return legal;
        }

        public static AutoModErrorCode ImportToExisting(this SaveFile sav, IReadOnlyList<ShowdownSet> sets, IList<PKM> BoxData, int start = 0, bool replace = true, bool allowAPI = true)
        {
            var emptySlots = replace
                ? Enumerable.Range(0, sets.Count).ToList()
                : FindAllEmptySlots(BoxData);

            if (emptySlots.Count < sets.Count && sets.Count != 1)
                return AutoModErrorCode.NotEnoughSpace;

            var generated = 0;
            var invalidAPISets = new List<ShowdownSet>();
            for (int i = 0; i < sets.Count; i++)
            {
                var set = sets[i];
                if (set.InvalidLines.Count > 0)
                    return AutoModErrorCode.InvalidLines;

                var pk = sav.GetLegalFromSet(set, out var msg, allowAPI);
                if (msg == LegalizationResult.API_Invalid)
                    invalidAPISets.Add(set);

                BoxData[start + emptySlots[i]] = pk;
                generated++;
            }

            Debug.WriteLine($"API Genned Sets: {generated - invalidAPISets.Count}/{generated}, {invalidAPISets.Count} were not.");
            foreach (var set in invalidAPISets)
                Debug.WriteLine(set.Text);
            return AutoModErrorCode.None;
        }

        public static PKM GetLegalFromSet(this SaveFile sav, ShowdownSet set, out LegalizationResult msg, bool allowAPI = true)
        {
            var template = sav.BlankPKM;
            template.ApplySetDetails(set);
            return sav.GetLegalFromSet(set, template, out msg, allowAPI);
        }

        private static PKM GetLegalFromSet(this SaveFile sav, ShowdownSet set, PKM template, out LegalizationResult msg, bool allowAPI = true)
        {
            if (allowAPI && sav.TryAPIConvert(set, template, out PKM pk))
            {
                msg = LegalizationResult.API_Valid;
                return pk;
            }
            msg = LegalizationResult.API_Invalid;
            return sav.GetBruteForcedLegalMon(set, template);
        }

        private static bool TryAPIConvert(this SaveFile sav, ShowdownSet set, PKM template, out PKM pkm)
        {
            pkm = sav.GetLegalFromTemplate(template, set, out bool satisfied);
            if (!satisfied)
                return false;

            var trainer = TrainerSettings.GetSavedTrainerData(pkm, sav);
            pkm.SetAllTrainerData(trainer);
            return true;
        }

        private static PKM GetBruteForcedLegalMon(this ITrainerInfo sav, ShowdownSet set, PKM template)
        {
            bool resetForm = ShowdownUtil.IsInvalidForm(set.Form);
            var trainer = TrainerSettings.GetSavedTrainerData(template, sav);
            var legal = BruteForce.ApplyDetails(template, set, resetForm, trainer);
            legal.SetAllTrainerData(trainer);
            return legal;
        }

        /// <summary>
        /// Method to find all empty slots in a current box
        /// </summary>
        /// <param name="data">Box Data of the save file</param>
        /// <returns>A list of all indices in the current box that are empty</returns>
        private static List<int> FindAllEmptySlots(IList<PKM> data)
        {
            var emptySlots = new List<int>();
            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].Species < 1)
                    emptySlots.Add(i);
            }
            return emptySlots;
        }
    }
}