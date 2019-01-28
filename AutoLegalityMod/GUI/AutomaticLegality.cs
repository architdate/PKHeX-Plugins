using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AutoLegalityMod
{
    public static class AutomaticLegality
    {
        static AutomaticLegality()
        {
            // Make a blank MGDB directory and initialize trainerdata
            if (!Directory.Exists(MGDatabasePath))
                Directory.CreateDirectory(MGDatabasePath);
        }

        // TODO: Check for Auto Legality Mod Updates
        public static ISaveFileProvider SaveFileEditor { private get; set; }
        public static IPKMView PKMEditor { private get; set; }
        private static SaveFile SAV => API.SAV;

        /// <summary>
        /// Global Variables for Auto Legality Mod
        /// </summary>
        private static readonly string MGDatabasePath = Path.Combine(Directory.GetCurrentDirectory(), "mgdb");

        public static PKM Legalize(PKM pk)
        {
            ShowdownSet set = new ShowdownSet(ShowdownSet.GetShowdownText(pk));

            PKM APIGenerated = SaveFileEditor.SAV.BlankPKM;
            bool satisfied = false;
            try { APIGenerated = API.APILegality(pk, set, out satisfied); }
            catch { }

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

        /// <summary>
        /// Imports <see cref="ShowdownSet"/> list(s) originating from a concatenated list.
        /// </summary>
        public static void ImportModded(string source)
        {
            var Sets = ShowdownUtil.ShowdownSets(source, out Dictionary<int, string[]> TeamData);
            if (TeamData != null)
                WinFormsUtil.Alert(ShowdownUtil.TeamDataAlert(TeamData));

            ImportModded(Sets);
        }

        /// <summary>
        /// Imports <see cref="ShowdownSet"/> list(s).
        /// </summary>
        public static void ImportModded(IEnumerable<string> sets)
        {
            var entries = sets.Select(z => new ShowdownSet(z)).ToList();
            ImportModded(entries);
        }

        /// <summary>
        /// Import Showdown Sets and alert user of any messages intended
        /// </summary>
        public static void ImportModded(IReadOnlyList<ShowdownSet> sets)
        {
            var timer = Stopwatch.StartNew();

            Debug.WriteLine("Commencing Import");

            const bool allowAPI = true;
            AutoModErrorCode result;
            if (sets.Count == 1)
            {
                result = ImportSetToTabs(sets[0], allowAPI);
            }
            else
            {
                bool replace = (Control.ModifierKeys & Keys.Control) != 0;
                result = ImportSetsToBoxes(sets, replace, allowAPI);
            }

            // Debug Statements
            timer.Stop();
            TimeSpan timespan = timer.Elapsed;
            Debug.WriteLine($"Time to complete function: {timespan.Minutes:00} minutes {timespan.Seconds:00} seconds {timespan.Milliseconds / 10:00} milliseconds");

            var message = result.GetMessage();
            if (!string.IsNullOrEmpty(message))
                WinFormsUtil.Alert(message);
        }

        private static AutoModErrorCode ImportSetToTabs(ShowdownSet set, bool allowAPI)
        {
            if (DialogResult.Yes != WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Import this set?", set.Text))
                return AutoModErrorCode.NoSingleImport;
            if (set.InvalidLines.Count > 0)
                return AutoModErrorCode.InvalidLines;

            PKM legal = GetLegalFromSet(set, allowAPI, out var _);
            Debug.WriteLine("Single Set Genning Complete. Loading final data to tabs.");
            PKMEditor.PopulateFields(legal);
            return AutoModErrorCode.None;
        }

        /// <summary>
        /// Function that generates legal PKM objects from ShowdownSets and views them/sets them in boxes
        /// </summary>
        /// <param name="sets">A list of ShowdownSet(s) that need to be genned</param>
        /// <param name="replace">A boolean that determines if current pokemon will be replaced or not</param>
        /// <param name="allowAPI">Use of generators before bruteforcing</param>
        private static AutoModErrorCode ImportSetsToBoxes(IReadOnlyList<ShowdownSet> sets, bool replace, bool allowAPI)
        {
            var BoxData = SAV.BoxData;
            int start = SaveFileEditor.CurrentBox * SAV.BoxSlotCount;

            var result = ImportToExisting(sets, BoxData, start, replace, allowAPI);
            if (result != AutoModErrorCode.None)
                return result;

            Debug.WriteLine("Multi Set Genning Complete. Setting data to the save file and reloading view.");
            SAV.BoxData = BoxData;
            SaveFileEditor.ReloadSlots();
            return AutoModErrorCode.None;
        }

        private static AutoModErrorCode ImportToExisting(IReadOnlyList<ShowdownSet> sets, IList<PKM> BoxData, int start, bool replace, bool allowAPI)
        {
            var emptySlots = replace
                ? Enumerable.Range(0, sets.Count).ToList()
                : FindAllEmptySlots(BoxData);

            if (emptySlots.Count < sets.Count && sets.Count != 1)
                return AutoModErrorCode.NotEnoughSpace;

            int apiCounter = 0;
            var invalidAPISets = new List<ShowdownSet>();
            for (int i = 0; i < sets.Count; i++)
            {
                ShowdownSet set = sets[i];
                if (set.InvalidLines.Count > 0)
                    return AutoModErrorCode.InvalidLines;

                PKM legal = GetLegalFromSet(set, allowAPI, out var msg);
                switch (msg)
                {
                    case LegalizationResult.API_Valid:
                        apiCounter++;
                        break;
                    case LegalizationResult.API_Invalid:
                        invalidAPISets.Add(set);
                        break;
                }

                BoxData[start + emptySlots[i]] = legal;
            }

            var total = invalidAPISets.Count + apiCounter;
            Debug.WriteLine($"API Genned Sets: {apiCounter}/{total}, {invalidAPISets.Count} were not.");
            foreach (var set in invalidAPISets)
                Debug.WriteLine(set.Text);
            return AutoModErrorCode.None;
        }

        private enum LegalizationResult
        {
            Other,
            API_Invalid,
            API_Valid,
        }

        private static PKM GetLegalFromSet(ShowdownSet set, bool allowAPI, out LegalizationResult msg)
        {
            PKM roughPKM = SAV.BlankPKM;
            roughPKM.ApplySetDetails(set);
            roughPKM.Version = (int)GameVersion.MN; // Avoid the blank version glitch
            if (allowAPI && TryAPIConvert(set, roughPKM, out PKM pk))
            {
                msg = LegalizationResult.API_Valid;
                return pk;
            }
            msg = LegalizationResult.API_Invalid;
            return GetBruteForcedLegalMon(set, roughPKM);
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
