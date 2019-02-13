using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// PGL Website QR Rental Team
    /// </summary>
    public class RentalTeam
    {
        public readonly IReadOnlyList<QRPK7> Team;
        public byte[] GlobalLinkID { get; }
        public byte[] UnknownData { get; }

        public RentalTeam(byte[] data)
        {
            Debug.WriteLine(data.Length);
            Team = new[]
            {
                new QRPK7(data.Take(0x30).ToArray()),
                new QRPK7(data.Skip(0x30).Take(0x30).ToArray()),
                new QRPK7(data.Skip(0x60).Take(0x30).ToArray()),
                new QRPK7(data.Skip(0x90).Take(0x30).ToArray()),
                new QRPK7(data.Skip(0xC0).Take(0x30).ToArray()),
                new QRPK7(data.Skip(0xF0).Take(0x30).ToArray())
            };

            Debug.WriteLine(string.Join(Environment.NewLine, ConvertedTeam.Select(z => z.Text)));

            GlobalLinkID = data.Skip(0x120).Take(8).ToArray();
            UnknownData = data.Skip(0x128).ToArray();
        }

        public IEnumerable<ShowdownSet> ConvertedTeam => Team.Select(z => z.ConvertToPKM()).Select(z => new ShowdownSet(z));
    }
}