using System;
using System.IO;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    /// <summary>
    /// Main Plugin with clipboard import calls
    /// </summary>
    public class PasteImporter : AutoModPlugin
    {
        // TODO: Check for Auto Legality Mod Updates
        public override string Name => "Import with Auto-Legality Mod";
        public override int Priority => 0;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name)
            {
                Image = Properties.Resources.autolegalitymod,
                ShortcutKeys = Keys.Control | Keys.I
            };
            ctrl.Click += ImportPaste;
            modmenu.DropDownItems.Add(ctrl);

            var parent = modmenu.OwnerItem;
            var form = parent.GetCurrentParent().Parent.FindForm();
            form.Icon = Properties.Resources.icon;

            ShowdownSetLoader.PKMEditor = PKMEditor;
            ShowdownSetLoader.SaveFileEditor = SaveFileEditor;
        }

        public override void NotifySaveLoaded()
        {
            base.NotifySaveLoaded();
            API.SAV = SaveFileEditor.SAV;
        }

        private static void ImportPaste(object sender, EventArgs e)
        {
            // Check for showdown data in clipboard
            var text = GetTextShowdownData();
            if (string.IsNullOrWhiteSpace(text))
                return;
            ShowdownSetLoader.Import(text);
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
                if (ShowdownUtil.IsTextShowdownData(txt))
                    return txt;
            }

            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { "txt" }, out string path))
            {
                WinFormsUtil.Alert("No data provided.");
                return null;
            }

            var text = File.ReadAllText(path).TrimEnd();
            if (ShowdownUtil.IsTextShowdownData(text))
                return text;

            WinFormsUtil.Alert("Text file with invalid data provided. Please provide a text file with proper Showdown data");
            return null;
        }
    }
}
