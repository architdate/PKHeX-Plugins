using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    public partial class ALMError : Form
    {
        public ALMError(string[] error_txt, string[] buttons)
        {
            if (buttons.Length > 4)
                throw new ArgumentOutOfRangeException("Only expect 4 buttons at maximum");
            if (buttons.Length == 0)
                throw new ArgumentOutOfRangeException("Need atleast 1 button");
            InitializeComponent();
            label1.Text = string.Join(Environment.NewLine + Environment.NewLine, error_txt);
            var btn_ctrls = new[] { BTN1, BTN2, BTN3, BTN4 };
            buttons = buttons.Reverse().ToArray();
            var btn_loc = label1.Location.Y + label1.Size.Height + 10;
            var height_diff = btn_loc - BTN1.Location.Y;
            for (int i = 0; i < buttons.Length; i++)
            {
                btn_ctrls[i].Visible = true;
                btn_ctrls[i].Enabled = true;
                btn_ctrls[i].Text = buttons[i];
                //height_diff = btn_loc - btn_ctrls[i].Location.Y;
                //btn_ctrls[i].Location = new Point(btn_ctrls[i].Location.X, btn_loc);
            }
            Size = new Size(Size.Width, Size.Height + height_diff);
        }

        private void BTN4_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void BTN3_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        private void BTN2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Retry;
            Close();
        }

        private void BTN1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
