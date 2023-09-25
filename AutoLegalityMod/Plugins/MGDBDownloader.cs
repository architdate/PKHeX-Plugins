using System;
using System.IO;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.Enhancements;

namespace AutoModPlugins
{
    public class MGDBDownloader : AutoModPlugin
    {
        public override string Name => "Download MGDB";
        public override int Priority => 1;
        public static string MGDatabasePath =>
            Path.Combine(Directory.GetCurrentDirectory(), "mgdb");

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.mgdbdownload };
            ctrl.Click += DownloadMGDB;
            ctrl.Name = "Menu_MGDBDownloader";
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void DownloadMGDB(object? o, EventArgs e)
        {
            if (Directory.Exists(MGDatabasePath))
            {
                var result = WinFormsUtil.Prompt(
                    MessageBoxButtons.YesNo,
                    "MGDB already exists!",
                    "Update MGDB?"
                );
                if (result != DialogResult.Yes)
                    return;
                DeleteDirectory(MGDatabasePath); // Adding events will be handled by the next conditional
            }
            if (Directory.Exists(MGDatabasePath))
                return;

            var prompt = WinFormsUtil.Prompt(
                MessageBoxButtons.YesNoCancel,
                "Download entire database?",
                "Download the entire database, which includes past generation events?",
                "Selecting No will download only the public release of the database."
            );

            if (prompt == DialogResult.Cancel)
                return;
            var entire = prompt == DialogResult.Yes;
            EventsGallery.DownloadMGDBFromGitHub(MGDatabasePath, entire);
            WinFormsUtil.Alert("Download Finished");
            EncounterEvent.RefreshMGDB(MGDatabasePath);
        }

        public static void DeleteDirectory(string target_dir)
        {
            var files = Directory.GetFiles(target_dir);
            var dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
    }
}
