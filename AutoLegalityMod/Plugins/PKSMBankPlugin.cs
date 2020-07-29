using System.IO;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class PKSMBankPlugin : AutoModPlugin
    {
        public override string Name => "PKSM Bank Tools";
        public override int Priority => 2;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) {Name = "Menu_PKSMBank"};

            var c1 = new ToolStripMenuItem("Merge PKM into PKSM Bank") { Image = Properties.Resources.uploadgpss };
            c1.Click += (s, e) => Import(); 
            c1.Name = "Menu_CreatePKSMBank";
            var c2 = new ToolStripMenuItem("Split PKSM Bank into PKM") { Image = Properties.Resources.mgdbdownload };
            c2.Click += (s, e) => Export();
            c2.Name = "Menu_ExportPKSMBank";

            ctrl.DropDownItems.Add(c1);
            ctrl.DropDownItems.Add(c2);
            modmenu.DropDownItems.Add(ctrl);
            
            ctrl.Image = Properties.Resources.flagbrew;
        }

        private static void Export()
        {
            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { ".bnk" }, out string path))
                return;

            var bank = File.ReadAllBytes(path);
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK)
                return;
            var count = PKSMUtil.ExportBank(bank, fbd.SelectedPath, out var previews);
            PKMPreview.ExportCSV(previews, fbd.SelectedPath);
            WinFormsUtil.Alert("Bank Exported!", $"Dumped {count} Pokémon!");
        }

        private static void Import()
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.OK)
                return;
            var count = PKSMUtil.CreateBank(fbd.SelectedPath);
            WinFormsUtil.Alert("Bank Created!", $"Added {count} Pokémon to the bank!");
        }
    }
}
