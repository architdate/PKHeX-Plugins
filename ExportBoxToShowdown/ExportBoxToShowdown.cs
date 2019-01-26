using PKHeX.Core;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace ExportBoxToShowdown
{
    public class ExportBoxToShowdown : IPlugin
    {
        public string Name => "Export Box to Showdown";
        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }

        public void Initialize(params object[] args)
        {
            Debug.WriteLine($"[Auto Legality Mod] Loading {Name}");
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
                mod.Image = ExportBoxToShowdownResources.menuautolegality;
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
            ctrl.Click += BoxToShowdown;
            ctrl.Image = ExportBoxToShowdownResources.exportboxtoshowdown;
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

        private void BoxToShowdown(object sender, EventArgs e)
        {
            try
            {
                var str = GetShowdownSetsFromBoxCurrent(SaveFileEditor.SAV);
                if (string.IsNullOrWhiteSpace(str)) return;
                Clipboard.SetText(str);
            }
            catch { }
            MessageBox.Show("Exported the active box to Showdown format");
        }

        private static string GetShowdownSetsFromBoxCurrent(SaveFile sav) => GetShowdownSetsFromBox(sav, sav.CurrentBox);

        private static string GetShowdownSetsFromBox(SaveFile sav, int box)
        {
            var CurrBox = sav.GetBoxData(box);
            return ShowdownSet.GetShowdownSets(CurrBox, Environment.NewLine + Environment.NewLine);
        }
    }
}
