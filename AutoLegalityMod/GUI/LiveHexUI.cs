using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AutoModPlugins.GUI;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public partial class LiveHexUI : Form, ISlotViewer<PictureBox>
    {
        public ISaveFileProvider SAV { get; }

        public int ViewIndex => BoxSelect.SelectedIndex;
        public IList<PictureBox> SlotPictureBoxes => null;
        SaveFile ISlotViewer<PictureBox>.SAV => null;

        private readonly LiveHexController Remote;
        private readonly SaveDataEditor<PictureBox> x;

#pragma warning disable CA2213 // Disposable fields should be disposed
        private readonly ComboBox BoxSelect; // this is just us holding a reference; disposal is done by its parent
#pragma warning restore CA2213 // Disposable fields should be disposed

        public LiveHexUI(ISaveFileProvider sav, IPKMView editor)
        {
            SAV = sav;
            Remote = new LiveHexController(sav, editor);

            InitializeComponent();
            WinFormsTranslator.TranslateInterface(this, WinFormsTranslator.CurrentLanguage);

            TB_IP.Text = Properties.AutoLegality.Default.LatestIP;

            // add an event to the editor
            // ReSharper disable once SuspiciousTypeConversion.Global
            BoxSelect = ((Control)sav).Controls.Find("CB_BoxSelect", true).FirstOrDefault() as ComboBox;
            if (BoxSelect != null)
                BoxSelect.SelectedIndexChanged += ChangeBox;
            Closing += (s, e) => BoxSelect.SelectedIndexChanged -= ChangeBox;

            var type = sav.GetType();
            var fields = type.GetTypeInfo().DeclaredFields;
            var test = fields.First(z => z.Name == "EditEnv");
            x = (SaveDataEditor<PictureBox>)test.GetValue(sav);
            x.Slots.Publisher.Subscribers.Add(this);

            TB_Port.Text = Remote.Bot.Port.ToString();
            CenterToParent();
        }

        private void SetTrainerData(SaveFile sav)
        {
            switch (sav)
            {
                case SAV8SWSH s8:
                    var info = s8.MyStatus;
                    var data = Remote.Bot.ReadBytes(0x42935e48, 0x110);
                    data.CopyTo(info.Data);
                    break;
            }
        }

        private void ChangeBox(object sender, EventArgs e)
        {
            if (checkBox1.Checked && Remote.Bot.Connected)
                Remote.ChangeBox(ViewIndex);
        }

        private void B_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                Remote.Bot.IP = TB_IP.Text;
                Remote.Bot.Port = int.Parse(TB_Port.Text);
                Remote.Bot.Connect();

                // Set Trainer Data
                SetTrainerData(SAV.SAV);

                // Enable controls
                B_Connect.Enabled = TB_IP.Enabled = TB_Port.Enabled = false;
                groupBox1.Enabled = groupBox2.Enabled = groupBox3.Enabled = true;

                // Load current box
                Remote.ReadBox(SAV.CurrentBox);
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error(ex.Message);
            }
        }

        private void LiveHexUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Remote.Bot.Connected)
                Remote.Bot.Disconnect();
            x.Slots.Publisher.Subscribers.Remove(this);

            Properties.AutoLegality.Default.LatestIP = TB_IP.Text;
            Properties.AutoLegality.Default.Save();
        }

        private void B_ReadCurrent_Click(object sender, EventArgs e) => Remote.ReadBox(SAV.CurrentBox);
        private void B_WriteCurrent_Click(object sender, EventArgs e) => Remote.WriteBox(SAV.CurrentBox);
        private void B_ReadSlot_Click(object sender, EventArgs e) => Remote.ReadActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);
        private void B_WriteSlot_Click(object sender, EventArgs e) => Remote.WriteActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);

        private void B_ReadOffset_Click(object sender, EventArgs e)
        {
            var txt = TB_Offset.Text;
            var offset = Util.GetHexValue(txt);
            if (offset.ToString("X8") != txt.ToUpper().PadLeft(8, '0'))
            {
                WinFormsUtil.Alert("Specified offset is not a valid hex string.");
                return;
            }
            try
            {
                var result = Remote.ReadOffset(offset);
                if (!result)
                    WinFormsUtil.Alert("No valid data is located at the specified offset.");
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
        }

        private void B_ReadRAM_Click(object sender, EventArgs e)
        {
            var txt = RamOffset.Text;
            var offset = Util.GetHexValue(txt);
            var valid = int.TryParse(RamSize.Text, out int size);
            if (offset.ToString("X8") != txt.ToUpper().PadLeft(8, '0') || !valid)
            {
                WinFormsUtil.Alert("Make sure that the RAM offset is a hex string and the size is a valid integer");
                return;
            }

            try
            {
                var result = Remote.ReadRAM(offset, size);
                using (var form = new SimpleHexEditor(result))
                {
                    var res = form.ShowDialog();
                    if (res == DialogResult.OK)
                    {
                        var modifiedRAM = form.Bytes;
                        Remote.WriteRAM(offset, modifiedRAM);
                    }
                }
                Debug.WriteLine("RAM Modified");
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error("Unable to load data from the specified offset.", ex.Message);
            }
        }

        public void NotifySlotOld(ISlotInfo previous) { }

        public void NotifySlotChanged(ISlotInfo slot, SlotTouchType type, PKM pkm)
        {
            if (!checkBox2.Checked || !Remote.Bot.Connected)
                return;
            if (!(slot is SlotInfoBox b))
                return;
            if (!type.IsContentChange())
                return;
            int box = b.Box;
            int slotpkm = b.Slot;
            Remote.Bot.SendSlot(pkm.EncryptedPartyData, box, slotpkm);
        }

        public ISlotInfo GetSlotData(PictureBox view) => null;
        public int GetViewIndex(ISlotInfo slot) => -1;
    }

    internal class HexTextBox : TextBox
    {
        private const int WM_PASTE = 0x0302;

        protected override void WndProc(ref Message m)
        {
            Debug.WriteLine(m.Msg);
            if (m.Msg == WM_PASTE)
            {
                var text = Clipboard.GetText();
                if (text.StartsWith("0x"))
                {
                    text = text.Substring(2);
                    Clipboard.SetText(text);
                }
            }

            base.WndProc(ref m);
        }
    }
}
