﻿using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PKHeX.WinForms
{
    public static class ImageUtil
    {
        // Image Layering/Blending Utility
        public static Bitmap LayerImage(Image baseLayer, Image overLayer, int x, int y, double trans)
        {
            if (baseLayer == null)
                return overLayer as Bitmap;
            Bitmap img = new Bitmap(baseLayer.Width, baseLayer.Height);
            using (Graphics gr = Graphics.FromImage(img))
            {
                gr.DrawImage(baseLayer, new Rectangle(0, 0, baseLayer.Width, baseLayer.Height));
                Image o = trans == 1f ? overLayer : ChangeOpacity(overLayer, trans);
                gr.DrawImage(o, new Rectangle(x, y, overLayer.Width, overLayer.Height));
            }
            return img;
        }
        public static Bitmap ChangeOpacity(Image img, double trans)
        {
            if (img == null)
                return null;
            if (img.PixelFormat.HasFlag(PixelFormat.Indexed))
                return (Bitmap)img;

            var bmp = (Bitmap)img.Clone();
            GetBitmapData(bmp, out BitmapData bmpData, out IntPtr ptr, out byte[] data);

            Marshal.Copy(ptr, data, 0, data.Length);
            SetAllTransparencyTo(data, trans);
            Marshal.Copy(data, 0, ptr, data.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
        public static Bitmap ChangeAllColorTo(Image img, Color c)
        {
            if (img == null)
                return null;
            if (img.PixelFormat.HasFlag(PixelFormat.Indexed))
                return (Bitmap)img;

            var bmp = (Bitmap)img.Clone();
            GetBitmapData(bmp, out BitmapData bmpData, out IntPtr ptr, out byte[] data);

            Marshal.Copy(ptr, data, 0, data.Length);
            SetAllColorTo(data, c);
            Marshal.Copy(data, 0, ptr, data.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
        public static Bitmap ToGrayscale(Image img)
        {
            if (img == null)
                return null;
            if (img.PixelFormat.HasFlag(PixelFormat.Indexed))
                return (Bitmap)img;

            var bmp = (Bitmap)img.Clone();
            GetBitmapData(bmp, out BitmapData bmpData, out IntPtr ptr, out byte[] data);

            Marshal.Copy(ptr, data, 0, data.Length);
            SetAllColorToGrayScale(data);
            Marshal.Copy(data, 0, ptr, data.Length);
            bmp.UnlockBits(bmpData);

            return bmp;
        }
        private static void GetBitmapData(Bitmap bmp, out BitmapData bmpData, out IntPtr ptr, out byte[] data)
        {
            bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            ptr = bmpData.Scan0;
            data = new byte[bmp.Width * bmp.Height * 4];
        }
        public static Bitmap GetBitmap(byte[] data, int width, int height, int stride = -1, PixelFormat format = PixelFormat.Format32bppArgb)
        {
            if (stride == -1 && format == PixelFormat.Format32bppArgb)
                stride = 4 * width; // defaults
            return new Bitmap(width, height, stride, format, Marshal.UnsafeAddrOfPinnedArrayElement(data, 0));
        }
        public static byte[] GetPixelData(Bitmap bitmap)
        {
            var argbData = new byte[bitmap.Width * bitmap.Height * 4];
            var bd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            Marshal.Copy(bd.Scan0, argbData, 0, bitmap.Width * bitmap.Height * 4);
            bitmap.UnlockBits(bd);
            return argbData;
        }
        private static void SetAllTransparencyTo(byte[] data, double trans)
        {
            for (int i = 0; i < data.Length; i += 4)
                data[i + 3] = (byte)(data[i + 3] * trans);
        }
        private static void SetAllColorTo(byte[] data, Color c)
        {
            byte R = c.R;
            byte G = c.G;
            byte B = c.B;
            for (int i = 0; i < data.Length; i += 4)
            {
                if (data[i + 3] == 0)
                    continue;
                data[i + 0] = B;
                data[i + 1] = G;
                data[i + 2] = R;
            }
        }
        private static void SetAllColorToGrayScale(byte[] data)
        {
            for (int i = 0; i < data.Length; i += 4)
            {
                if (data[i + 3] == 0)
                    continue;
                byte greyS = (byte)((0.3 * data[i + 2] + 0.59 * data[i + 1] + 0.11 * data[i + 0]) / 3);
                data[i + 0] = greyS;
                data[i + 1] = greyS;
                data[i + 2] = greyS;
            }
        }
    }
}
