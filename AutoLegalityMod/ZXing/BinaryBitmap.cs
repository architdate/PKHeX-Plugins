/*
* Copyright 2009 ZXing authors
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

namespace com.google.zxing
{
    /// <summary> This class is the core bitmap class used by ZXing to represent 1 bit data. Reader objects
    /// accept a BinaryBitmap and attempt to decode it.
    ///
    /// </summary>
    /// <author>  dswitkin@google.com (Daniel Switkin)
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>

    public sealed class BinaryBitmap
    {
        /// <summary> Converts a 2D array of luminance data to 1 bit. As above, assume this method is expensive
        /// and do not call it repeatedly. This method is intended for decoding 2D barcodes and may or
        /// may not apply sharpening. Therefore, a row from this matrix may not be identical to one
        /// fetched using getBlackRow(), so don't mix and match between them.
        ///
        /// </summary>
        /// <returns> The 2D array of bits for the image (true means black).
        /// </returns>
        public BitMatrix BlackMatrix
        {
            get
            {
                // The matrix is created on demand the first time it is requested, then cached. There are two
                // reasons for this:
                // 1. This work will never be done if the caller only installs 1D Reader objects, or if a
                //    1D Reader finds a barcode before the 2D Readers run.
                // 2. This work will only be done once even if the caller installs multiple 2D Readers.
                if (matrix != null)
                    return matrix;
                matrix = binarizer.BlackMatrix;
                return matrix;
            }
        }

        //UPGRADE_NOTE: Final was removed from the declaration of 'binarizer '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private readonly Binarizer binarizer;
        private BitMatrix matrix;

        public BinaryBitmap(Binarizer binarizer)
        {
            this.binarizer = binarizer ?? throw new ArgumentException("Binarizer must be non-null.");
            matrix = null;
        }
    }
}