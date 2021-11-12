using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public class PokeSysBotMini
    {
        public long BoxStart;
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

        public ulong GetBoxOffset(int box) => (ulong)BoxStart + (ulong)((SlotSize + GapSize) * SlotCount * box);
        public ulong GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (ulong)((SlotSize + GapSize) * slot);

        public byte[] ReadBox(int box, int len)
        {
            var allpkm = new List<byte[]>();
            if (Version == LiveHeXVersion.LGPE_v102) return LPLGPE.ReadBox(this, box, len, allpkm);
            if (Version == LiveHeXVersion.BDSP) return LPBDSP.ReadBox(this, box, allpkm);
            return LPBasic.ReadBox(this, box, len, allpkm);
        }

        public byte[] ReadSlot(int box, int slot)
        {
            if (Version == LiveHeXVersion.LGPE_v102) return LPLGPE.ReadSlot(this, box, slot);
            if (Version == LiveHeXVersion.BDSP) return LPBDSP.ReadSlot(this, box, slot);
            return LPBasic.ReadSlot(this, box, slot);
        }

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
            if (Version == LiveHeXVersion.LGPE_v102)
            {
                LPLGPE.SendBox(this, boxData, box);
                return;
            }
            if (Version == LiveHeXVersion.BDSP)
            {
                LPBDSP.SendBox(this, boxData, box);
                return;
            }
            LPBasic.SendBox(this, boxData, box);
        }

        public void SendSlot(byte[] data, int box, int slot)
        {
            if (Version == LiveHeXVersion.LGPE_v102)
            {
                LPLGPE.SendSlot(this, data, box, slot);
                return;
            }
            if (Version == LiveHeXVersion.BDSP)
            {
                LPBDSP.SendSlot(this, data, box, slot);
                return;
            }
            LPBasic.SendSlot(this, data, box, slot);
        }

        public byte[] ReadOffset(ulong offset) => com.ReadBytes(offset, SlotSize);
    }
}