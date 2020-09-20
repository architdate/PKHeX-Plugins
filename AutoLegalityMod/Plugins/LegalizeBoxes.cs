using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class LegalizeBoxes : AutoModPlugin
    {
        public override string Name => "Legalize Active Pokemon";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.legalizeboxes };
            ctrl.Click += Legalize;
            ctrl.Name = "Menu_LeaglizeBoxes";
            modmenu.DropDownItems.Add(ctrl);
        }

        private void Legalize(object sender, EventArgs e)
        {
            var box = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            if (!box)
            {
                LegalizeActive();
                return;
            }

            var all = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            if (!all)
                LegalizeCurrent();
            else
                LegalizeAllBoxes();
        }

        private void LegalizeCurrent()
        {
            var sav = SaveFileEditor.SAV;
            var count = sav.LegalizeBox(sav.CurrentBox);
            if (count <= 0) // failed to modify anything
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert($"Legalized {count} Pokémon in Current Box!");
        }

        private void LegalizeAllBoxes()
        {
            var sav = SaveFileEditor.SAV;
            var count = sav.LegalizeBoxes();
            if (count <= 0) // failed to modify anything
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert($"Legalized {count} Pokémon across all boxes!");
        }

        private void LegalizeActive()
        {
            var pk = PKMEditor.PreparePKM();
            var la = new LegalityAnalysis(pk);
            if (la.Valid)
                return; // already valid, don't modify it

            var sav = SaveFileEditor.SAV;
            var result = sav.Legalize(pk);

            // let's double check

            la = new LegalityAnalysis(result);
            if (!la.Valid)
            {
                WinFormsUtil.Error("Unable to make the Active Pokemon legal!");
                return;
            }

            PKMEditor.PopulateFields(result);
            WinFormsUtil.Alert("Legalized Active Pokemon!");
        }
    }
}
