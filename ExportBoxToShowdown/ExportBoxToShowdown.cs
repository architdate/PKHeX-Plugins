using PKHeX.Core;
using System.Windows.Forms;
using AutoLegalityMod;

namespace ExportBoxToShowdown
{
    public class ExportBoxToShowdown : AutoModPlugin
    {
        public override string Name => "Export Box to Showdown";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += (s, e) => Export(SaveFileEditor.SAV);
            ctrl.Image = Properties.Resources.exportboxtoshowdown;
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
            catch { }
        }
    }
}
