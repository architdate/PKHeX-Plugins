using System;
using System.Linq;
using System.Windows.Forms;
using QRPlugins;

namespace AutoModPlugins
{
    public class PGLRentalLegality : AutoModPlugin
    {
        public override string Name => "Import PGL QR code";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name)
            {
                Image = QRResources.pglqrcode,
                ShortcutKeys = Keys.Alt | Keys.Q,
            };
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += PGLShowdownSet;
        }

        private static void PGLShowdownSet(object? sender, EventArgs e)
        {
            if (!Clipboard.ContainsImage())
                return;

            var img = Clipboard.GetImage();
            if (img is null)
                return;

            var rentalTeam = QRParser.DecryptQRCode(img);
            if (rentalTeam is null)
                return;

            var sets = rentalTeam.ConvertedTeam.ToList();
            ShowdownSetLoader.Import(sets);
        }
    }
}
