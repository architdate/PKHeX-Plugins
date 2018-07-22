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

        public RGBLuminanceSource(byte[] d, int W, int H)
            : base(W, H)
        {
            __width = W;
            __height = H;
            int width = W;
            int height = H;
            // In order to measure pure decoding speed, we convert the entire image to a greyscale array
            // up front, which is the same as the Y channel of the YUVLuminanceSource in the real app.
            luminances = new sbyte[width * height];
            for (int y = 0; y < height; y++)
            {
                int offset = y * width;
                for (int x = 0; x < width; x++)
                {
                    int r = d[(offset * 3) + (x * 3)];
                    int g = d[(offset * 3) + (x * 3) + 1];
                    int b = d[(offset * 3) + (x * 3) + 2];
                    if (r == g && g == b)
                    {
                        // Image is already greyscale, so pick any channel.
                        luminances[offset + x] = (sbyte)r;
                    }
                    else
                    {
                        // Calculate luminance cheaply, favoring green.
                        luminances[offset + x] = (sbyte)((r + g + g + b) >> 2);
                    }
                }
            }
        }

        public RGBLuminanceSource(byte[] d, int W, int H, bool Is8Bit)
            : base(W, H)
        {
            __width = W;
            __height = H;
            luminances = new sbyte[W * H];
            Buffer.BlockCopy(d, 0, luminances, 0, W * H);
        }

        public RGBLuminanceSource(byte[] d, int W, int H, bool Is8Bit, Rectangle Region)
            : base(W, H)
        {
            __width = Region.Width;
            __height = Region.Height;
            __Region = Region;
            //luminances = Red.Imaging.Filters.CropArea(d, W, H, Region);
        }

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