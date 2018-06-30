using System.Drawing;
using PKHeX.Core;
using QRCoder;

namespace PKHeX.WinForms
{
    public partial class QR
    {
        private Image finalqr;
        private readonly PKM pkm;
        private Image qr;

        public QR(Image qr, PKM pk)
        {
            pkm = pk;
            this.qr = qr;

            RefreshImage();
        }

        private void RefreshImage()
        {
            Image newpic = new Bitmap(405, 405);
            using (Graphics g = Graphics.FromImage(newpic))
            {
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, newpic.Width, newpic.Height);
                g.DrawImage(qr, 0, 0);
            }
            finalqr = newpic;
        }

        // QR7 Utility
        public static Image GenerateQRCode7(PK7 pk7, int box = 0, int slot = 0, int num_copies = 1)
        {
            byte[] data = QR7.GenerateQRData(pk7, box, slot, num_copies);
            return GenerateQRCode(data, ppm: 4);
        }
        private static Image GenerateQRCode(byte[] data, int ppm = 4)
        {
            using (var generator = new QRCodeGenerator())
            using (var qr_data = generator.CreateQRCode(data))
            using (var qr_code = new QRCode(qr_data))
                return qr_code.GetGraphic(ppm);
        }
    }
}
