using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

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
        private uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)((SlotSize + GapSize) * slot);

        public byte[] ReadBox(int box, int len)
        {
            var bytes = com.ReadBytes(GetBoxOffset(box), len);
            if (GapSize == 0)
                return bytes;
            var allpkm = new List<byte[]>();
            var currofs = 0;
            if (Version != LiveHeXVersion.LGPE_v102)
                return bytes;
            for (int i = 0; i < SlotCount; i++)
            {
                var StoredLength = SlotSize - 0x1C;
                var stored = bytes.Slice(currofs, StoredLength);
                var party = bytes.Slice(currofs + StoredLength + 0x70, 0x1C);
                allpkm.Add(ArrayUtil.ConcatAll(stored, party));
                currofs += SlotSize + GapSize;
            }
            return ArrayUtil.ConcatAll(allpkm.ToArray());
        }

        public byte[] ReadSlot(int box, int slot)
        {
            var bytes = com.ReadBytes(GetSlotOffset(box, slot), SlotSize + GapSize);
            if (GapSize == 0)
                return bytes;
            if (Version != LiveHeXVersion.LGPE_v102)
                return bytes;
            var StoredLength = SlotSize - 0x1C;
            var stored = bytes.Slice(0, StoredLength);
            var party = bytes.Slice(StoredLength + 0x70, 0x1C);
            return ArrayUtil.ConcatAll(stored, party);
        }
        public byte[] ReadOffset(uint offset) => com.ReadBytes(offset, SlotSize);

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
        }

        public void SendSlot(byte[] data, int box, int slot)
        {
            var slotofs = GetSlotOffset(box, slot);
            if (Version == LiveHeXVersion.LGPE_v102)
            {
                var StoredLength = SlotSize - 0x1C;
                com.WriteBytes(data.Slice(0, StoredLength), slotofs);
                com.WriteBytes(data.SliceEnd(StoredLength), (uint)(slotofs + StoredLength + 0x70));
                return;
            }
            com.WriteBytes(data, slotofs);
        }
    }
}