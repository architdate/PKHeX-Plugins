using System;
using System.Windows.Forms;
using PKHeX.Core;
using SmogonGenner;

namespace AutoLegalityMod
{
    public class SmogonGenner : AutoModPlugin
    {
        public override string Name => "Gen Smogon Sets";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += SmogonGenning;
            ctrl.Image = Properties.Resources.smogongenner;
        }

        private void SmogonGenning(object sender, EventArgs e)
        {
            PKM rough = PKMEditor.PreparePKM();
            GenSmogonSets(rough);
        }

        private static void GenSmogonSets(PKM rough)
        {
            SmogonSetList info;
            try
            {
                info = new SmogonSetList(rough);
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error($"An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: {ex}");
                return;
            }

            if (info.ShowdownSets.Length == 0)
            {
                WinFormsUtil.Error("No movesets available. Perhaps you could help out? Check the Contributions & Corrections forum.\n\nForum: https://www.smogon.com/forums/forums/contributions-corrections.388/");
                return;
            }

            try { AutomaticLegality.ImportModded(info.ShowdownSets); }
            catch { WinFormsUtil.Error("Something went wrong"); }

            WinFormsUtil.Alert(info.Summary);
        }
    }
}
