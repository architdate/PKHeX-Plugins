using PKHeX.Core;
using System;
using System.IO;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoLegalityMod
{
    /// <summary>
    /// Main Plugin with clipboard import calls
    /// </summary>
    public class AutoLegalityMod : AutoModPlugin
    {
        public override string Name => "Import with Auto-Legality Mod";
        public override int Priority => 0;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += ClickShowdownImportPKMModded;
            ctrl.Name = "Menu_AutoLegalityMod";
            ctrl.Image = Properties.Resources.autolegalitymod;
            ctrl.ShortcutKeys = Keys.Control | Keys.I;

            var parent = modmenu.OwnerItem;
            var form = parent.GetCurrentParent().Parent.FindForm();
            form.Icon = Properties.Resources.icon;

            AutomaticLegality.PKMEditor = PKMEditor;
            AutomaticLegality.SaveFileEditor = SaveFileEditor;
        }

        public override void NotifySaveLoaded()
        {
            base.NotifySaveLoaded();
            API.SAV = SaveFileEditor.SAV;
        }

        public void ClickShowdownImportPKMModded(object sender, EventArgs e)
        {
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
            if (ShowdownUtil.IsTeamBackup(source))
                return true;
            string[] stringSeparators = { "\n\r" };

            var result = source.Split(stringSeparators, StringSplitOptions.None);
            return new ShowdownSet(result[0]).Species >= 0;
        }
    }
}
