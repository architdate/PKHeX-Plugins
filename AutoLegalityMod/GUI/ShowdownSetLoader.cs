using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using PKHeX.Core.Enhancements;

namespace AutoModPlugins
{
    /// <summary>
    /// Logic that loads a <see cref="ShowdownSet"/>
    /// </summary>
    public static class ShowdownSetLoader
    {
        // Initialized during plugin setup
        public static ISaveFileProvider SaveFileEditor { private get; set; } = null!;
        public static IPKMView PKMEditor { private get; set; } = null!;

        /// <summary>
        /// Imports <see cref="ShowdownSet"/> list(s) originating from a concatenated list.
        /// </summary>
        /// <param name="source">Text containing <see cref="ShowdownSet"/> data</param>
        public static void Import(string source)
        {
            if (ShowdownUtil.IsTeamBackup(source))
            {
                var teams = ShowdownTeamSet.GetTeams(source).ToArray();
                var names = teams.Select(z => z.Summary);
                WinFormsUtil.Alert("Generating the following teams:", string.Join(Environment.NewLine, names));
                Import(teams.SelectMany(z => z.Team).ToList());
                return;
            }

            var sets = ShowdownUtil.ShowdownSets(source);
            Import(sets);
        }

        /// <summary>
        /// Imports <see cref="ShowdownSet"/> list(s).
        /// </summary>
        /// <param name="sets">Text containing <see cref="ShowdownSet"/> data</param>
        public static void Import(IEnumerable<string> sets)
        {
            var entries = sets.Select(z => new ShowdownSet(z)).ToList();
            Import(entries);
        }

        /// <summary>
        /// Import Showdown Sets and alert user of any messages intended
        /// </summary>
        /// <param name="sets">Data to be loaded</param>
        /// <param name="skipDialog">Prevents creating dialog messages</param>
        public static void Import(IReadOnlyList<ShowdownSet> sets, bool skipDialog = false)
        {
            AutoModErrorCode result;
            if (sets.Count == 1)
            {
                result = ImportSetToTabs(sets[0], skipDialog);
            }
            else
            {
                var replace = (Control.ModifierKeys & Keys.Alt) != 0;
                result = ImportSetsToBoxes(sets, replace);
            }

            var message = result.GetMessage();
            if (!string.IsNullOrEmpty(message))
                WinFormsUtil.Alert(message);
        }

        private static AutoModErrorCode ImportSetToTabs(ShowdownSet set, bool skipDialog = false)
        {
            var regen = new RegenTemplate(set, SaveFileEditor.SAV.Generation);
            if (!skipDialog && DialogResult.Yes != WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Import this set?", regen.Text))
                return AutoModErrorCode.NoSingleImport;

            if (set.InvalidLines.Count > 0)
                return AutoModErrorCode.InvalidLines;

            Debug.WriteLine($"Commencing Import of {GameInfo.Strings.Species[set.Species]}");
            var timer = Stopwatch.StartNew();

            var sav = SaveFileEditor.SAV;
            var legal = sav.GetLegalFromSet(regen, out var msg);
            if (msg == LegalizationResult.Timeout)
                Debug.WriteLine("Set timed out while generating");
            Debug.WriteLine("Single Set Genning Complete. Loading final data to tabs.");
            PKMEditor.PopulateFields(legal);

            // Debug Statements
            timer.Stop();
            var timespan = timer.Elapsed;
            Debug.WriteLine($"Time to complete {nameof(ImportSetToTabs)}: {timespan.Minutes:00} minutes {timespan.Seconds:00} seconds {timespan.Milliseconds / 10:00} milliseconds");
            return AutoModErrorCode.None;
        }

        /// <summary>
        /// Function that generates legal PKM objects from ShowdownSets and views them/sets them in boxes
        /// </summary>
        /// <param name="sets">A list of ShowdownSet(s) that need to be genned</param>
        /// <param name="replace">A boolean that determines if current pokemon will be replaced or not</param>
        private static AutoModErrorCode ImportSetsToBoxes(IReadOnlyList<ShowdownSet> sets, bool replace)
        {
            var timer = Stopwatch.StartNew();
            var sav = SaveFileEditor.SAV;
            var BoxData = sav.BoxData;
            var start = SaveFileEditor.CurrentBox * sav.BoxSlotCount;

            Debug.WriteLine($"Commencing Import of {sets.Count} set(s).");
            var result = sav.ImportToExisting(sets, BoxData, start, replace);
            if (result != AutoModErrorCode.None)
                return result;

            Debug.WriteLine("Multi Set Genning Complete. Setting data to the save file and reloading view.");
            SaveFileEditor.ReloadSlots();

            // Debug Statements
            timer.Stop();
            var timespan = timer.Elapsed;
            Debug.WriteLine($"Time to complete {nameof(ImportSetsToBoxes)}: {timespan.Minutes:00} minutes {timespan.Seconds:00} seconds {timespan.Milliseconds / 10:00} milliseconds");
            return AutoModErrorCode.None;
        }

        public static void SetAPILegalitySettings()
        {
            var settings = AutoLegality.Default;
            APILegality.UseTrainerData = settings.UseTrainerData;
            APILegality.SetAllLegalRibbons = settings.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = settings.SetMatchingBalls;
            APILegality.ForceSpecifiedBall = settings.ForceSpecifiedBall;
            APILegality.UseCompetitiveMarkings = settings.UseCompetitiveMarkings;
            APILegality.UseMarkings = settings.UseMarkings;
            APILegality.UseXOROSHIRO = settings.UseXOROSHIRO;
            APILegality.SetRandomTracker = settings.SetRandomTracker;
            APILegality.PrioritizeGame = settings.PrioritizeGame;
            APILegality.PrioritizeGameVersion = settings.PriorityGameVersion;
            APILegality.SetBattleVersion = settings.SetBattleVersion;
            APILegality.AllowTrainerOverride = settings.AllowTrainerOverride;
            APILegality.Timeout = settings.Timeout;
            Legalizer.EnableEasterEggs = settings.EnableEasterEggs;
            SmogonGenner.PromptForImport = settings.PromptForSmogonImport;

            EncounterMovesetGenerator.PriorityList = settings.PrioritizeEvent
                ? new[] {EncounterOrder.Mystery, EncounterOrder.Egg, EncounterOrder.Static, EncounterOrder.Trade, EncounterOrder.Slot}
                : new[] {EncounterOrder.Egg, EncounterOrder.Static, EncounterOrder.Trade, EncounterOrder.Slot, EncounterOrder.Mystery};
        }
    }
}
