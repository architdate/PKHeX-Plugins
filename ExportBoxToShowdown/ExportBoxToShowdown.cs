using PKHeX.Core;
using System;
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
            ctrl.Click += BoxToShowdown;
            ctrl.Image = ExportBoxToShowdownResources.exportboxtoshowdown;
        }

        private void BoxToShowdown(object sender, EventArgs e)
        {
            try
            {
                var str = GetShowdownSetsFromBoxCurrent(SaveFileEditor.SAV);
                if (string.IsNullOrWhiteSpace(str)) return;
                Clipboard.SetText(str);
            }
            catch { }
            MessageBox.Show("Exported the active box to Showdown format");
        }

        private static string GetShowdownSetsFromBoxCurrent(SaveFile sav) => GetShowdownSetsFromBox(sav, sav.CurrentBox);

        private static string GetShowdownSetsFromBox(SaveFile sav, int box)
        {
            var CurrBox = sav.GetBoxData(box);
            return ShowdownSet.GetShowdownSets(CurrBox, Environment.NewLine + Environment.NewLine);
        }
    }
}
