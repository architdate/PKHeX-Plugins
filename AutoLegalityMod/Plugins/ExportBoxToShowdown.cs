using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class ExportBoxToShowdown : AutoModPlugin
    {
        public override string Name => "Export Box to Showdown";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.exportboxtoshowdown };
            ctrl.Click += (s, e) => Export(SaveFileEditor.SAV);
            ctrl.Name = "Menu_ExportBoxtoShowdown";
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void Export(SaveFile sav)
        {
            try
            {
                var str = sav.GetShowdownSetsFromBoxCurrent();
                if (string.IsNullOrWhiteSpace(str))
                    return;
                Clipboard.SetText(str);
                WinFormsUtil.Alert("Exported the active box to Showdown format");
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception e)
            {
                WinFormsUtil.Error("Unable to export text to clipboard.", e.Message);
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }
    }
}
