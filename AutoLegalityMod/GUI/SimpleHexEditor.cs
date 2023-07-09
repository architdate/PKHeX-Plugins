using System;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using PKHeX.Core.Injection;
using PKHeX.Core;

namespace AutoModPlugins.GUI
{
    public partial class SimpleHexEditor : Form
    {
        public byte[] Bytes;
        private readonly System.Timers.Timer refresh = new();
        private static ulong address;
        private static bool decrypt;
        private static uint block_key;
        private static int headersize;
        private readonly RWMethod method;
        private readonly PokeSysBotMini? psb;

        public SimpleHexEditor(byte[] originalBytes, PokeSysBotMini? bot = null, ulong addr = 0, RWMethod rwm = RWMethod.Heap, bool decrypt_blk = false, uint decrypt_key = 0, int header = 0)
        {
            InitializeComponent();
            this.TranslateInterface(WinFormsTranslator.CurrentLanguage);
            PG_BlockView.Size = RTB_RAM.Size;
            refresh.Interval = (double)RT_Timer.Value;
            refresh.Elapsed += AutoRefresh;
            refresh.AutoReset = false;
            if (addr == 0 || bot == null)
                CB_AutoRefresh.Enabled = RT_Timer.Enabled = RT_Label.Enabled = false;
            Bytes = originalBytes;
            address = addr;
            method = rwm;
            decrypt = decrypt_blk;
            if (decrypt)
            {
                B_Update.Enabled = false;
                RTB_RAM.ReadOnly = true;
            }
            block_key = decrypt_key;
            headersize = header;
            psb = bot;
            CB_CopyMethod.DataSource = Enum.GetValues(typeof(CopyMethod)).Cast<CopyMethod>();
            CB_CopyMethod.SelectedItem = CopyMethod.Bytes;
            RTB_RAM.Text = string.Join(" ", originalBytes.Select(z => $"{z:X2}"));
            Bytes = originalBytes;
        }

        public void AutoRefresh(object? source, ElapsedEventArgs? e)
        {
            if (!CB_AutoRefresh.Checked || psb is null)
            {
                // prevent race condition between the uncheck event and auto refresh
                refresh.Stop();
                return;
            }
            try
            {
                var length = Bytes.Length;
                byte[] result;
                if (psb.com is not ICommunicatorNX cnx)
                {
                    result = psb.com.ReadBytes(address, length);
                }
                else
                {
                    result = method switch
                    {
                        RWMethod.Main => cnx.ReadBytesMain(address, length + headersize),
                        RWMethod.Absolute => cnx.ReadBytesAbsolute(address, length + headersize),
                        _ => psb.com.ReadBytes(address, length + headersize),
                    };
                    if (decrypt)
                        result = DecryptBlock(block_key, result)[headersize..];
                }
                var r_text = string.Join(" ", result.Select(z => $"{z:X2}"));
                RTB_RAM.Invoke((MethodInvoker)delegate {
                    if (RTB_RAM.Text != r_text) // Prevent text updates if there is no update since they hinder copying
                        RTB_RAM.Text = r_text;
                });
                if (RT_Timer.Enabled)
                    RT_Timer.Invoke((MethodInvoker)delegate { refresh.Interval = (double)RT_Timer.Value; });
                refresh.Start();
            }
            catch // Execution stopped mid thread
            {
                refresh.Start();
            }
        }

        private static byte[] DecryptBlock(uint key, byte[] block)
        {
            var rng = new SCXorShift32(key);
            for (int i = 0; i < block.Length; i++)
                block[i] = (byte)(block[i] ^ rng.Next());
            return block;
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
                if (decrypt == false)
                {
                    B_Update.Enabled = true;
                    RTB_RAM.ReadOnly = false;
                }
                // RTB_RAM.Text = string.Join(" ", Bytes.Select(z => $"{z:X2}")); // set back to the original value
                refresh.Stop();
            }
        }

        private void SimpleHexEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            CB_AutoRefresh.Checked = false;
            refresh.Stop();
            while (refresh.Enabled)
            {
                try
                {
                    refresh.Stop();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    internal class HexRichTextBox : RichTextBox
    {
        public CopyMethod method;

        protected override bool ProcessCmdKey(ref Message msg, Keys e)
        {
            bool ctrlV = e == (Keys.Control | Keys.V);
            bool shiftIns = e == (Keys.Shift | Keys.Insert);
            bool ctrlC = e == (Keys.Control | Keys.C);
            bool ctrlX = e == (Keys.Control | Keys.X);

            if (method == CopyMethod.Integers && (ctrlV || shiftIns))
            {
                var text = Clipboard.GetText();
                {
                    var split = new string[(text.Length / 2) + (text.Length % 2 == 0 ? 0 : 1)];
                    for (int i = 0; i < split.Length; i++)
                        split[i] = text.Substring(i * 2, (i * 2) + 2 > text.Length ? 1 : 2);
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
                    Clipboard.SetText(string.Concat(SelectedText.Split(' ').Reverse()));
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
