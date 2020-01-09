using System;
using System.Windows.Forms;

namespace AutoModPlugins
{
    public class UploadGPSS : AutoModPlugin
    {
        public override string Name => "Upload to GPSS";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Properties.Resources.legalizeboxes };
            ctrl.Click += GPSSUpload;
            modmenu.DropDownItems.Add(ctrl);
        }

        private void GPSSUpload(object sender, EventArgs e)
        {
            var pk = PKMEditor.PreparePKM();
            byte[] rawdata = pk.Data;
            var postval = PKHeX.Core.AutoMod.NetUtil.GPSSPost(rawdata);
            WinFormsUtil.Alert(postval);
        }

    }
}