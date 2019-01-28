using System;
using System.Windows.Forms;
using AutoLegalityMod;

namespace ExportTrainerData
{
    public class ExportTrainerData : AutoModPlugin
    {
        public override string Name => "Export Trainer Data";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += ExportData;
            ctrl.Image = Properties.Resources.exporttrainerdata;
        }

        private void ExportData(object sender, EventArgs e)
        {
            var complete = TrainerDataExporter.ExportTextFile(PKMEditor.PreparePKM());
            var result = complete
                ? "trainerdata.txt Successfully Exported in the same directory as PKHeX"
                : "Some of the fields were wrongly filled. Exported the default trainerdata.txt";
            WinFormsUtil.Alert(result);
        }
    }
}
