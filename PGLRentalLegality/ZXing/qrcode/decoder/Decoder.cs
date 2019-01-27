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

using com.google.zxing.common;
using com.google.zxing.common.reedsolomon;

namespace com.google.zxing.qrcode.decoder
{
	/// <summary> <p>The main class which implements QR Code decoding -- as opposed to locating and extracting
	/// the QR Code from an image.</p>
	///
	/// </summary>
	/// <author>  Sean Owen
	/// </author>
	/// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
	/// </author>
	public sealed class Decoder
	{
		//UPGRADE_NOTE: Final was removed from the declaration of 'rsDecoder '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
		private readonly ReedSolomonDecoder rsDecoder;

		public Decoder()
		{
			rsDecoder = new ReedSolomonDecoder(GF256.QR_CODE_FIELD);
		}

		/// <summary> <p>Convenience method that can decode a QR Code represented as a 2D array of booleans.
		/// "true" is taken to mean a black module.</p>
		///
		/// </summary>
		/// <param name="image">booleans representing white/black QR Code modules
		/// </param>
		/// <returns> text and bytes encoded within the QR Code
		/// </returns>
		/// <throws>  ReaderException if the QR Code cannot be decoded </throws>
		public DecoderResult Decode(bool[][] image)
		{
			int dimension = image.Length;
			BitMatrix bits = new BitMatrix(dimension);
			for (int i = 0; i < dimension; i++)
			{
				for (int j = 0; j < dimension; j++)
				{
					if (image[i][j])
					{
						bits.Set_Renamed(j, i);
					}
				}
			}
			return Decode(bits);
		}

		/// <summary> <p>Decodes a QR Code represented as a {@link BitMatrix}. A 1 or "true" is taken to mean a black module.</p>
		///
		/// </summary>
		/// <param name="bits">booleans representing white/black QR Code modules
		/// </param>
		/// <returns> text and bytes encoded within the QR Code
		/// </returns>
		/// <throws>  ReaderException if the QR Code cannot be decoded </throws>
		public DecoderResult Decode(BitMatrix bits)
		{
			// Construct a parser and read version, error-correction level
			BitMatrixParser parser = new BitMatrixParser(bits);
			Version version = parser.ReadVersion();
			ErrorCorrectionLevel ecLevel = parser.ReadFormatInformation().ErrorCorrectionLevel;

			// Read codewords
			sbyte[] codewords = parser.ReadCodewords();
			// Separate into data blocks
			DataBlock[] dataBlocks = DataBlock.GetDataBlocks(codewords, version, ecLevel);

			// Count total number of data bytes
			int totalBytes = 0;
			foreach (DataBlock t in dataBlocks)
			{
			    totalBytes += t.NumDataCodewords;
			}
			sbyte[] resultBytes = new sbyte[totalBytes];
			int resultOffset = 0;

			// Error-correct and copy data blocks together into a stream of bytes
			foreach (DataBlock dataBlock in dataBlocks)
			{
			    sbyte[] codewordBytes = dataBlock.Codewords;
			    int numDataCodewords = dataBlock.NumDataCodewords;
			    CorrectErrors(codewordBytes, numDataCodewords);
			    for (int i = 0; i < numDataCodewords; i++)
			    {
			        resultBytes[resultOffset++] = codewordBytes[i];
			    }
			}

			// Decode the contents of that stream of bytes
			return DecodedBitStreamParser.Decode(resultBytes, version, ecLevel);
		}

		/// <summary> <p>Given data and error-correction codewords received, possibly corrupted by errors, attempts to
		/// correct the errors in-place using Reed-Solomon error correction.</p>
		///
		/// </summary>
		/// <param name="codewordBytes">data and error correction codewords
		/// </param>
		/// <param name="numDataCodewords">number of codewords that are data bytes
		/// </param>
		/// <throws>  ReaderException if error correction fails </throws>
		private void CorrectErrors(sbyte[] codewordBytes, int numDataCodewords)
		{
			int numCodewords = codewordBytes.Length;
			// First read into an array of ints
			int[] codewordsInts = new int[numCodewords];
			for (int i = 0; i < numCodewords; i++)
			{
				codewordsInts[i] = codewordBytes[i] & 0xFF;
			}
			int numECCodewords = codewordBytes.Length - numDataCodewords;
			try
			{
				rsDecoder.Decode(codewordsInts, numECCodewords);
			}
			catch (ReedSolomonException)
			{
				throw ReaderException.Instance;
			}
			// Copy back into array of bytes -- only need to worry about the bytes that were data
			// We don't care about errors in the error-correction codewords
			for (int i = 0; i < numDataCodewords; i++)
			{
				codewordBytes[i] = (sbyte) codewordsInts[i];
			}
		}
	}
}
