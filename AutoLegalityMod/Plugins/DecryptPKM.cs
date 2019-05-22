using PKHeX.Core;
using System.IO;
using System.Windows.Forms;

namespace AutoModPlugins
{
    public class DecryptPKM : AutoModPlugin
    {
        public override string Name => "Decrypt PKM";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Properties.Resources.exportboxtoshowdown };
            ctrl.Click += (s, e) => Decrypt();
            modmenu.DropDownItems.Add(ctrl);
        }

        private void Decrypt()
        {
            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { "txt" }, out string path))
            {
                WinFormsUtil.Alert("No data provided.");
            }

            var bytes = File.ReadAllBytes(path);
            var pk = PKMConverter.GetPKMfromBytes(bytes);
            PKMEditor.PopulateFields(pk);
        }
    }
}
