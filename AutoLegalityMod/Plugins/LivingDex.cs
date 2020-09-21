using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class LivingDex : AutoModPlugin
    {
        public override string Name => "Generate Living Dex";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.livingdex };
            ctrl.Click += GenLivingDex;
            ctrl.Name = "Menu_LivingDex";
            modmenu.DropDownItems.Add(ctrl);
        }

        private void GenLivingDex(object sender, EventArgs e)
        {
            if (DialogResult.Yes != WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Generate a Living Dex?")) return;
            var sav = SaveFileEditor.SAV;
            var pkms = sav.GenerateLivingDex();
            var bd = sav.BoxData;
            pkms.CopyTo(bd);
            sav.BoxData = bd;
            SaveFileEditor.ReloadSlots();
        }
    }
}
