using System;
using PKHeX.Core;
using System.Windows.Forms;

namespace AutoModPlugins
{
    public class ExportBoxToShowdown : AutoModPlugin
    {
        public override string Name => "Export Box to Showdown";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Properties.Resources.exportboxtoshowdown };
            ctrl.Click += (s, e) => Export(SaveFileEditor.SAV);
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void Export(SaveFile sav)
        {
            try
            {
                var str = sav.GetShowdownSetsFromBoxCurrent();
                if (string.IsNullOrWhiteSpace(str))
                    return;
                Clipboard.SetText(str);
                WinFormsUtil.Alert("Exported the active box to Showdown format");
            }
            catch (Exception e)
            {
                WinFormsUtil.Error("Unable to export text to clipboard.", e.Message);
            }
        }
    }
}
