using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
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

        private static readonly EncounterTypeGroup[] EncounterPriority =
        {
            EncounterTypeGroup.Egg, EncounterTypeGroup.Static, EncounterTypeGroup.Trade, EncounterTypeGroup.Slot, EncounterTypeGroup.Mystery,
        };

        /// <summary>
        /// Imports <see cref="ShowdownSet"/> list(s) originating from a concatenated list.
        /// </summary>
        /// <param name="source">Text containing <see cref="ShowdownSet"/> data</param>
        public static void Import(string source)
        {
            if (ShowdownUtil.IsTeamBackup(source))
            {
                var teams = ShowdownTeamSet.GetTeams(source);
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
            timer.Stop();

            if (msg is LegalizationResult.VersionMismatch)
            {
                var errorstr = "The PKHeX-Plugins version does not match the PKHeX version.\n\n" +
                    $"Refer to the Wiki to fix this error.\n\n" +
                    $"The current ALM Version is {ALMVersion.Versions.AlmVersionCurrent}\n" +
                    $"The current PKHeX Version is {ALMVersion.Versions.CoreVersionCurrent}";

                var error = WinFormsUtil.ALMErrorBasic(errorstr);
                error.ShowDialog();

                var res = error.DialogResult;
                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Installing-PKHeX-Plugins", UseShellExecute = true });
                return AutoModErrorCode.VersionMismatch;
            }

            if (msg is LegalizationResult.Timeout or LegalizationResult.Failed)
            {
                Legalizer.Dump(regen, msg == LegalizationResult.Failed);

                string? analysis = null;
                if (msg is LegalizationResult.Failed)
                    analysis = regen.SetAnalysis(sav, legal);

                var errorstr = msg == LegalizationResult.Failed ? "failed to generate" : "timed out";
                var invalid_set_error = (analysis == null ? $"Set {errorstr}." : $"Set Invalid: {analysis}") +
                    "\n\nRefer to the wiki for more help on generating sets correctly." +
                    "\n\nIf you are sure this set is valid, please create an issue on GitHub and upload the error_log.txt file in the issue.";
                var error = WinFormsUtil.ALMErrorBasic(invalid_set_error);
                error.ShowDialog();

                var res = error.DialogResult;
                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Getting-Started-with-Auto-Legality-Mod", UseShellExecute = true });
            }

            Debug.WriteLine("Single Set Genning Complete. Loading final data to tabs.");
            PKMEditor.PopulateFields(legal);

            var timespan = timer.Elapsed;
            Debug.WriteLine($"Time to complete {nameof(ImportSetToTabs)}: {timespan.Minutes:00} minutes {timespan.Seconds:00} seconds {timespan.Milliseconds / 10:00} milliseconds");
            return AutoModErrorCode.None;
        }

        /// <summary>
        /// Function that generates legal PKM objects from ShowdownSets and views them/sets them in boxes
        /// </summary>
        /// <param name="sets">A list of ShowdownSet(s) that need to be generated</param>
        /// <param name="replace">A boolean that determines if current pokemon will be replaced or not</param>
        private static AutoModErrorCode ImportSetsToBoxes(IReadOnlyList<ShowdownSet> sets, bool replace)
        {
            var timer = Stopwatch.StartNew();
            var sav = SaveFileEditor.SAV;
            var BoxData = sav.BoxData;
            var start = SaveFileEditor.CurrentBox * sav.BoxSlotCount;

            Debug.WriteLine($"Commencing Import of {sets.Count} set(s).");
            var result = sav.ImportToExisting(sets, BoxData, out var invalid, out var timeout, start, replace);
            if (timeout.Count > 0 || invalid.Count > 0)
            {
                var errorstr = $"{timeout.Count} set(s) timed out and {invalid.Count} set(s) are invalid." +
                                "\n\nRefer to the wiki for more help on generating sets correctly." +
                                "\n\nIf you are sure this set is valid, please create an issue on GitHub and upload the error_log.txt file in the issue.";

                var error = WinFormsUtil.ALMErrorBasic(errorstr);
                error.ShowDialog();

                var res = error.DialogResult;
                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Getting-Started-with-Auto-Legality-Mod", UseShellExecute = true });
            }

            if (result is AutoModErrorCode.VersionMismatch)
            {
                var errorstr = "The PKHeX-Plugins version does not match the PKHeX version.\nRefer to the Wiki for how to fix this error.\n\n" +
                              $"The current ALM Version is {ALMVersion.Versions.AlmVersionCurrent}\n" +
                              $"The current PKHeX Version is {ALMVersion.Versions.CoreVersionCurrent}";

                var error = WinFormsUtil.ALMErrorBasic(errorstr);
                error.ShowDialog();

                var res = error.DialogResult;
                if (res == DialogResult.Retry)
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Installing-PKHeX-Plugins", UseShellExecute = true });
                return AutoModErrorCode.VersionMismatch;
            }

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

        public static void SetAPILegalitySettings(PluginSettings settings)
        {
            APILegality.UseTrainerData = settings.UseTrainerData;
            TrainerSettings.DefaultOT = settings.DefaultOT;
            TrainerSettings.DefaultTID16 = settings.DefaultTID16;
            TrainerSettings.DefaultSID16 = settings.DefaultSID16;
            APILegality.SetAllLegalRibbons = settings.SetAllLegalRibbons;
            APILegality.SetMatchingBalls = settings.SetMatchingBalls;
            APILegality.ForceSpecifiedBall = settings.ForceSpecifiedBall;
            APILegality.UseCompetitiveMarkings = settings.UseCompetitiveMarkings;
            APILegality.UseMarkings = settings.UseMarkings;
            APILegality.EnableDevMode = settings.EnableDevMode;
            APILegality.UseXOROSHIRO = settings.UseXOROSHIRO;
            APILegality.PrioritizeGame = settings.PrioritizeGame;
            APILegality.PrioritizeGameVersion = settings.PriorityGameVersion;
            APILegality.SetBattleVersion = settings.SetBattleVersion;
            APILegality.AllowTrainerOverride = settings.AllowTrainerOverride;
            APILegality.Timeout = settings.Timeout;
            APILegality.ForceLevel100for50 = settings.ForceLevel100for50;
            Legalizer.EnableEasterEggs = settings.EnableEasterEggs;
            SmogonGenner.PromptForImport = settings.PromptForSmogonImport;
            ModLogic.IncludeForms = settings.IncludeForms;
            ModLogic.SetShiny = settings.SetShiny;
            ModLogic.SetAlpha = settings.SetAlpha;
            ModLogic.NativeOnly = settings.NativeOnly;

            if (APILegality.UseCompetitiveMarkings)
                MarkingApplicator.MarkingMethod = APILegality.CompetitiveMarking;

            if (APILegality.EnableDevMode && settings.LatestAllowedVersion == "0.0.0.0")
            {
                settings.LatestAllowedVersion = ALMVersion.Versions.CoreVersionLatest?.ToString() ?? "0.0.0.0";
                APILegality.LatestAllowedVersion = settings.LatestAllowedVersion;
            }
            else APILegality.LatestAllowedVersion = settings.LatestAllowedVersion;

            settings.PrioritizeEncounters ??= EncounterPriority.ToList();
            foreach (var ep in EncounterPriority)
                if (!settings.PrioritizeEncounters.Contains(ep)) settings.PrioritizeEncounters.Add(ep);
            settings.PrioritizeEncounters = settings.PrioritizeEncounters.Distinct().ToList();
            EncounterMovesetGenerator.PriorityList = settings.PrioritizeEncounters;
        }
    }
}
