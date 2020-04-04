using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoModPlugins
{
    /// <summary>
    /// Base plugin logic; automatically adds plugin info
    /// </summary>
    public abstract class AutoModPlugin : IPlugin
    {
        private const string ParentMenuName = "Menu_AutoLegality";
        private const string ParentMenuText = "Auto Legality Mod";
        private const string ParentMenuParent = "Menu_Tools";

        /// <summary>
        /// Main Plugin Variables
        /// </summary>
        public abstract string Name { get; }
        public abstract int Priority { get; }
        public ISaveFileProvider SaveFileEditor { get; private set; }
        protected IPKMView PKMEditor { get; private set; }

        public void Initialize(params object[] args)
        {
            Debug.WriteLine($"[Auto Legality Mod] Loading {Name}");
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
            WinFormsTranslator.TranslateInterface(((ContainerControl)SaveFileEditor).ParentForm, WinFormsTranslator.CurrentLanguage);
            
            // ALM Settings
            ShowdownSetLoader.SetAPILegalitySettings();
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            if (!(items.Find(ParentMenuParent, false)[0] is ToolStripDropDownItem tools))
                return;
            var toolsitems = tools.DropDownItems;
            var modmenusearch = toolsitems.Find(ParentMenuName, false);
            var modmenu = GetModMenu(tools, modmenusearch);
            AddPluginControl(modmenu);
        }

        private static ToolStripMenuItem GetModMenu(ToolStripDropDownItem tools, IReadOnlyList<ToolStripItem> search)
        {
            if (search.Count != 0)
                return (ToolStripMenuItem)search[0];

            var modmenu = CreateBaseGroupItem();
            tools.DropDownItems.Insert(0, modmenu);
            return modmenu;
        }

        private static ToolStripMenuItem CreateBaseGroupItem()
        {
            return new ToolStripMenuItem(ParentMenuText)
            {
                Image = Properties.Resources.menuautolegality,
                Name = ParentMenuName
            };
        }

        protected abstract void AddPluginControl(ToolStripDropDownItem modmenu);

        public virtual void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public virtual bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }
    }
}
