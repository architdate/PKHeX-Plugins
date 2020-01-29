using System;
using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    public partial class LoadBox : Form
    {
        public int Box = -1;
        public LoadBox()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Box = int.Parse(boxnum.Text);
            this.Close();
        }
    }
}
