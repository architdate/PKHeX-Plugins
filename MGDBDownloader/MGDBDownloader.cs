using System;
using System.Net;
using System.IO.Compression;

using System.Windows.Forms;
using PKHeX.Core;
using System.IO;
using System.Linq;
using System.Threading;
using AutoLegalityMod;

namespace MGDBDownloader
{
    public class MGDBDownloader : AutoModPlugin
    {
        public override string Name => "Download MGDB";
        public override int Priority => 1;
        public static string MGDatabasePath => Path.Combine(Directory.GetCurrentDirectory(), "mgdb");

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            // many local files can delay mgdb initialization by PKHeX (file i/o speed)
            // delay returning control to the main application until the mgdb is finished loading
            if (Directory.Exists(MGDatabasePath)
                && Directory.EnumerateFiles(MGDatabasePath, "*", SearchOption.AllDirectories).Any())
            {
                while (!EncounterEvent.Initialized)
                    Thread.Sleep(50);
            }

            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += DownloadMGDB;
            ctrl.Image = Properties.Resources.mgdbdownload;
        }

        public void DownloadMGDB(object o, EventArgs e)
        {
            if (Directory.Exists(MGDatabasePath))
            {
                DialogResult dialogResult = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Update MGDB?", "MGDB already exists!");
                if (dialogResult == DialogResult.Yes)
                {
                    DeleteDirectory(MGDatabasePath); // Adding events will be handled by the next conditional
                }
            }
            if (!Directory.Exists(MGDatabasePath))
            {
                DialogResult latestCommit = WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Download the entire database, which includes past generation events?\nSelecting No will download only the public release of the database.", "Download entire database?");
                if (latestCommit == DialogResult.Yes)
                {
                    const string mgdbURL = "https://github.com/projectpokemon/EventsGallery/archive/master.zip";

                    WebClient client = new WebClient();

                    const string mgdbZipPath = "mgdb.zip";
                    client.DownloadFile(new Uri(mgdbURL), mgdbZipPath);
                    ZipFile.ExtractToDirectory(mgdbZipPath, MGDatabasePath);
                    File.Delete("mgdb.zip");
                    DeleteDirectory(Path.Combine(MGDatabasePath, "EventsGallery-master", "Unreleased"));
                    DeleteDirectory(Path.Combine(MGDatabasePath, "EventsGallery-master", "Extras"));
                    File.Delete(Path.Combine(MGDatabasePath, "EventsGallery-master", ".gitignore"));
                    File.Delete(Path.Combine(MGDatabasePath, "EventsGallery-master", "README.md"));
                    WinFormsUtil.Alert("Download Finished");
                }
                else
                {
                    WebClient client = new WebClient();
                    string json_data = DownloadString("https://api.github.com/repos/projectpokemon/EventsGallery/releases/latest");
                    string mgdbURL = json_data.Split(new[] { "browser_download_url" }, StringSplitOptions.None)[1].Substring(3).Split('"')[0];
                    Console.WriteLine(mgdbURL);
                    const string mgdbZipPath = "mgdb.zip";
                    client.DownloadFile(new Uri(mgdbURL), mgdbZipPath);
                    ZipFile.ExtractToDirectory(mgdbZipPath, MGDatabasePath);
                    File.Delete("mgdb.zip");
                    WinFormsUtil.Alert("Download Finished");
                }
            }
        }

        public static string DownloadString(string address)
        {
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
                request.Method = "GET";
                request.UserAgent = "PKHeX-Auto-Legality-Mod";
                request.Accept = "application/json";
                WebResponse response = request.GetResponse(); //Error Here
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                return reader.ReadToEnd();
            }
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
