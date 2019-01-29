using System;
using System.Windows.Forms;
using System.IO;
using AutoLegalityMod;
using PKHeX.Core;

namespace MGDBDownloader
{
    public class MGDBDownloader : AutoModPlugin
    {
        public override string Name => "Download MGDB";
        public override int Priority => 1;
        public static string MGDatabasePath => Path.Combine(Directory.GetCurrentDirectory(), "mgdb");

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += DownloadMGDB;
            ctrl.Image = Properties.Resources.mgdbdownload;
        }

        public void DownloadMGDB(object o, EventArgs e)
        {
            if (Directory.Exists(MGDatabasePath))
            {
                var result = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "MGDB already exists!", "Update MGDB?");
                if (result != DialogResult.Yes)
                    return;
                DeleteDirectory(MGDatabasePath); // Adding events will be handled by the next conditional
            }
            if (Directory.Exists(MGDatabasePath))
                return;

            var prompt = WinFormsUtil.Prompt(MessageBoxButtons.YesNo,
                "Download entire database?",
                "Download the entire database, which includes past generation events?",
                "Selecting No will download only the public release of the database.");

            bool entire = prompt == DialogResult.Yes;
            EventsGalleryDownload.DownloadMGDBFromGitHub(MGDatabasePath, entire);
            WinFormsUtil.Alert("Download Finished");
            Legal.RefreshMGDB(MGDatabasePath);
        }

        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

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
