using System.Drawing;

// From: https://github.com/codebude/QRCoder
namespace QRCoder
{
    using System;

    public sealed class QRCode : AbstractQRCode<Bitmap>, IDisposable
    {
        public QRCode(QRCodeData data) : base(data) {}

        public override Bitmap GetGraphic(int pixelsPerModule)
        {
            return GetGraphic(pixelsPerModule, Color.Black, Color.White, true);
        }

        public Bitmap GetGraphic(int pixelsPerModule, Color darkColor, Color lightColor, bool drawQuietZones = true)
        {
            var size = (QrCodeData.ModuleMatrix.Count - (drawQuietZones ? 0 : 8)) * pixelsPerModule;
            var offset = drawQuietZones ? 0 : 4 * pixelsPerModule;

            var bmp = new Bitmap(size, size);
            var gfx = Graphics.FromImage(bmp);
            for (var x = 0; x < size + offset; x += pixelsPerModule)
            {
                for (var y = 0; y < size + offset; y += pixelsPerModule)
                {
                    var module = QrCodeData.ModuleMatrix[((y + pixelsPerModule)/pixelsPerModule) - 1][((x + pixelsPerModule)/pixelsPerModule) - 1];
                    var brush = module ? new SolidBrush(darkColor) : new SolidBrush(lightColor);
                    gfx.FillRectangle(brush, new Rectangle(x - offset, y - offset, pixelsPerModule, pixelsPerModule));
                }
            }

            gfx.Save();
            return bmp;
        }

        public void Dispose()
        {
            QrCodeData = null;
        }
    }
}
