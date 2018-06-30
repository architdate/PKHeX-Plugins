using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using System.Diagnostics;

using com.google.zxing.qrcode;
using com.google.zxing;
using com.google.zxing.common;
using com.google.zxing.qrcode.decoder;
using com.google.zxing.qrcode.detector;

namespace QRDecoder
{
    class MDecoder
    {
        private Bitmap srcPicture;

        public void setPicture(Bitmap pic)
        {
            this.srcPicture = pic;
        }

        public Bitmap getPicture()
        {
            return this.srcPicture;
        }

        public string MDecode(Bitmap pic)
        {
            this.srcPicture = pic;

            string decode;

            try
            {
                decode = findQrCodeText(new com.google.zxing.qrcode.QRCodeReader(), this.srcPicture);
            }
            catch (Exception exp)
            {
                decode = null;
            }

            return decode;
        }

        [DebuggerHidden]
        public string findQrCodeText(com.google.zxing.Reader decoder, Bitmap bitmap)
        {
            var rgb = new RGBLuminanceSource(bitmap, bitmap.Width, bitmap.Height);
            var hybrid = new com.google.zxing.common.HybridBinarizer(rgb);
            com.google.zxing.BinaryBitmap binBitmap = new com.google.zxing.BinaryBitmap(hybrid);
            string decodedString = decoder.decode(binBitmap, null).Text;
            return decodedString;
        }

        [DebuggerHidden]
        public string Detect(Bitmap bitmap)
        {
            try
            {
                com.google.zxing.LuminanceSource source = new RGBLuminanceSource(bitmap, bitmap.Width, bitmap.Height);
                var binarizer = new HybridBinarizer(source);
                var binBitmap = new BinaryBitmap(binarizer);
                BitMatrix bm = binBitmap.BlackMatrix;
                Detector detector = new Detector(bm);
                DetectorResult result = detector.detect();

                string retStr = "Found at points ";
                foreach (ResultPoint point in result.Points)
                {
                    retStr += point.ToString() + ", ";
                }

                return retStr;
            }
            catch
            {
                return "Failed to detect QR code.";
            }
        }
    }
}
