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
using System.Collections;

namespace com.google.zxing
{
	/// <summary> Enumerates barcode formats known to this package.
	///
	/// </summary>
	/// <author>  Sean Owen
	/// </author>
	/// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
	/// </author>

	public sealed class BarcodeFormat
	{
		public string Name { get; }

	    // No, we can't use an enum here. J2ME doesn't support it.

		//UPGRADE_NOTE: Final was removed from the declaration of 'VALUES '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
		private static readonly Hashtable VALUES = Hashtable.Synchronized(new Hashtable());

		/// <summary>QR Code 2D barcode format. </summary>
		//UPGRADE_NOTE: Final was removed from the declaration of 'QR_CODE '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
		public static readonly BarcodeFormat QR_CODE = new BarcodeFormat("QR_CODE");

	    private BarcodeFormat(string name)
		{
			Name = name;
			VALUES[name] = this;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}