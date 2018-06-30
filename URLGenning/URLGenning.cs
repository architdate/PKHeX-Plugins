using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using PKHeX.Core;

namespace URLGenning
{
    public class URLGenning : IPlugin
    {
        public string Name => "Gen from URL";
        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public object[] arguments;
        public ToolStripMenuItem ModMenu;

        public void Initialize(params object[] args)
        {
            arguments = args;
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
                mod.Image = URLGenningResources.menuautolegality;
                mod.Name = "Menu_AutoLegality";
                var modmenu = mod;
                ModMenu = modmenu;
                AddPluginControl(modmenu);
            }
            else
            {
                var modmenu = modmenusearch[0] as ToolStripMenuItem;
                ModMenu = modmenu;
                AddPluginControl(modmenu);
            }
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(URLGen);
            ctrl.Image = URLGenningResources.urlimport;
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

        private void URLGen(object sender, EventArgs e)
        {
            string url = Clipboard.GetText().Trim();
            string initURL = url;
            bool isUri = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
            if (!isUri)
            {
                MessageBox.Show("The text in the clipboard is not a valid URL");
                return;
            }
            if (!CheckPokePaste(url) && !CheckPasteBin(url))
            {
                MessageBox.Show("The URL provided is not a pokepast.es or a pastebin.com URL");
                return;
            }
            url = FixURL(url);
            string sets = GetText(url).TrimStart().TrimEnd();
            if (sets.StartsWith("Error :")) return;
            Clipboard.SetText(sets);
            try {
                var alm = ModMenu.DropDownItems.Find("Menu_AutoLegalityMod", false);
                if (alm.Length == 0)
                { MessageBox.Show("Auto Legality Mod Plugin missing."); return; }
                else alm[0].PerformClick();
            }
            catch { MessageBox.Show("The data inside the URL are not valid Showdown Sets"); }
            Dictionary<string, string> metadata = GetMetadata(MetaDataURL(url));
            string typeOfBin = (CheckPasteBin(url)) ? "Pastebin" : "PokePaste";
            MessageBox.Show("All sets genned from the following URL: " + initURL + "\n\n" + typeOfBin + " data:\nTitle: " + metadata["Title"] + "\nAuthor: " + metadata["Author"] + "\nDescription: " + metadata["Description"]);
            Clipboard.SetText(initURL);
        }

        private string FixURL(string url)
        {
            if (CheckPokePaste(url) && url.EndsWith("/raw")) return url;
            else if (CheckPasteBin(url) && url.Contains("/raw/")) return url;
            else if (CheckPokePaste(url)) return url + "/raw";
            else if (CheckPasteBin(url)) return url.Replace("pastebin.com/", "pastebin.com/raw/");
            else return url; // This should never happen
        }

        private string MetaDataURL(string url)
        {
            if (CheckPasteBin(url)) return url.Replace("/raw/", "/");
            else return url.Replace("/raw", "");
        }

        private bool CheckPokePaste(string url)
        {
            if (url.Contains("pokepast.es/")) return true;
            return false;
        }

        private bool CheckPasteBin(string url)
        {
            if (url.Contains("pastebin.com/")) return true;
            return false;
        }

        private Dictionary<string, string> GetMetadata(string url)
        {
            string title = "Showdown Paste";
            string author = "Pokémon Trainer";
            string description = "A Mysterious Paste";
            // Passed URL must be non raw
            if (CheckPasteBin(url))
            {
                string htmldoc = GetText(url);
                title = htmldoc.Split(new string[] { "<div class=\"paste_box_line1\" title=\"" }, StringSplitOptions.None)[1].Split('"')[0].Trim();
                author = htmldoc.Split(new string[] { "<div class=\"paste_box_line2\">" }, StringSplitOptions.None)[1].Split('>')[1].Split('<')[0].Trim();
                description = "Pastebin created on: " + htmldoc.Split(new string[] { "<div class=\"paste_box_line2\">" }, StringSplitOptions.None)[1].Split('>')[3].Split('<')[0].Trim();
            }
            if (CheckPokePaste(url))
            {
                string htmldoc = GetText(url);
                string pastedata = htmldoc.Split(new string[] { "<aside>" }, StringSplitOptions.None)[1].Split(new string[] { "</aside>" }, StringSplitOptions.None)[0];
                bool hasTitle = pastedata.Split(new string[] { "<h1>" }, StringSplitOptions.None).Length > 1;
                bool hasAuthor = pastedata.Split(new string[] { "<h2>&nbsp;by" }, StringSplitOptions.None).Length > 1;
                bool hasDescription = pastedata.Split(new string[] { "<p>" }, StringSplitOptions.None).Length > 1;
                if (hasTitle) title = pastedata.Split(new string[] { "<h1>" }, StringSplitOptions.None)[1].Split('<')[0].Trim();
                if (hasAuthor) author = pastedata.Split(new string[] { "<h2>&nbsp;by" }, StringSplitOptions.None)[1].Split('<')[0].Trim();
                if (hasDescription) description = pastedata.Split(new string[] { "<p>" }, StringSplitOptions.None)[1].Split('<')[0].Trim();
            }
            return new Dictionary<string, string>() { { "Author", author }, { "Title", title }, { "Description", description } };
        }

        private string GetText(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return responseString;
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: " + e.ToString());
                return "Error :" + e.ToString();
            }
        }
    }
}
