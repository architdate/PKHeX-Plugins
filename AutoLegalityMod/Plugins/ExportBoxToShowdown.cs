using System;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class ExportBoxToShowdown : AutoModPlugin
    {
        public override string Name => "Export Box to ALM Showdown Template";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.exportboxtoshowdown };
            ctrl.Click += (s, e) => Export(SaveFileEditor);
            ctrl.Name = "Menu_ExportBoxtoShowdown";
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void Export(ISaveFileProvider provider)
        {
            try
            {
                var str = provider.GetRegenSetsFromBoxCurrent();
                if (string.IsNullOrWhiteSpace(str))
                    return;
                Clipboard.SetText(str);
                WinFormsUtil.Alert("Exported the active box to RegenTemplate format");
            }
            catch (Exception e)
            {
                WinFormsUtil.Error("Unable to export text to clipboard.", e.Message);
            }
        }
    }
}
