using System;
using System.Net;
using System.IO.Compression;

using System.Windows.Forms;
using PKHeX.Core;
using System.IO;

namespace MGDBDownloader
{
    public class MGDBDownloader : IPlugin
    {
        public string Name => "Download MGDB";
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public static string MGDatabasePath => Path.Combine(Directory.GetCurrentDirectory(), "mgdb");

        public void Initialize(params object[] args)
        {
            Console.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null)
                return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            var tools = items.Find("Menu_Tools", false)[0] as ToolStripDropDownItem;
            var toolsitems = tools.DropDownItems;
            var modmenusearch = toolsitems.Find("Menu_AutoLegality", false);
            if (modmenusearch.Length == 0)
            {
                var mod = new ToolStripMenuItem("Auto Legality Mod");
                tools.DropDownItems.Insert(0, mod);
                mod.Image = MGDBDownloaderResources.menuautolegality;
                mod.Name = "Menu_AutoLegality";
                var modmenu = mod;
                AddPluginControl(modmenu);
            }
            else
            {
                var modmenu = modmenusearch[0] as ToolStripMenuItem;
                AddPluginControl(modmenu);
            }
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(DownloadMGDB);
            ctrl.Image = MGDBDownloaderResources.mgdbdownload;
        }

        public void DownloadMGDB(object o, EventArgs e)
        {
            if (Directory.Exists(MGDatabasePath))
            {
                DialogResult dialogResult = MessageBox.Show("Update MGDB?", "MGDB already exists!", MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    DeleteDirectory(MGDatabasePath); // Adding events will be handled by the next conditional
                }
            }
            if (!Directory.Exists(MGDatabasePath))
            {
                DialogResult latestCommit = MessageBox.Show("Download the entire database, which includes past generation events?\nSelecting No will download only the public release of the database.", "Download entire database?", MessageBoxButtons.YesNo);
                if (latestCommit == DialogResult.Yes)
                {
                    string mgdbURL = @"https://github.com/projectpokemon/EventsGallery/archive/master.zip";

                    WebClient client = new WebClient();

                    string mgdbZipPath = @"mgdb.zip";
                    client.DownloadFile(new Uri(mgdbURL), mgdbZipPath);
                    ZipFile.ExtractToDirectory(mgdbZipPath, MGDatabasePath);
                    File.Delete("mgdb.zip");
                    DeleteDirectory(Path.Combine(MGDatabasePath, "EventsGallery-master", "Unreleased"));
                    DeleteDirectory(Path.Combine(MGDatabasePath, "EventsGallery-master", "Extras"));
                    File.Delete(Path.Combine(MGDatabasePath, "EventsGallery-master", ".gitignore"));
                    File.Delete(Path.Combine(MGDatabasePath, "EventsGallery-master", "README.md"));
                    MessageBox.Show("Download Finished");
                }
                else
                {
                    WebClient client = new WebClient();
                    string json_data = DownloadString("https://api.github.com/repos/projectpokemon/EventsGallery/releases/latest");
                    string mgdbURL = json_data.Split(new string[] { "browser_download_url" }, StringSplitOptions.None)[1].Substring(3).Split('"')[0];
                    Console.WriteLine(mgdbURL);
                    string mgdbZipPath = @"mgdb.zip";
                    client.DownloadFile(new Uri(mgdbURL), mgdbZipPath);
                    ZipFile.ExtractToDirectory(mgdbZipPath, MGDatabasePath);
                    File.Delete("mgdb.zip");
                    MessageBox.Show("Download Finished");
                }
            }
        }

        public static string DownloadString(string address)
        {
            using (WebClient client = new WebClient())
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.github.com/repos/projectpokemon/EventsGallery/releases/latest");
                request.Method = "GET";
                request.UserAgent = "PKHeX-Auto-Legality-Mod";
                request.Accept = "application/json";
                WebResponse response = request.GetResponse(); //Error Here
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = reader.ReadToEnd();

                return result;
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

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }
    }
}
