using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class Legalizer
    {
        public static PKM Legalize(PKM pk)
        {
            var set = new ShowdownSet(ShowdownSet.GetShowdownText(pk));

            PKM APIGenerated;
            bool satisfied = false;
            try { APIGenerated = API.APILegality(pk, set, out satisfied); }
            catch { APIGenerated = API.SAV.BlankPKM; }

            var trainer = pk.GetRoughTrainerData();
            PKM legal;
            if (satisfied)
            {
                legal = APIGenerated;
            }
            else
            {
                bool resetForm = ShowdownUtil.IsInvalidForm(set.Form);
                legal = BruteForce.ApplyDetails(pk, set, resetForm, trainer);
            }
            legal.SetTrainerData(trainer, satisfied);
            return legal;
        }

        public static AutoModErrorCode ImportToExisting(IReadOnlyList<ShowdownSet> sets, IList<PKM> BoxData, int start, bool replace, bool allowAPI)
        {
            var emptySlots = replace
                ? Enumerable.Range(0, sets.Count).ToList()
                : FindAllEmptySlots(BoxData);

            if (emptySlots.Count < sets.Count && sets.Count != 1)
                return AutoModErrorCode.NotEnoughSpace;

            int validAPI = 0;
            var invalidAPISets = new List<ShowdownSet>();
            for (int i = 0; i < sets.Count; i++)
            {
                var set = sets[i];
                if (set.InvalidLines.Count > 0)
                    return AutoModErrorCode.InvalidLines;

                var pk = GetLegalFromSet(set, out var msg, allowAPI);
                switch (msg)
                {
                    case LegalizationResult.API_Valid:
                        validAPI++;
                        break;
                    case LegalizationResult.API_Invalid:
                        invalidAPISets.Add(set);
                        break;
                }

                BoxData[start + emptySlots[i]] = pk;
            }

            var total = invalidAPISets.Count + validAPI;
            Debug.WriteLine($"API Genned Sets: {validAPI}/{total}, {invalidAPISets.Count} were not.");
            foreach (var set in invalidAPISets)
                Debug.WriteLine(set.Text);
            return AutoModErrorCode.None;
        }

        public static PKM GetLegalFromSet(ShowdownSet set, out LegalizationResult msg, bool allowAPI = true)
        {
            var template = API.SAV.BlankPKM;
            template.ApplySetDetails(set);
            template.Version = (int)GameVersion.MN; // Avoid the blank version glitch
            return GetLegalFromSet(set, template, out msg, allowAPI);
        }

        private static PKM GetLegalFromSet(ShowdownSet set, PKM template, out LegalizationResult msg, bool allowAPI = true)
        {
            if (allowAPI && TryAPIConvert(set, template, out PKM pk))
            {
                msg = LegalizationResult.API_Valid;
                return pk;
            }
            msg = LegalizationResult.API_Invalid;
            return GetBruteForcedLegalMon(set, template);
        }

        private static bool TryAPIConvert(ShowdownSet set, PKM template, out PKM pkm)
        {
            try
            {
                pkm = API.APILegality(template, set, out bool satisfied);
                if (!satisfied)
                    return false;

                var trainer = TrainerSettings.GetSavedTrainerData(pkm);
                pkm.SetAllTrainerData(trainer);
                return true;
            }
            catch
            {
                pkm = null;
                return false;
            }
        }

        private static PKM GetBruteForcedLegalMon(ShowdownSet set, PKM template)
        {
            bool resetForm = ShowdownUtil.IsInvalidForm(set.Form);
            var trainer = TrainerSettings.GetSavedTrainerData(template);
            var legal = BruteForce.ApplyDetails(template, set, resetForm, trainer);
            legal.SetAllTrainerData(trainer);
            return legal;
        }

        /// <summary>
        /// Method to find all empty slots in a current box
        /// </summary>
        /// <param name="data">Box Data of the SAV file</param>
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