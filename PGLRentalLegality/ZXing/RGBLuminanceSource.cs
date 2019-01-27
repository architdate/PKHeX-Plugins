using System.Drawing;
using System;

namespace com.google.zxing.common
{
    public class RGBLuminanceSource : LuminanceSource
    {
        private readonly sbyte[] luminances;
        private bool isRotated;
        private Rectangle __Region;

        public override int Height => !isRotated ? __height : __width;
        public override int Width => !isRotated ? __width : __height;

        private readonly int __height;
        private readonly int __width;

        public RGBLuminanceSource(Bitmap d, int W, int H)
            : base(W, H)
        {
            int width = __width = W;
            int height = __height = H;
            // In order to measure pure decoding speed, we convert the entire image to a greyscale array
            // up front, which is the same as the Y channel of the YUVLuminanceSource in the real app.
            luminances = new sbyte[width * height];
            //if (format == PixelFormat.Format8bppIndexed)
            {
                Color c;
                for (int y = 0; y < height; y++)
                {
                    int offset = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        c = d.GetPixel(x, y);
                        luminances[offset + x] = (sbyte)(((int)c.R) << 16 | ((int)c.G) << 8 | ((int)c.B));
                    }
                }
            }
        }

        public override sbyte[] getRow(int y, sbyte[] row)
        {
            if (!isRotated)
            {
                int width = Width;
                if (row == null || row.Length < width)
                {
                    row = new sbyte[width];
                }
                for (int i = 0; i < width; i++)
                    row[i] = luminances[(y * width) + i];
                //System.arraycopy(luminances, y * width, row, 0, width);
                return row;
            }
            else
            {
                int width = __width;
                int height = __height;
                if (row == null || row.Length < height)
                {
                    row = new sbyte[height];
                }
                for (int i = 0; i < height; i++)
                    row[i] = luminances[(i * width) + y];
                //System.arraycopy(luminances, y * width, row, 0, width);
                return row;
            }
        }

        public override sbyte[] Matrix => luminances;

        public override LuminanceSource rotateCounterClockwise()
        {
            isRotated = true;
            return this;
        }

        public override bool RotateSupported => true;
    }
}