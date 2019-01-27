using System;
using System.Windows.Forms;
using AutoLegalityMod;

namespace ExportQRCodes
{
    public class ExportQRCodes : AutoModPlugin
    {
        public override string Name => "Export QR Codes";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += ExportQRs;
            ctrl.Image = ExportQRCodesResources.exportqrcode;
        }

        private void ExportQRs(object sender, EventArgs e)
        {
            var sav = SaveFileEditor.SAV;
            if (!sav.HasBox)
            {
                MessageBox.Show("Save file does not have box data.");
                return;
            }
            var boxData = sav.GetBoxData(sav.CurrentBox);
            QRCodeDumper.DumpQRCodes(boxData);
        }
    }
}
