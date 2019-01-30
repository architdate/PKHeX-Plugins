/*
* Copyright 2007 ZXing authors
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
using System;
using com.google.zxing.common;
using com.google.zxing.qrcode.decoder;
using com.google.zxing.qrcode.detector;

namespace com.google.zxing.qrcode
{
    /// <summary>
    /// This implementation can detect and decode QR Codes in an image.
    /// </summary>
    /// <author>  Sean Owen
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class QRCodeReader
    {
        private Decoder Decoder { get; } = new Decoder();

        //UPGRADE_NOTE: Final was removed from the declaration of 'NO_POINTS '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private static readonly ResultPoint[] NO_POINTS = new ResultPoint[0];

        /// <summary> Locates and decodes a QR code in an image.
        ///
        /// </summary>
        /// <param name="image">Image to decode</param>
        /// <returns> a String representing the content encoded by the QR code
        /// </returns>
        /// <throws>  ReaderException if a QR code cannot be found, or cannot be decoded </throws>
        public Result Decode(BinaryBitmap image) => Decode(image, null);

        public Result Decode(BinaryBitmap image, System.Collections.Hashtable hints)
        {
            DecoderResult decoderResult;
            ResultPoint[] points;
            if (hints?.ContainsKey(DecodeHintType.PURE_BARCODE) == true)
            {
                BitMatrix bits = ExtractPureBits(image.BlackMatrix);
                decoderResult = Decoder.Decode(bits);
                points = NO_POINTS;
            }
            else
            {
                DetectorResult detectorResult = new Detector(image.BlackMatrix).Detect(hints);
                decoderResult = Decoder.Decode(detectorResult.Bits);
                points = detectorResult.Points;
            }

            Result result = new Result(decoderResult.Text, decoderResult.RawBytes, points, BarcodeFormat.QR_CODE);
            if (decoderResult.ByteSegments != null)
            {
                result.PutMetadata(ResultMetadataType.BYTE_SEGMENTS, decoderResult.ByteSegments);
            }
            if (decoderResult.ECLevel != null)
            {
                result.PutMetadata(ResultMetadataType.ERROR_CORRECTION_LEVEL, decoderResult.ECLevel.ToString());
            }
            return result;
        }

        /// <summary> This method detects a barcode in a "pure" image -- that is, pure monochrome image
        /// which contains only an unrotated, unskewed, image of a barcode, with some white border
        /// around it. This is a specialized method that works exceptionally fast in this special
        /// case.
        /// </summary>
        /// <param name="image">Image to extract from</param>
        private static BitMatrix ExtractPureBits(BitMatrix image)
        {
            // Now need to determine module size in pixels

            int height = image.Height;
            int width = image.Width;
            int minDimension = Math.Min(height, width);

            // First, skip white border by tracking diagonally from the top left down and to the right:
            int borderWidth = 0;
            while (borderWidth < minDimension && !image.Get_Renamed(borderWidth, borderWidth))
                borderWidth++;

            if (borderWidth == minDimension)
                throw ReaderException.Instance;

            // And then keep tracking across the top-left black module to determine module size
            int moduleEnd = borderWidth;
            while (moduleEnd < minDimension && image.Get_Renamed(moduleEnd, moduleEnd))
                moduleEnd++;

            if (moduleEnd == minDimension)
                throw ReaderException.Instance;

            int moduleSize = moduleEnd - borderWidth;

            // And now find where the rightmost black module on the first row ends
            int rowEndOfSymbol = width - 1;
            while (rowEndOfSymbol >= 0 && !image.Get_Renamed(rowEndOfSymbol, borderWidth))
                rowEndOfSymbol--;

            if (rowEndOfSymbol < 0)
                throw ReaderException.Instance;

            rowEndOfSymbol++;

            // Make sure width of barcode is a multiple of module size
            if ((rowEndOfSymbol - borderWidth) % moduleSize != 0)
                throw ReaderException.Instance;

            int dimension = (rowEndOfSymbol - borderWidth) / moduleSize;

            // Push in the "border" by half the module width so that we start
            // sampling in the middle of the module. Just in case the image is a
            // little off, this will help recover.
            borderWidth += (moduleSize >> 1);

            int sampleDimension = borderWidth + ((dimension - 1) * moduleSize);
            if (sampleDimension >= width || sampleDimension >= height)
                throw ReaderException.Instance;

            // Now just read off the bits
            BitMatrix bits = new BitMatrix(dimension);
            for (int i = 0; i < dimension; i++)
            {
                int iOffset = borderWidth + (i * moduleSize);
                for (int j = 0; j < dimension; j++)
                {
                    if (image.Get_Renamed(borderWidth + (j * moduleSize), iOffset))
                        bits.Set_Renamed(j, i);
                }
            }
            return bits;
        }
    }
}