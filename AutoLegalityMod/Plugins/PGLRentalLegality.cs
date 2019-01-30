using System;
using System.Linq;
using System.Windows.Forms;

namespace AutoModPlugins
{
    public class PGLRentalLegality : AutoModPlugin
    {
        public override string Name => "Import PGL QR code";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += PGLShowdownSet;
            ctrl.Image = Properties.Resources.pglqrcode;
            ctrl.ShortcutKeys = Keys.Alt | Keys.Q;
        }

        private static void PGLShowdownSet(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsImage())
                return;
            var img = Clipboard.GetImage();

            var rentalTeam = new QRParser().DecryptQRCode(img);
            var sets = rentalTeam.ConvertedTeam.ToList();
            AutomaticLegality.ImportModded(sets);
        }
    }
}
