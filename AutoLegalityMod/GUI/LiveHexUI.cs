using System;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public partial class LiveHexUI : Form
    {
        public ISaveFileProvider SAV { get; }
        private readonly LiveHexController Remote;
        private readonly ComboBox BoxSelect;

        public LiveHexUI(ISaveFileProvider sav, IPKMView editor)
        {
            SAV = sav;
            Remote = new LiveHexController(sav, editor);

            InitializeComponent();

            // add an event to the editor
            // ReSharper disable once SuspiciousTypeConversion.Global
            BoxSelect = (ComboBox)((Control)sav).Controls["CB_BoxSelect"];
            if (BoxSelect != null)
                BoxSelect.SelectedIndexChanged += ChangeBox;

            TB_IP.Text = Remote.Bot.IP;
            TB_Port.Text = Remote.Bot.Port.ToString();
        }

        private void ChangeBox(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
                Remote.ChangeBox(BoxSelect.SelectedIndex);
        }

        private void B_Connect_Click(object sender, EventArgs e)
        {
            try
            {
                Remote.Bot.IP = TB_IP.Text;
                Remote.Bot.Port = int.Parse(TB_Port.Text);
                Remote.Bot.Connect();
                B_Connect.Enabled = TB_IP.Enabled = TB_Port.Enabled = false;
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
    }
}
