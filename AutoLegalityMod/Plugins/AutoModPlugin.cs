using AutoModPlugins.GUI;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoModPlugins
{
    /// <summary>
    /// Base plugin logic; automatically adds plugin info
    /// </summary>
    public abstract class AutoModPlugin : IPlugin
    {
        private const string ParentMenuName = "Menu_AutoLegality";
        private const string ParentMenuText = "Auto-Legality Mod";
        private const string ParentMenuParent = "Menu_Tools";
        private const string LoggingPrefix = "[Auto-Legality Mod]";

        private readonly CancellationTokenSource Source = new();

        /// <summary>
        /// Main Plugin Variables
        /// </summary>
        public abstract string Name { get; }
        public abstract int Priority { get; }

        // Initialized during plugin startup
        public ISaveFileProvider SaveFileEditor { get; private set; } = null!;
        protected IPKMView PKMEditor { get; private set; } = null!;
        internal static readonly string almconfig = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "almconfig.json");
        internal static PluginSettings _settings = new() { ConfigPath = almconfig };

        public void Initialize(params object[] args)
        {
            Debug.WriteLine($"{LoggingPrefix} Loading {Name}");
            SaveFileEditor = (ISaveFileProvider)(Array.Find(args, z => z is ISaveFileProvider) ?? throw new Exception("Null ISaveFileProvider"));
            PKMEditor = (IPKMView)(Array.Find(args, z => z is IPKMView) ?? throw new Exception("Null IPKMView"));
            var menu = (ToolStrip)(Array.Find(args, z => z is ToolStrip) ?? throw new Exception("Null ToolStrip"));
            LoadMenuStrip(menu);

            // Load settings
            if (File.Exists(_settings.ConfigPath))
            {
                var text = File.ReadAllText(_settings.ConfigPath);
                _settings = JsonSerializer.Deserialize<PluginSettings>(text)!;
            }

            // Match PKHeX Versioning and ALM Settings only on parent plugin
            if (Priority != 0)
                return;

            Task.Run(async () =>
            {
                var (hasError, error) = await SetUpEnvironment(Source.Token).ConfigureAwait(false);
                if (hasError && error is not null)
                {
                    if (error.InvokeRequired)
                        error.Invoke(() => ShowAlmErrorDialog(error, menu));
                    else ShowAlmErrorDialog(error, menu);
                }
            }, Source.Token);
        }

        private static void ShowAlmErrorDialog(ALMError error, ToolStrip menu)
        {
            SystemSounds.Hand.Play();
            var res = error.ShowDialog(menu);
            if (res == DialogResult.Retry)
                Process.Start(new ProcessStartInfo { FileName = "https://github.com/architdate/PKHeX-Plugins/wiki/Installing-PKHeX-Plugins#manual-installation-or-installing-older-releases", UseShellExecute = true });
        }

        private async Task<(bool, ALMError?)> SetUpEnvironment(CancellationToken token)
        {
            ShowdownSetLoader.SetAPILegalitySettings(_settings);
            await TranslateInterface(token).ConfigureAwait(false);
            return CheckForMismatch();
        }

        private async Task TranslateInterface(CancellationToken token)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var form = ((ContainerControl)SaveFileEditor).ParentForm;
            if (form is null)
                return;

            // wait for all plugins to be loaded
            while (!form.IsHandleCreated)
                await Task.Delay(0_100, token).ConfigureAwait(false);

            if (form.InvokeRequired)
                form.Invoke(() => form.TranslateInterface(WinFormsTranslator.CurrentLanguage));
            else form.TranslateInterface(WinFormsTranslator.CurrentLanguage);
            Debug.WriteLine($"{LoggingPrefix} Translated form.");
        }

        private static (bool, ALMError?) CheckForMismatch()
        {
            bool mismatch = ALMVersion.GetIsMismatch();
            bool reset = ALMVersion.Versions.CoreVersionCurrent > new Version(_settings.LatestAllowedVersion);
            if (reset)
                _settings.LatestAllowedVersion = "0.0.0.0";

            _settings.EnableDevMode = _settings.EnableDevMode && !mismatch;
            if (mismatch || reset)
                _settings.Save();

            return (mismatch, mismatch ? WinFormsUtil.ALMErrorMismatch(ALMVersion.Versions) : null);
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
