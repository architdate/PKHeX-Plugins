using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    public partial class ALMSettings : Form
    {
        public ALMSettings(object obj)
        {
            InitializeComponent();
            PG_Settings.SelectedObject = obj;

            this.CenterToForm(FindForm());
        }

        private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
                Close();
        }
    }
}