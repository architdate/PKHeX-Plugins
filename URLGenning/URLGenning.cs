using System;
using System.Windows.Forms;
using PKHeX.Core;
using AutoLegalityMod;

namespace URLGenning
{
    public class URLGenning : IPlugin
    {
        public string Name => "Gen from URL";
        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public object[] arguments;

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
            ctrl.Click += URLGen;
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

        private static void URLGen(object sender, EventArgs e)
        {
            string url = Clipboard.GetText().Trim();
            string initURL = url;
            TeamPasteInfo info;
            try
            {
                info = new TeamPasteInfo(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: {ex}");
                return;
            }
            if (!info.Valid)
            {
                MessageBox.Show("The text in the clipboard is not a valid URL.");
                return;
            }
            if (info.Source == TeamPasteInfo.PasteSource.None)
            {
                MessageBox.Show("The URL provided is not from a supported website.");
                return;
            }

            Clipboard.SetText(info.Sets);
            try { AutomaticLegality.ImportModded(); }
            catch { MessageBox.Show("The data inside the URL are not valid Showdown Sets"); }

            var response = $"All sets genned from the following URL: {info.URL}\n\n{info.Summary}";
            MessageBox.Show(response);
            Clipboard.SetText(initURL); // restore clipboard
        }
    }
}
