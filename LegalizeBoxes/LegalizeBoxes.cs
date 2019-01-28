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
                    ShowdownSet Set = new ShowdownSet(ShowdownSet.GetShowdownText(illegalPK));

                    PKM APIGenerated = SaveFileEditor.SAV.BlankPKM;
                    bool satisfied = false;
                    try { APIGenerated = API.APILegality(illegalPK, Set, out satisfied); }
                    catch { }

                    var trainer = illegalPK.GetRoughTrainerData();
                    PKM legal;
                    if (!satisfied)
                    {
                        bool resetForm = ShowdownUtil.IsInvalidForm(Set.Form);
                        legal = BruteForce.ApplyDetails(illegalPK, Set, resetForm, trainer);
                    }
                    else
                    {
                        legal = APIGenerated;
                    }
                    legal.SetTrainerData(trainer, satisfied);

                    if (box)
                    {
                        BoxData[(SaveFileEditor.CurrentBox * SaveFileEditor.SAV.BoxSlotCount) + i] = legal;
                    }
                    else
                    {
                        PKMEditor.PopulateFields(legal);
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
