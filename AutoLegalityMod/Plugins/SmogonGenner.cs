using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.Enhancements;

namespace AutoModPlugins
{
    public class SmogonGenner : AutoModPlugin
    {
        public override string Name => "Gen Smogon Sets";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.smogongenner };
            ctrl.Name = "Menu_SmogonGenner";
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += SmogonGenning;
        }

        private void SmogonGenning(object sender, EventArgs e)
        {
            var rough = PKMEditor.PreparePKM();
            GenSmogonSets(rough);
        }

        private static void GenSmogonSets(PKM rough)
        {
            SmogonSetList info;
            try
            {
                info = new SmogonSetList(rough);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                WinFormsUtil.Error($"An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: {ex}");
                return;
            }
#pragma warning restore CA1031 // Do not catch general exception types

            if (!info.Valid || info.Sets.Count == 0)
            {
                WinFormsUtil.Error("No movesets available. Perhaps you could help out? Check the Contributions & Corrections forum.\n\nForum: https://www.smogon.com/forums/forums/contributions-corrections.388/");
                return;
            }

            for (int i = 0; i < info.Sets.Count;)
            {
                if (DialogResult.Yes != WinFormsUtil.Prompt(MessageBoxButtons.YesNo, "Import this set?", info.SetTitle[i], info.Sets[i].Text))
                {
                    info.Sets.RemoveAt(i);
                    info.SetTitle.RemoveAt(i);
                    info.SetConfig.RemoveAt(i);
                    info.SetText.RemoveAt(i);
                }
                else
                    i++;
            }

            ShowdownSetLoader.Import(info.Sets, true);
            WinFormsUtil.Alert(info.Summary);
        }
    }
}
