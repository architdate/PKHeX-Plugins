using System;
using System.Windows.Forms;
using AutoModPlugins.GUI;
using AutoModPlugins.Properties;

namespace AutoModPlugins
{
    public class SettingsEditor : AutoModPlugin
    {
        public override string Name => "Plugin Settings";
        public override int Priority => 3;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name) { Image = Resources.settings };
            ctrl.Click += SettingsForm;
            ctrl.Name = "Menu_ALMSettingsEditor";
            modmenu.DropDownItems.Add(ctrl);
        }

        private static void SettingsForm(object? sender, EventArgs e)
        {
            using var form = new ALMSettings(_settings);
            form.ShowDialog();
        }
    }
}

