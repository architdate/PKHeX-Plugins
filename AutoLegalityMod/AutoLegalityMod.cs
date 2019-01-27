using PKHeX.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AutoLegalityMod
{
    public class AutoMod : IPlugin
    {
        /// <summary>
        /// Main Plugin Variables
        /// </summary>
        public string Name => "Import with Auto-Legality Mod";
        public int Priority => 0;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }

        public void Initialize(params object[] args)
        {
            Debug.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null) return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            API.SAV = SaveFileEditor.SAV;
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
            ctrl.Click += ClickShowdownImportPKMModded;
            ctrl.Name = "Menu_AutoLegalityMod";
            ctrl.Image = AutoLegalityResources.autolegalitymod;
            ctrl.ShortcutKeys = (Keys.Control | Keys.I);
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
            API.SAV = SaveFileEditor.SAV;
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }

        public void ClickShowdownImportPKMModded(object sender, EventArgs e)
        {
            AutomaticLegality.PKMEditor = PKMEditor;
            AutomaticLegality.SaveFileEditor = SaveFileEditor;

            // Check for showdown data in clipboard
            var text = GetTextShowdownData();
            if (string.IsNullOrWhiteSpace(text))
                return;
            AutomaticLegality.ImportModded(text);
        }

        /// <summary>
        /// Check whether the showdown text is supposed to be loaded via a text file. If so, set the clipboard to its contents.
        /// </summary>
        /// <returns>output boolean that tells if the data provided is valid or not</returns>
        private static string GetTextShowdownData()
        {
            bool skipClipboardCheck = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            if (!skipClipboardCheck && Clipboard.ContainsText())
            {
                var txt = Clipboard.GetText();
                if (IsTextShowdownData(txt))
                    return txt;
            }

            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { "txt" }, out string path))
            {
                WinFormsUtil.Alert("No data provided.");
                return null;
            }

            var text = File.ReadAllText(path).TrimEnd();
            if (IsTextShowdownData(text))
                return text;

            WinFormsUtil.Alert("Text file with invalid data provided. Please provide a text file with proper Showdown data");
            return null;
        }

        /// <summary>
        /// Checks the input text is a showdown set or not
        /// </summary>
        /// <returns>boolean of the summary</returns>
        private static bool IsTextShowdownData(string source)
        {
            if (AutomaticLegality.IsTeamBackup(source))
                return true;
            string[] stringSeparators = { "\n\r" };

            var result = source.Split(stringSeparators, StringSplitOptions.None);
            return new ShowdownSet(result[0]).Species >= 0;
        }
    }
}
