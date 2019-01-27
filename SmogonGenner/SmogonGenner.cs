using System;
using System.Windows.Forms;
using AutoLegalityMod;
using PKHeX.Core;

namespace SmogonGenner
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
            ctrl.Image = SmogonGennerResources.smogongenner;
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
                MessageBox.Show($"An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: {ex}");
                return;
            }

            if (info.ShowdownSets.Length == 0)
            {
                MessageBox.Show("No movesets available. Perhaps you could help out? Check the Contributions & Corrections forum.\n\nForum: https://www.smogon.com/forums/forums/contributions-corrections.388/");
                return;
            }

            try { AutomaticLegality.ImportModded(info.ShowdownSets); }
            catch { MessageBox.Show("Something went wrong"); }

            MessageBox.Show(info.Summary);
        }
    }
}
