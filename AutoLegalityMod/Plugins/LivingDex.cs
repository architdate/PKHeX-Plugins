using System;
using System.Windows.Forms;
using PKHeX.Core;

namespace AutoLegalityMod
{
    public class LivingDex : AutoModPlugin
    {
        public override string Name => "Generate Living Dex";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += GenLivingDex;
            ctrl.Image = Properties.Resources.livingdex;
        }

        private void GenLivingDex(object sender, EventArgs e)
        {
            var sav = SaveFileEditor.SAV;
            var pkms = sav.GenerateLivingDex();
            var bd = sav.BoxData;
            pkms.CopyTo(bd);
            sav.BoxData = bd;
            SaveFileEditor.ReloadSlots();
        }
    }
}
