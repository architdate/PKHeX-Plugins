using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins.GUI
{
    public partial class ALMSettings : Form
    {
        public ALMSettings(object obj)
        {
            InitializeComponent();
            PG_Settings.SelectedObject = obj;
            WinFormsTranslator.TranslateInterface(this, WinFormsTranslator.CurrentLanguage);

            this.CenterToForm(FindForm());
        }

        private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
                Close();
        }

        private void ALMSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ALM Settings
            ShowdownSetLoader.SetAPILegalitySettings();

            Properties.AutoLegality.Default.Save();
        }

        private void RunBulkTests_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(TeamTest.TestPath))
            {
                WinFormsUtil.Error("Valid Test Path does not exist");
                return;
            }

            var results = TeamTest.VerifyFiles();
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "logs"));

            foreach (var res in results)
            {
                var fileName = $"{Path.GetFileName(res.Key).Replace('.', '_')}{DateTime.Now:_yyyy-MM-dd-HH-mm-ss}.log";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "logs", fileName);
                var msg = string.Join("\n\n", res.Value["illegal"].Select(x => x.Text));
                File.WriteAllText(path, msg);
            }

            var sb = new StringBuilder();
            foreach (var res in results)
                sb.Append(Path.GetFileName(res.Key)).Append(" : Legal - ").Append(res.Value["legal"].Length).Append(" | Illegal - ").Append(res.Value["illegal"].Length).AppendLine();
            WinFormsUtil.Alert(sb.ToString());
        }
    }
}
