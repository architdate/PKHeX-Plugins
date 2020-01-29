using System;
using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    public partial class InjectPKM : Form
    {
        public int boxval = 1;
        public int slotval = 1;
        public InjectPKM()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            boxval = int.Parse(boxnum.Text);
            slotval = int.Parse(slotnum.Text);
            this.Close();
        }
    }
}
