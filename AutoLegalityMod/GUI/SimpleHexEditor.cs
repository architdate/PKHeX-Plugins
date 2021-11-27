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
                RTB_RAM.Invoke((MethodInvoker)delegate { RTB_RAM.Text = string.Join(" ", result.Select(z => $"{z:X2}")); });
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
                RTB_RAM.Text = string.Join(" ", Bytes.Select(z => $"{z:X2}"));
                refresh.Stop();
            }
        }

        private void SimpleHexEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            refresh.Stop();
        }
    }
}
