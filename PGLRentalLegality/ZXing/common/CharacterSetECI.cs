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
using System.Collections;

namespace com.google.zxing.common
{
	/// <summary> Encapsulates a Character Set ECI, according to "Extended Channel Interpretations" 5.3.1.1 of ISO 18004.
	/// </summary>
	/// <author>  Sean Owen
	/// </author>
	/// <author>www.Redivivus.in (suraj.supekar@redivivus.in) - Ported from ZXING Java Source
	/// </author>
	public sealed class CharacterSetECI:ECI
	{
		public string EncodingName { get; }

	    private static Hashtable VALUE_TO_ECI;
		private static Hashtable NAME_TO_ECI;

		private static void Initialize()
		{
			VALUE_TO_ECI = Hashtable.Synchronized(new Hashtable(29));
			NAME_TO_ECI = Hashtable.Synchronized(new Hashtable(29));
			// TODO figure out if these values are even right!
			AddCharacterSet(0, "Cp437");
			AddCharacterSet(1, new[]{"ISO8859_1", "ISO-8859-1"});
			AddCharacterSet(2, "Cp437");
			AddCharacterSet(3, new[]{"ISO8859_1", "ISO-8859-1"});
			AddCharacterSet(4, "ISO8859_2");
			AddCharacterSet(5, "ISO8859_3");
			AddCharacterSet(6, "ISO8859_4");
			AddCharacterSet(7, "ISO8859_5");
			AddCharacterSet(8, "ISO8859_6");
			AddCharacterSet(9, "ISO8859_7");
			AddCharacterSet(10, "ISO8859_8");
			AddCharacterSet(11, "ISO8859_9");
			AddCharacterSet(12, "ISO8859_10");
			AddCharacterSet(13, "ISO8859_11");
			AddCharacterSet(15, "ISO8859_13");
			AddCharacterSet(16, "ISO8859_14");
			AddCharacterSet(17, "ISO8859_15");
			AddCharacterSet(18, "ISO8859_16");
			AddCharacterSet(20, new[]{"SJIS", "Shift_JIS"});
		}

		//UPGRADE_NOTE: Final was removed from the declaration of 'encodingName '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"

	    private CharacterSetECI(int value_Renamed, string encodingName):base(value_Renamed)
		{
			EncodingName = encodingName;
		}

		private static void AddCharacterSet(int value, string encodingName)
		{
			CharacterSetECI eci = new CharacterSetECI(value, encodingName);
			VALUE_TO_ECI[value] = eci; // can't use valueOf
			NAME_TO_ECI[encodingName] = eci;
		}

		private static void AddCharacterSet(int value, string[] encodingNames)
		{
			CharacterSetECI eci = new CharacterSetECI(value, encodingNames[0]);
			VALUE_TO_ECI[value] = eci; // can't use valueOf
		    foreach (string s in encodingNames)
		        NAME_TO_ECI[s] = eci;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="value">character set ECI value
		/// </param>
		/// <returns> {@link CharacterSetECI} representing ECI of given value, or null if it is legal but
		/// unsupported
		/// </returns>
		/// <throws>  IllegalArgumentException if ECI value is invalid </throws>
		public static CharacterSetECI GetCharacterSetECIByValue(int value)
		{
		    if (VALUE_TO_ECI == null)
		        Initialize();

		    if (value < 0 || value >= 900)
		        throw new ArgumentException("Bad ECI value: " + value);

		    return (CharacterSetECI) VALUE_TO_ECI[value];
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="name">character set ECI encoding name
		/// </param>
		/// <returns> {@link CharacterSetECI} representing ECI for character encoding, or null if it is legal
		/// but unsupported
		/// </returns>
		public static CharacterSetECI GetCharacterSetECIByName(string name)
		{
		    if (NAME_TO_ECI == null)
		        Initialize();
		    return (CharacterSetECI) NAME_TO_ECI[name];
		}
	}
}