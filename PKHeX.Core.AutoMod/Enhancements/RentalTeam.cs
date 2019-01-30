using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public class RentalTeam
    {
        public readonly List<QRPoke> Team;
        public byte[] GlobalLinkID { get; }
        public byte[] UnknownData { get; }

        public RentalTeam(byte[] data)
        {
            Console.WriteLine(data.Length);
            Team = new List<QRPoke>
            {
                new QRPoke(data.Take(0x30).ToArray()),
                new QRPoke(data.Skip(0x30).Take(0x30).ToArray()),
                new QRPoke(data.Skip(0x60).Take(0x30).ToArray()),
                new QRPoke(data.Skip(0x90).Take(0x30).ToArray()),
                new QRPoke(data.Skip(0xC0).Take(0x30).ToArray()),
                new QRPoke(data.Skip(0xF0).Take(0x30).ToArray())
            };

            foreach (QRPoke p in Team)
                Console.WriteLine(p.ToShowdownFormat(true) + "\n");

            GlobalLinkID = data.Skip(0x120).Take(8).ToArray();
            UnknownData = data.Skip(0x128).ToArray();
        }
    }
}