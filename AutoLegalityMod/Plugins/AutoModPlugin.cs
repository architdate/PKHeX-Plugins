using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    /// <summary>
    /// Base plugin logic; automatically adds plugin info
    /// </summary>
    public abstract class AutoModPlugin : IPlugin
    {
        private const string VERSION = ALMVersion.CurrentVersion;

        private const string ParentMenuName = "Menu_AutoLegality";
        private const string ParentMenuText = "Auto-Legality Mod";
        private const string ParentMenuParent = "Menu_Tools";
        private const string LoggingPrefix = "[Auto-Legality Mod]";

        public bool PossibleVersionMismatch;

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
            Debug.WriteLine($"{LoggingPrefix} Loading {Name}");
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);

            // Match PKHeX Versioning and ALM Settings only on parent plugin
            if (Priority != 0)
                return;

            Task.Run(SetUpEnvironment);
        }

        private async Task SetUpEnvironment()
        {
            ShowdownSetLoader.SetAPILegalitySettings();
            await TranslateInterface().ConfigureAwait(false);
            CheckVersionUpdates();
        }

        private async Task TranslateInterface()
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var form = ((ContainerControl)SaveFileEditor).ParentForm;
            if (form is null)
                return;

            // wait for all plugins to be loaded
            while (!form.IsHandleCreated)
                await Task.Delay(1_000).ConfigureAwait(false);

            await Task.Delay(3_000).ConfigureAwait(false);
            form.Invoke(() => form.TranslateInterface(WinFormsTranslator.CurrentLanguage));
            Debug.WriteLine($"{LoggingPrefix} Translated form.");
        }

        private void CheckVersionUpdates()
        {
            var latest_alm = PKHeX.Core.AutoMod.NetUtil.GetLatestALMVersion();
            var curr_valid = Version.TryParse(VERSION, out var current_alm);
            var curr_pkhex = Assembly.GetEntryAssembly()?.GetName().Version;
            if (!curr_valid || curr_pkhex == null || latest_alm == null)
                return;

            if (curr_pkhex > current_alm)
                PossibleVersionMismatch = true;
            if (latest_alm <= current_alm)
                return;

            // ReSharper disable once SuspiciousTypeConversion.Global
            var msg = $"Update for ALM is available. Please download it from GitHub. The updated release is only compatible with PKHeX version: {latest_alm.Major}.{latest_alm.Minor}.{latest_alm.Build}.";
            if (PossibleVersionMismatch)
                msg += "\n\nThere is also a possible version mismatch between the current ALM version and current PKHeX version.";
            const string redirect = "Click on the GitHub button to get the latest update for ALM.\nClick on the Discord button if you still require further assistance.";

            var res = WinFormsUtil.ALMError(msg, redirect);
            if (res == DialogResult.Yes)
                Process.Start("https://discord.gg/tDMvSRv");
            else if (res == DialogResult.No)
                Process.Start("https://github.com/architdate/PKHeX-Plugins/releases/latest");
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

        private static ToolStripMenuItem CreateBaseGroupItem() => new(ParentMenuText)
        {
            Image = Resources.menuautolegality,
            Name = ParentMenuName,
        };

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
