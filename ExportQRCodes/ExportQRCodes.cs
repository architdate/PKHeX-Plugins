using System;
using PKHeX.Core;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;
using PKHeX.WinForms;
using System.IO;

namespace ExportQRCodes
{
    public class ExportQRCodes : IPlugin
    {
        public string Name => nameof(ExportQRCodes);
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }

        public void Initialize(params object[] args)
        {
            Console.WriteLine($"Loading {Name}...");
            if (args == null)
                return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            var tools = items.Find("Menu_Tools", false)[0] as ToolStripDropDownItem;
            AddPluginControl(tools);
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(ExportQRs);
        }

        private void ExportQRs(object sender, EventArgs e)
        {
            SaveFile SAV = SaveFileEditor.SAV;
            var boxdata = SAV.BoxData;
            if (boxdata == null)
            {
                MessageBox.Show("Box Data is null");
            }
            int ctr = 0;
            Dictionary<string, Image> qrcodes = new Dictionary<string, Image>();
            foreach (PKM pk in boxdata)
            {
                if (pk.Species == 0 || !pk.Valid || (pk.Box - 1) != SaveFileEditor.CurrentBox)
                    continue;
                ctr++;
                Image qr;
                qr = QR.GenerateQRCode7((PK7)pk);
                if (qr == null) continue;
                
                string[] r = pk.QRText;
                string refer = "PKHeX Auto Legality Mod";
                qrcodes.Add(Util.CleanFileName(pk.FileName), RefreshImage(qr));
            }
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "qrcodes")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "qrcodes"));
            int counter = 0;
            foreach (KeyValuePair<string, Image> qrcode in qrcodes)
            {
                Console.WriteLine(counter);
                counter++;
                qrcode.Value.Save(Path.Combine(Directory.GetCurrentDirectory(), "qrcodes", qrcode.Key + ".png"));
            }
        }

        private Image RefreshImage(Image qr)
        {
            Image newpic = new Bitmap(405, 455);
            using (Graphics g = Graphics.FromImage(newpic))
            {
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, newpic.Width, newpic.Height);
                g.DrawImage(qr, 0, 0);
            }
            return newpic;
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }
    }
}
