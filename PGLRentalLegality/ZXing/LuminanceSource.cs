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
namespace com.google.zxing
{
    /// <summary> The purpose of this class hierarchy is to abstract different bitmap implementations across
    /// platforms into a standard interface for requesting greyscale luminance values. The interface
    /// only provides immutable methods; therefore crop and rotation create copies. This is to ensure
    /// that one Reader does not modify the original luminance source and leave it in an unknown state
    /// for other Readers in the chain.
    ///
    /// </summary>
    /// <author>  dswitkin@google.com (Daniel Switkin)
    /// </author>
    /// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
    /// </author>

    public abstract class LuminanceSource
    {
        /// <summary> Fetches luminance data for the underlying bitmap. Values should be fetched using:
        /// int luminance = array[y * width + x] & 0xff;
        ///
        /// </summary>
        /// <returns> A row-major 2D array of luminance values. Do not use result.length as it may be
        /// larger than width * height bytes on some platforms. Do not modify the contents
        /// of the result.
        /// </returns>
        public abstract sbyte[] Matrix{get;}

        /// <summary> The width of the bitmap.
        /// </summary>
        public virtual int Width { get; }

        /// <summary> The height of the bitmap.
        /// </summary>
        public virtual int Height { get; }

        //UPGRADE_NOTE: Final was removed from the declaration of 'width '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        //UPGRADE_NOTE: Final was removed from the declaration of 'height '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"

        protected LuminanceSource(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary> Fetches one row of luminance data from the underlying platform's bitmap. Values range from
        /// 0 (black) to 255 (white). Because Java does not have an unsigned byte type, callers will have
        /// to bitwise and with 0xff for each value. It is preferable for implementations of this method
        /// to only fetch this row rather than the whole image, since no 2D Readers may be installed and
        /// getMatrix() may never be called.
        ///
        /// </summary>
        /// <param name="y">The row to fetch, 0 <= y < Height.
        /// </param>
        /// <param name="row">An optional preallocated array. If null or too small, it will be ignored.
        /// Always use the returned object, and ignore the .length of the array.
        /// </param>
        /// <returns> An array containing the luminance data.
        /// </returns>
        public abstract sbyte[] GetRow(int y, sbyte[] row);
    }
}