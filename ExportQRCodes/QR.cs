using System.Drawing;
using PKHeX.Core;
using QRCoder;

namespace PKHeX.WinForms
{
    public static class QR
    {
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
