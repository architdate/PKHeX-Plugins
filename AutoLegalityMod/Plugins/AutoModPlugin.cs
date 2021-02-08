using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod.AutoMod;

namespace AutoModPlugins
{
    /// <summary>
    /// Base plugin logic; automatically adds plugin info
    /// </summary>
    public abstract class AutoModPlugin : IPlugin
    {
        private const string VERSION = "21.01.30";

        private const string ParentMenuName = "Menu_AutoLegality";
        private const string ParentMenuText = "Auto-Legality Mod";
        private const string ParentMenuParent = "Menu_Tools";

        public bool PossibleVersionMismatch = false;

        /// <summary>
        /// Main Plugin Variables
        /// </summary>
        public abstract string Name { get; }
        public abstract int Priority { get; }

        // Initialized during plugin startup
        public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
        protected IPKMView PKMEditor { get; private set; } = null!;

        public void Initialize(params object[] args)
        {
            Debug.WriteLine($"[Auto-Legality Mod] Loading {Name}");
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
            // ReSharper disable once SuspiciousTypeConversion.Global
            ((ContainerControl)SaveFileEditor).ParentForm.TranslateInterface(WinFormsTranslator.CurrentLanguage);

            // ALM Settings
            ShowdownSetLoader.SetAPILegalitySettings();

            // Match PKHeX Versioning
            if (Priority == 0)
                CheckVersionUpdates();
        }

        private void CheckVersionUpdates()
        {
            var latest_alm = PKHeX.Core.AutoMod.AutoMod.NetUtil.GetLatestALMVersion();
            var curr_valid = Version.TryParse(VERSION, out var current_alm);
            var curr_pkhex = Assembly.GetEntryAssembly().GetName().Version!;
            if (!curr_valid || curr_pkhex == null || latest_alm == null)
                return;
            var msg = $"Update for ALM is available. Please download it from GitHub. The updated release is only compatible with PKHeX Version: {latest_alm.Major}.{latest_alm.Minor}.{latest_alm.Build}.";
            if (curr_pkhex > current_alm)
                PossibleVersionMismatch = true;
            if (latest_alm > current_alm && !PossibleVersionMismatch)
                WinFormsUtil.Alert(msg);
            if (latest_alm > current_alm && PossibleVersionMismatch)
                WinFormsUtil.Alert(msg + " There is also a possible version mismatch between the current ALM version and current PKHeX version");
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            if (items.Find(ParentMenuParent, false)[0] is not ToolStripDropDownItem tools)
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
            return new(ParentMenuText)
            {
                Image = Resources.menuautolegality,
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
