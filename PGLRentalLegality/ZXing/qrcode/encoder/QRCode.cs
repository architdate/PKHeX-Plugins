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
using System;
using com.google.zxing.common;
using com.google.zxing.qrcode.decoder;

namespace com.google.zxing.qrcode.encoder
{
    /// <summary>
    ///
    /// </summary>
    /// <author>  satorux@google.com (Satoru Takabayashi) - creator
    /// </author>
    /// <author>  dswitkin@google.com (Daniel Switkin) - ported from C++
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>
    public sealed class QRCode
    {
        /// <summary>
        /// Mode of the QR Code.
        /// </summary>
        public Mode Mode { get; set; }

        /// <summary>
        /// Error correction level of the QR Code.
        /// </summary>
        public ErrorCorrectionLevel ECLevel { get; set; }

        /// <summary>
        /// Version of the QR Code.  The bigger size, the bigger version.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// ByteMatrix width of the QR Code.
        /// </summary>
        public int MatrixWidth { get; set; }

        /// <summary>
        /// Mask pattern of the QR Code.
        /// </summary>
        public int MaskPattern { get; set; }

        /// <summary>
        /// Number of total bytes in the QR Code.
        /// </summary>
        public int NumTotalBytes { get; set; }

        /// <summary>
        /// Number of data bytes in the QR Code.
        /// </summary>
        public int NumDataBytes { get; set; }

        /// <summary>
        /// Number of error correction bytes in the QR Code.
        /// </summary>
        public int NumECBytes { get; set; }

        /// <summary>
        /// Number of Reedsolomon blocks in the QR Code.
        /// </summary>
        public int NumRSBlocks { get; set; }

        /// <summary>
        /// ByteMatrix data of the QR Code. This takes ownership of the 2D array.
        /// </summary>
        public ByteMatrix Matrix { get; set; }

        /// <summary>
        /// Checks all the member variables are set properly. Returns true on success. Otherwise, returns false.
        /// </summary>
        public bool Valid => Mode != null && ECLevel != null && Version != - 1 && MatrixWidth != - 1 && MaskPattern != - 1 && NumTotalBytes != - 1 && NumDataBytes != - 1 && NumECBytes != - 1 && NumRSBlocks != - 1 && IsValidMaskPattern(MaskPattern) && NumTotalBytes == NumDataBytes + NumECBytes && Matrix != null && MatrixWidth == Matrix.Width && Matrix.Width == Matrix.Height;

        public const int NUM_MASK_PATTERNS = 8;

        public QRCode()
        {
            Mode = null;
            ECLevel = null;
            Version = - 1;
            MatrixWidth = - 1;
            MaskPattern = - 1;
            NumTotalBytes = - 1;
            NumDataBytes = - 1;
            NumECBytes = - 1;
            NumRSBlocks = - 1;
            Matrix = null;
        }

        // Return the value of the module (cell) pointed by "x" and "y" in the matrix of the QR Code. They
        // call cells in the matrix "modules". 1 represents a black cell, and 0 represents a white cell.
        public int At(int x, int y)
        {
            // The value must be zero or one.
            int value = Matrix.Get_Renamed(x, y);
            if (!(value == 0 || value == 1))
            {
                // this is really like an assert... not sure what better exception to use?
                throw new SystemException("Bad value");
            }
            return value;
        }

        // Return debug String.
        public override string ToString()
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder(200);
            result.Append("<<\n");
            result.Append(" mode: ");
            result.Append(Mode);
            result.Append("\n ecLevel: ");
            result.Append(ECLevel);
            result.Append("\n version: ");
            result.Append(Version);
            result.Append("\n matrixWidth: ");
            result.Append(MatrixWidth);
            result.Append("\n maskPattern: ");
            result.Append(MaskPattern);
            result.Append("\n numTotalBytes: ");
            result.Append(NumTotalBytes);
            result.Append("\n numDataBytes: ");
            result.Append(NumDataBytes);
            result.Append("\n numECBytes: ");
            result.Append(NumECBytes);
            result.Append("\n numRSBlocks: ");
            result.Append(NumRSBlocks);
            if (Matrix == null)
            {
                result.Append("\n matrix: null\n");
            }
            else
            {
                result.Append("\n matrix:\n");
                result.Append(Matrix);
            }
            result.Append(">>\n");
            return result.ToString();
        }

        // Check if "mask_pattern" is valid.
        public static bool IsValidMaskPattern(int maskPattern)
        {
            return maskPattern >= 0 && maskPattern < NUM_MASK_PATTERNS;
        }
    }
}