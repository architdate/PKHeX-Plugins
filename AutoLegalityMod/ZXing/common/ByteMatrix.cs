/*
* Copyright 2008 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;
namespace com.google.zxing.common
{
    /// <summary> <para>
    /// A class which wraps a 2D array of bytes. The default usage is signed. If you want to use it as a
    /// unsigned container, it's up to you to do byteValue & 0xff at each location.
    /// </para>
    /// <para>
    /// JAVAPORT: The original code was a 2D array of ints, but since it only ever gets assigned
    /// -1, 0, and 1, I'm going to use less memory and go with bytes.
    /// </para>
    ///
    /// </summary>
    /// <author>  dswitkin@google.com (Daniel Switkin)
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class ByteMatrix
    {
        public int Height { get; }

        public int Width { get; }

        public sbyte[][] Array { get; }

        //UPGRADE_NOTE: Final was removed from the declaration of 'bytes '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        //UPGRADE_NOTE: Final was removed from the declaration of 'width '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        //UPGRADE_NOTE: Final was removed from the declaration of 'height '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"

        public ByteMatrix(int width, int height)
        {
            Array = new sbyte[height][];
            for (int i = 0; i < height; i++)
            {
                Array[i] = new sbyte[width];
            }
            Width = width;
            Height = height;
        }

        public sbyte Get_Renamed(int x, int y)
        {
            return Array[y][x];
        }

        public void Set_Renamed(int x, int y, sbyte value_Renamed)
        {
            Array[y][x] = value_Renamed;
        }

        public void Set_Renamed(int x, int y, int value_Renamed)
        {
            Array[y][x] = (sbyte)value_Renamed;
        }

        public void Clear(sbyte value_Renamed)
        {
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    Array[y][x] = value_Renamed;
                }
            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder((2 * Width * Height) + 2);
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    switch (Array[y][x])
                    {
                        case 0:
                            result.Append(" 0");
                            break;

                        case 1:
                            result.Append(" 1");
                            break;

                        default:
                            result.Append("  ");
                            break;
                    }
                }
                result.Append('\n');
            }
            return result.ToString();
        }

        /// <summary>
        /// Converts this ByteMatrix to a black and white bitmap.
        /// </summary>
        /// <returns>A black and white bitmap converted from this ByteMatrix.</returns>
        public Bitmap ToBitmap()
        {
            const byte BLACK = 0;
            const byte WHITE = 255;
            sbyte[][] array = Array;
            int width = Width;
            int height = Height;
            //Here create the Bitmap to the known height, width and format
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            //Create a BitmapData and Lock all pixels to be written
            BitmapData bmpData =
                bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                                         ImageLockMode.WriteOnly, bmp.PixelFormat);

            // If you wanted to support formats other than 8bpp, you should use Bitmap.GetPixelFormatSize(bmp.PixelFormat) to adjust the array size
            byte[] pixels = new byte[bmpData.Stride * height];

            int iPixelsCounter = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[iPixelsCounter++] = array[y][x] == BLACK ? BLACK : WHITE;
                }
                iPixelsCounter += bmpData.Stride - width;
            }

            //Copy the data from the byte array into BitmapData.Scan0
            Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length);

            //Unlock the pixels
            bmp.UnlockBits(bmpData);

            //Return the bitmap
            return bmp;
        }
    }
}
