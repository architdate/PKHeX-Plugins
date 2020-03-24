using System.Linq;
using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    public partial class RAMEdit : Form
    {
        public byte[] modifiedBytes;
        private int length;
        public string text;

        public RAMEdit(byte[] originalBytes)
        {
            InitializeComponent();
            var editable = string.Concat(originalBytes.Select(z => $"{z:X2}  "));
            RAM.Text = editable;
            text = editable;
            length = originalBytes.Length;
        }

        private void CloseForm(object sender, FormClosingEventArgs e)
        {
            // DialogResult = DialogResult.Abort;
        }

        private void Update_Click(object sender, System.EventArgs e)
        {
            text = RAM.Text;
            var bytestring = text.Replace("\t", "").Replace(" ", "").Trim();
            var buffer = new byte[(length * 2) + 1];
            modifiedBytes = PKHeX.Core.AutoMod.Decoder.StringToByteArray(bytestring);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
