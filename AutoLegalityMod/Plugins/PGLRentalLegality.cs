using System;
using System.Collections.Generic;
using System.Drawing;
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

        private void PGLShowdownSet(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsImage())
                return;
            var img = Clipboard.GetImage();
            var sets = GetSetsFromPGLQR(img);
            AutomaticLegality.ImportModded(sets);
        }

        private static IEnumerable<string> GetSetsFromPGLQR(Image img)
        {
            var rentalTeam = new QRParser().DecryptQRCode(img);
            return rentalTeam.Team.Select(z => z.ToShowdownFormat(false));
        }
    }
}
