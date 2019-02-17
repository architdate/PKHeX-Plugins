using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Dual-approach legalization methods (regenerate and brute force)
    /// </summary>
    public static class Legalizer
    {
        /// <summary>
        /// Tries to regenerate the <see cref="pk"/> into a valid pkm.
        /// </summary>
        /// <param name="tr">Source/Destination trainer</param>
        /// <param name="pk">Currently invalid pkm data</param>
        /// <returns>Legalized PKM (hopefully legal)</returns>
        public static PKM Legalize(this ITrainerInfo tr, PKM pk)
        {
            var set = new ShowdownSet(ShowdownSet.GetShowdownText(pk));
            var legal = tr.GetLegalFromTemplate(pk, set, out var satisfied);
            var trainer = new PokeTrainerDetails(pk.Clone());
            if (!satisfied)
            {
                var resetForm = ShowdownUtil.IsInvalidForm(set.Form);
                legal = BruteForce.ApplyDetails(pk, set, resetForm, trainer);
                legal.SetTrainerData(trainer);
            }
            return legal;
        }

        /// <summary>
        /// Imports <see cref="sets"/> to a provided <see cref="arr"/>, with a context of <see cref="tr"/>.
        /// </summary>
        /// <param name="tr">Source/Destination trainer</param>
        /// <param name="sets">Set data to import</param>
        /// <param name="arr">Current list of data to write to</param>
        /// <param name="start">Starting offset to place converted details</param>
        /// <param name="overwrite">Overwrite</param>
        /// <param name="allowAPI">Use <see cref="Core"/> to find and generate a new pkm</param>
        /// <returns>Result code indicating success or failure</returns>
        public static AutoModErrorCode ImportToExisting(this ITrainerInfo tr, IReadOnlyList<ShowdownSet> sets, IList<PKM> arr, int start = 0, bool overwrite = true, bool allowAPI = true)
        {
            var emptySlots = overwrite
                ? Enumerable.Range(0, sets.Count).ToList()
                : FindAllEmptySlots(arr);

            if (emptySlots.Count < sets.Count)
                return AutoModErrorCode.NotEnoughSpace;

            var generated = 0;
            var invalidAPISets = new List<ShowdownSet>();
            for (int i = 0; i < sets.Count; i++)
            {
                var set = sets[i];
                if (set.InvalidLines.Count > 0)
                    return AutoModErrorCode.InvalidLines;

                Debug.WriteLine($"Generating Set: {GameInfo.Strings.Species[set.Species]}");
                var pk = tr.GetLegalFromSet(set, out var msg, allowAPI);
                if (msg == LegalizationResult.BruteForce)
                    invalidAPISets.Add(set);

                arr[start + emptySlots[i]] = pk;
                generated++;
            }

            Debug.WriteLine($"API Genned Sets: {generated - invalidAPISets.Count}/{generated}, {invalidAPISets.Count} were not.");
            foreach (var set in invalidAPISets)
                Debug.WriteLine(set.Text);
            return AutoModErrorCode.None;
        }

        /// <summary>
        /// Imports a <see cref="set"/> to create a new <see cref="PKM"/> with a context of <see cref="tr"/>.
        /// </summary>
        /// <param name="tr">Source/Destination trainer</param>
        /// <param name="set">Set data to import</param>
        /// <param name="msg">Result code indicating success or failure</param>
        /// <param name="allowAPI">Use <see cref="Core"/> to find and generate a new pkm</param>
        /// <returns>Legalized PKM (hopefully legal)</returns>
        public static PKM GetLegalFromSet(this ITrainerInfo tr, ShowdownSet set, out LegalizationResult msg, bool allowAPI = true)
        {
            var game = (GameVersion)tr.Game;
            var template = PKMConverter.GetBlank(game.GetGeneration(), game);
            template.ApplySetDetails(set);
            return tr.GetLegalFromSet(set, template, out msg, allowAPI);
        }

        private static PKM GetLegalFromSet(this ITrainerInfo tr, ShowdownSet set, PKM template, out LegalizationResult msg, bool allowAPI = true)
        {
            if (allowAPI && tr.TryAPIConvert(set, template, out PKM pk))
            {
                msg = LegalizationResult.Regenerated;
                return pk;
            }
            msg = LegalizationResult.BruteForce;
            return tr.GetBruteForcedLegalMon(set, template);
        }

        private static bool TryAPIConvert(this ITrainerInfo tr, ShowdownSet set, PKM template, out PKM pkm)
        {
            pkm = tr.GetLegalFromTemplate(template, set, out bool satisfied);
            if (!satisfied)
                return false;

            var trainer = TrainerSettings.GetSavedTrainerData(pkm, tr);
            pkm.SetAllTrainerData(trainer);
            return true;
        }

        private static PKM GetBruteForcedLegalMon(this ITrainerInfo tr, ShowdownSet set, PKM template)
        {
            var resetForm = ShowdownUtil.IsInvalidForm(set.Form);
            var trainer = TrainerSettings.GetSavedTrainerData(template, tr);
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