using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core.Enhancements;

namespace AutoModPlugins
{
    public class URLGenning : AutoModPlugin
    {
        public override string Name => "Gen from URL";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.urlimport };
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
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                WinFormsUtil.Error("An error occurred while trying to obtain the contents of the URL.", $"The exact error is as follows: {ex}");
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
