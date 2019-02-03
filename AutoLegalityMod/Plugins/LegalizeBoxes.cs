using System;
using PKHeX.Core;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class LegalizeBoxes : AutoModPlugin
    {
        public override string Name => "Legalize Active Pokemon";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Properties.Resources.legalizeboxes };
            ctrl.Click += Legalize;
            modmenu.DropDownItems.Add(ctrl);
        }

        private void Legalize(object sender, EventArgs e)
        {
            bool box = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            if (!box)
            {
                LegalizeActive();
                return;
            }

            bool all = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            if (!all)
            {
                LegalizeCurrent();
                return;
            }
            LegalizeAllBoxes();
        }

        private void LegalizeCurrent()
        {
            var sav = SaveFileEditor.SAV;
            if (!sav.LegalizeBox(sav.CurrentBox))
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert("Legalized Current Box's Pokémon!");
        }

        private void LegalizeAllBoxes()
        {
            var sav = SaveFileEditor.SAV;
            if (!sav.LegalizeBoxes())
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert("Legalized All Box Pokémon!");
        }

        private void LegalizeActive()
        {
            var pk = PKMEditor.PreparePKM();
            var la = new LegalityAnalysis(pk);
            if (la.Valid)
                return;

            var result = Legalizer.Legalize(pk);
            PKMEditor.PopulateFields(result);
            WinFormsUtil.Alert("Legalized Active Pokemon!");
        }
    }
}
