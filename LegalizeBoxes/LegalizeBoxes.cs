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
            API.SAV = SaveFileEditor.SAV;

            var BoxData = SaveFileEditor.SAV.BoxData;
            bool box = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            for (int i = 0; i < 30; i++)
            {
                PKM illegalPK = PKMEditor.PreparePKM();

                if (box && BoxData.Count > (SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i)
                    illegalPK = BoxData[(SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i];

                if (illegalPK.Species > 0 && !new LegalityAnalysis(illegalPK).Valid)
                {
                    var result = AutomaticLegality.Legalize(illegalPK);
                    if (box)
                    {
                        BoxData[(SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i] = result;
                    }
                    else
                    {
                        PKMEditor.PopulateFields(result);
                        WinFormsUtil.Alert("Legalized Active Pokemon.");
                        return;
                    }
                }
            }
            SaveFileEditor.SAV.BoxData = BoxData;
            SaveFileEditor.ReloadSlots();
            WinFormsUtil.Alert("Legalized Box Pokemon");
        }
    }
}
