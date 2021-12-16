using System;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using PKHeX.Core.Injection;

namespace AutoModPlugins.GUI
{
    public partial class SimpleHexEditor : Form
    {
        public byte[] Bytes;
        private System.Timers.Timer refresh = new System.Timers.Timer();
        private static ulong address = 0;
        private RWMethod method = RWMethod.Heap;
        private PokeSysBotMini? psb = null;

        public SimpleHexEditor(byte[] originalBytes, PokeSysBotMini? bot = null, ulong addr = 0, RWMethod rwm = RWMethod.Heap)
        {
            InitializeComponent();
            this.TranslateInterface(WinFormsTranslator.CurrentLanguage);
            PG_BlockView.Size = RTB_RAM.Size;
            refresh.Interval = 1000;
            refresh.Elapsed += new ElapsedEventHandler(AutoRefresh);
            refresh.AutoReset = false;
            if (addr == 0 || bot == null)
                CB_AutoRefresh.Enabled = false;
            Bytes = originalBytes;
            address = addr;
            method = rwm;
            psb = bot;
            CB_CopyMethod.DataSource = Enum.GetValues(typeof(CopyMethod)).Cast<CopyMethod>();
            CB_CopyMethod.SelectedItem = CopyMethod.Bytes;
            RTB_RAM.Text = string.Join(" ", originalBytes.Select(z => $"{z:X2}"));
            Bytes = originalBytes;
        }

        public void AutoRefresh(object source, ElapsedEventArgs e)
        {
            try
            {
                var length = Bytes.Length;
                byte[] result;
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (psb.com is not ICommunicatorNX cnx) result = psb.com.ReadBytes(address, length);
                else
                {
                    if (method == RWMethod.Main) result = cnx.ReadBytesMain(address, length);
                    else if (method == RWMethod.Absolute) result = cnx.ReadBytesAbsolute(address, length);
                    else result = psb.com.ReadBytes(address, length);
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                var r_text = string.Join(" ", result.Select(z => $"{z:X2}"));
                RTB_RAM.Invoke((MethodInvoker)delegate { 
                    if (RTB_RAM.Text != r_text) // Prevent text updates if there is no update since they hinder copying
                        RTB_RAM.Text = r_text; 
                });
                refresh.Start();
            }
            catch // Execution stopped mid thread
            {
                refresh.Start();
                return;
            }
        }

        private void Update_Click(object sender, EventArgs e)
        {
            var bytestring = RTB_RAM.Text.Replace("\t", "").Replace(" ", "").Trim();
            Bytes = Decoder.StringToByteArray(bytestring);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ChangeCopyMethod(object sender, EventArgs e)
        {
            RTB_RAM.method = (CopyMethod)CB_CopyMethod.SelectedItem;
        }

        private void CB_AutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            if (CB_AutoRefresh.Checked)
            {
                B_Update.Enabled = false;
                RTB_RAM.ReadOnly = true;
                refresh.Start();
            }
            else
            {
                B_Update.Enabled = true;
                RTB_RAM.ReadOnly = false;
                // RTB_RAM.Text = string.Join(" ", Bytes.Select(z => $"{z:X2}")); // set back to the original value
                refresh.Stop();
            }
        }

        private void SimpleHexEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            refresh.Stop();
        }
    }

    internal class HexRichTextBox : RichTextBox
    {
        public CopyMethod method = CopyMethod.Bytes;

        protected override bool ProcessCmdKey(ref Message msg, Keys e)
        {
            bool ctrlV = e == (Keys.Control | Keys.V);
            bool shiftIns = e == (Keys.Shift | Keys.Insert);
            bool ctrlC = e == (Keys.Control | Keys.C);
            bool ctrlX = e == (Keys.Control | Keys.X);

            if (method == CopyMethod.Integers && (ctrlV || shiftIns))
            {
                var text = Clipboard.GetText();
                if (text != null)
                {
                    var split = new string[text.Length / 2 + (text.Length % 2 == 0 ? 0 : 1)];
                    for (int i = 0; i < split.Length; i++)
                        split[i] = text.Substring(i * 2, i * 2 + 2 > text.Length ? 1 : 2);
                    Clipboard.SetText(string.Join(" ", split));
                }
            }
            var handled = base.ProcessCmdKey(ref msg, e);
            if (method == CopyMethod.Integers)
            {
                if (ctrlC || ctrlX)
                {
                    if (string.IsNullOrWhiteSpace(SelectedText))
                        return false;
                    Clipboard.SetText(string.Join(string.Empty, SelectedText.Split(' ').Reverse()));
                    return true;
                }
            }
            return handled;
        }
    }

    internal enum CopyMethod
    {
        Bytes,
        Integers,
    }
}
