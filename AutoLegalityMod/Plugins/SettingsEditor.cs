using AutoModPlugins.GUI;
using System;
using System.Windows.Forms;

namespace AutoModPlugins
{
    public class SettingsEditor : AutoModPlugin
    {
        public override string Name => "Plugin Settings";
        public override int Priority => 3;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            Properties.AutoLegality.Default.Upgrade();
            var ctrl = new ToolStripMenuItem(Name) { Image = Properties.Resources.settings };
            ctrl.Click += SettingsForm;
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void SettingsForm(object sender, EventArgs e)
        {
            var settings = Properties.AutoLegality.Default;
            using var form = new ALMSettings(settings);
            form.ShowDialog();
            settings.Save();
        }
    }
}

