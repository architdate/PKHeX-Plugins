using PKHeX.Core;
using System;
using System.Windows.Forms;

namespace AutoLegalityMod
{
    public partial class AutoLegalityMod : IPlugin
    {

        /// <summary>
        /// Main Plugin Variables
        /// </summary>
        public string Name => "Import with Auto-Legality Mod";
        public int Priority => 0;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public static ISaveFileProvider SFE { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public static IPKMView PE { get; private set; }
        public ToolStripItem menuinstance;

        public void Initialize(params object[] args)
        {
            Console.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null) return;
            SFE = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PE = (IPKMView)Array.Find(args, z => z is IPKMView);
            SAV = SFE.SAV;
            PKMEditor = PE;
            SaveFileEditor = SFE;
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
                mod.Image = AutoLegalityResources.menuautolegality;
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

        private void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(ClickShowdownImportPKMModded);
            ctrl.Name = "Menu_AutoLegalityMod";
            ctrl.Image = AutoLegalityResources.autolegalitymod;
            ctrl.ShortcutKeys = (Keys.Control | Keys.I);
            menuinstance = ctrl;
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
            SAV = SaveFileEditor.SAV;
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }

        /// <summary>
        /// Main function to be called by the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void ClickShowdownImportPKMModded(object sender, EventArgs e)
        {
            AutomaticLegality.PKMEditor = PE;
            AutomaticLegality.SaveFileEditor = SFE;
            AutomaticLegality.ImportModded();
        }
        
    }
}
