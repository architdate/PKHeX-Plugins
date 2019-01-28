using System;
using PKHeX.Core;
using AutoLegalityMod;
using System.Windows.Forms;

namespace LegalizeBoxes
{
    public class LegalizeBoxes : AutoModPlugin
    {
        public override string Name => "Legalize Active Pokemon";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += Legalize;
            ctrl.Image = LegalizeBoxesResources.legalizeboxes;
        }

        private void Legalize(object sender, EventArgs e)
        {
            bool box = (Control.ModifierKeys & Keys.Control) == Keys.Control;

            if (!box)
                LegalizeActive();
            else
                LegalizeCurrentBox();
        }

        private static bool LegalizeBox(SaveFile SAV, int box)
        {
            if ((uint)box >= SAV.BoxCount)
                return false;

            var data = SAV.GetBoxData(box);
            bool modified = false;
            for (int i = 0; i < 30; i++)
            {
                var pk = data[i];
                if (pk.Species <= 0 || new LegalityAnalysis(pk).Valid)
                    continue;
                data[i] = AutomaticLegality.Legalize(pk);
                modified = true;
            }
            if (!modified)
                return false;
            SAV.SetBoxData(data, box);
            return true;
        }

        private void LegalizeCurrentBox()
        {
            var SAV = SaveFileEditor.SAV;
            var current = SAV.CurrentBox;
            if (!LegalizeBox(SAV, current))
                return;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert("Legalized Box Pokemon");
        }

        private void LegalizeActive()
        {
            PKM illegalPK = PKMEditor.PreparePKM();
            var la = new LegalityAnalysis(illegalPK);
            if (la.Valid)
                return;

            var result = AutomaticLegality.Legalize(illegalPK);
            PKMEditor.PopulateFields(result);
            WinFormsUtil.Alert("Legalized Active Pokemon.");
        }
    }
}
