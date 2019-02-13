using System.IO;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

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
            ctrl.Click += (s, e) => Export();
            ctrl.Image = Properties.Resources.mgdbdownload;
        }

        private static void Export()
        {
            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { ".bnk" }, out string path))
                return;

            var bank = File.ReadAllBytes(path);
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() != DialogResult.OK)
                    return;
                var count = PKSMUtil.ExportBank(bank, fbd.SelectedPath);
                WinFormsUtil.Alert("Bank Exported!", $"Dumped {count} Pokémon!");
            }
        }
    }
}
