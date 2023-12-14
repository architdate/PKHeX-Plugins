using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.Enhancements
{
    /// <summary>
    /// PGL Website QR Rental Team
    /// </summary>
    public class RentalTeam
    {
        public readonly IReadOnlyList<QRPK7> Team;
        public IReadOnlyList<byte> GlobalLinkID { get; }
        public IReadOnlyList<byte> UnknownData { get; }
        private static readonly SaveFile Dummy = new SAV7USUM();

        public RentalTeam(byte[] data)
        {
            Debug.WriteLine(data.Length);
            Team = new[]
            {
                new QRPK7(data[..0x30]),
                new QRPK7(data[0x30..0x60]),
                new QRPK7(data[0x60..0x90]),
                new QRPK7(data[0x90..0xC0]),
                new QRPK7(data[0xC0..0xF0]),
                new QRPK7(data[0xF0..0x120]),
            };

            Debug.WriteLine(string.Join(Environment.NewLine, ConvertedTeam.Select(z => z.Text)));

            GlobalLinkID = data[0x120..0x128];
            UnknownData = data[0x128..];
        }

        public IEnumerable<ShowdownSet> ConvertedTeam =>
            Team.Select(z => z.ConvertToPKM(Dummy)).Select(z => new ShowdownSet(z));
    }
}
