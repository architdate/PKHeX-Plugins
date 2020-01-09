using System.Windows.Forms;
using AutoModPlugins.Properties;

namespace AutoModPlugins.GUI
{
    public partial class ALMSettings : Form
    {
        public ALMSettings(object obj, params string[] blacklist)
        {
            InitializeComponent();
            PG_Settings.SelectedObject = Properties.AutoLegality.Default;

            this.CenterToForm(FindForm());
        }

        private void SettingsEditor_FormClosing(object sender, FormClosingEventArgs e) => SaveSettings();

        private void SaveSettings()
        {
            AutoLegality.Default.Save();
        }

        private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
                Close();
        }
    }
}