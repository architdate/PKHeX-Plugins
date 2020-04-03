﻿using System.Linq;
using System.Windows.Forms;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins.GUI
{
    public partial class SimpleHexEditor : Form
    {
        public byte[] Bytes { get; private set; }

        public SimpleHexEditor(byte[] originalBytes)
        {
            InitializeComponent();
            WinFormsTranslator.TranslateInterface(this, WinFormsTranslator.CurrentLanguage);
            RTB_RAM.Text = string.Join(" ", originalBytes.Select(z => $"{z:X2}"));
            Bytes = originalBytes;
        }

        private void Update_Click(object sender, System.EventArgs e)
        {
            var bytestring = RTB_RAM.Text.Replace("\t", "").Replace(" ", "").Trim();
            Bytes = Decoder.StringToByteArray(bytestring);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
