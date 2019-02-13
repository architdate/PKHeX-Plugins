using System;
using System.Windows.Forms;

namespace AutoModPlugins
{
    public class ExportPKSMBank : AutoModPlugin
    {
        public override string Name => "Export PKSM Bank to PKM";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += Export;
            ctrl.Image = Properties.Resources.mgdbdownload;
        }

        private void Export(object sender, EventArgs e)
        {
            var bank = PKSMUtil.GetBankData();

            // Check for invalid bank
            if (bank == null)
            {
                WinFormsUtil.Alert("Invalid bank input");
                return;
            }

            PKSMUtil.ExportBank(bank);
            WinFormsUtil.Alert("Bank Exported");
        }
    }
}
