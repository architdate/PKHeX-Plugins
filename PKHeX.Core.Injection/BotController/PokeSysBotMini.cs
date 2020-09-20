using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public class PokeSysBotMini
    {
        public readonly int BoxStart;
        public readonly int SlotSize;
        public readonly int SlotCount;
        public readonly int GapSize;
        public readonly LiveHeXVersion Version;
        public readonly ICommunicator com;
        public bool Connected => com.Connected;

        public PokeSysBotMini(LiveHeXVersion lv, InjectorCommunicationType ict)
        {
            Version = lv;
            com = RamOffsets.GetCommunicator(lv, ict);
            BoxStart = RamOffsets.GetB1S1Offset(lv);
            SlotSize = RamOffsets.GetSlotSize(lv);
            SlotCount = RamOffsets.GetSlotCount(lv);
            GapSize = RamOffsets.GetGapSize(lv);
        }

        private uint GetBoxOffset(int box) => (uint)BoxStart + (uint)((SlotSize + GapSize) * SlotCount * box);
        private uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)(SlotSize * slot);

        public byte[] ReadBox(int box, int len)
        {
            var bytes = com.ReadBytes(GetBoxOffset(box), len);
            if (GapSize == 0)
                return bytes;
            var allpkm = new List<byte[]>();
            var retval = Array.Empty<byte>();
            var currofs = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                allpkm.Add(bytes.Slice(currofs, SlotSize));
                currofs += SlotSize + GapSize;
            }
            foreach (var x in allpkm)
            {
                retval = retval.Concat(x).ToArray();
            }
            return retval;
        }

        public byte[] ReadSlot(int box, int slot) => com.ReadBytes(GetSlotOffset(box, slot), SlotSize);
        public byte[] ReadOffset(uint offset) => com.ReadBytes(offset, SlotSize);

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
        }

        public void SendSlot(byte[] data, int box, int slot) => com.WriteBytes(data, GetSlotOffset(box, slot));
    }
}