using System;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class URLGenning : AutoModPlugin
    {
        public override string Name => "Gen from URL";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Properties.Resources.urlimport };
            ctrl.Click += URLGen;
            ctrl.Name = "Menu_URLGenning";
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void URLGen(object sender, EventArgs e)
        {
            var url = Clipboard.GetText().Trim();
            TeamPasteInfo info;
            try
            {
                info = new TeamPasteInfo(url);
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error("An error occured while trying to obtain the contents of the URL.", $"The exact error is as follows: {ex}");
                return;
            }
            if (!info.Valid)
            {
                WinFormsUtil.Error("The data inside the URL are not valid Showdown Sets");
                return;
            }
            if (info.Source == TeamPasteInfo.PasteSource.None)
            {
                WinFormsUtil.Error("The URL provided is not from a supported website.");
                return;
            }

            ShowdownSetLoader.Import(info.Sets);

            var response = $"All sets genned from the following URL: {info.URL}";
            WinFormsUtil.Alert(response, info.Summary);
        }
    }
}
