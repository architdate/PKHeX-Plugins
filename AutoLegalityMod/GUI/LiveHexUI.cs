using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
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
        private readonly ComboBox BoxSelect;

        public LiveHexUI(ISaveFileProvider sav, IPKMView editor)
        {
            SAV = sav;
            Remote = new LiveHexController(sav, editor);

            InitializeComponent();

            // add an event to the editor
            // ReSharper disable once SuspiciousTypeConversion.Global
            BoxSelect = ((Control)sav).Controls.Find("CB_BoxSelect", true).FirstOrDefault() as ComboBox;
            if (BoxSelect != null)
                BoxSelect.SelectedIndexChanged += ChangeBox;

            var type = sav.GetType();
            var fields = type.GetTypeInfo().DeclaredFields;
            var test = fields.First(z => z.Name == "EditEnv");
            var x = (SaveDataEditor<PictureBox>) test.GetValue(sav);
            x.Slots.Publisher.Subscribers.Add(this);

            TB_IP.Text = Remote.Bot.IP;
            TB_Port.Text = Remote.Bot.Port.ToString();
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
                Remote.ChangeBox(BoxSelect.SelectedIndex);
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
                checkBox1.Enabled = checkBox2.Enabled = true;
                groupBox1.Enabled = groupBox2.Enabled = true;
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
            if (BoxSelect != null)
                BoxSelect.SelectedIndexChanged -= ChangeBox;
        }

        private void B_ReadCurrent_Click(object sender, EventArgs e) => Remote.ReadBox(SAV.CurrentBox);
        private void B_WriteCurrent_Click(object sender, EventArgs e) => Remote.WriteBox(SAV.CurrentBox);
        private void B_ReadSlot_Click(object sender, EventArgs e) => Remote.ReadActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);
        private void B_WriteSlot_Click(object sender, EventArgs e) => Remote.WriteActiveSlot((int)NUD_Box.Value - 1, (int)NUD_Slot.Value - 1);

        public void NotifySlotOld(ISlotInfo previous) { }
        public void NotifySlotChanged(ISlotInfo slot, SlotTouchType type, PKM pkm)
        {
            if (!checkBox2.Checked || !Remote.Bot.Connected)
                return;
            if (!(slot is SlotInfoBox b))
                return;
            int box = b.Box;
            int slotpkm = b.Slot;
            Remote.Bot.SendSlot(pkm.EncryptedPartyData, box, slotpkm);
        }

        public ISlotInfo GetSlotData(PictureBox view) => null;
        public int GetViewIndex(ISlotInfo slot) => -1;

    }
}
