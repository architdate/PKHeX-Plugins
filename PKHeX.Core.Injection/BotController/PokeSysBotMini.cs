using System;
using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public class PokeSysBotMini : InjectionBase
    {
        public readonly long BoxStart;
        public readonly int SlotSize;
        public readonly int SlotCount;
        public readonly int GapSize;
        public readonly LiveHeXVersion Version;
        public readonly ICommunicator com;
        public readonly InjectionBase Injector;
        public bool Connected => com.Connected;

        public PokeSysBotMini(LiveHeXVersion lv, ICommunicator communicator, bool useCache)
            : base(lv, useCache)
        {
            Version = lv;
            com = communicator;
            Injector = GetInjector(lv, useCache);
            BoxStart = RamOffsets.GetB1S1Offset(lv);
            SlotSize = RamOffsets.GetSlotSize(lv);
            SlotCount = RamOffsets.GetSlotCount(lv);
            GapSize = RamOffsets.GetGapSize(lv);
        }

        public ulong GetSlotOffset(int box, int slot) =>
            GetBoxOffset(box) + (ulong)((SlotSize + GapSize) * slot);

        public ulong GetBoxOffset(int box) =>
            (ulong)BoxStart + (ulong)((SlotSize + GapSize) * SlotCount * box);

        public byte[] ReadSlot(int box, int slot) => Injector.ReadSlot(this, box, slot);

        public byte[] ReadBox(int box, int len)
        {
            var allpkm = new List<byte[]>();
            return Injector.ReadBox(this, box, len, allpkm);
        }

        public void SendSlot(byte[] data, int box, int slot) =>
            Injector.SendSlot(this, data, box, slot);

        public void SendBox(byte[] boxData, int box)
        {
            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);

            Injector.SendBox(this, boxData, box);
        }

        public byte[] ReadOffset(ulong offset) => com.ReadBytes(offset, SlotSize);
    }
}
