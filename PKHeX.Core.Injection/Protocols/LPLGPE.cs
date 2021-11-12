using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core.Injection
{
    public static class LPLGPE
    {
        public static LiveHeXVersion[] SupportedVersions = new LiveHeXVersion[] { LiveHeXVersion.LGPE_v102 };
        public static byte[] ReadBox(this PokeSysBotMini psb, int box, int len, List<byte[]> allpkm)
        {
            var bytes = psb.com.ReadBytes(psb.GetBoxOffset(box), len);
            if (psb.GapSize == 0)
                return bytes;
            var currofs = 0;
            for (int i = 0; i < psb.SlotCount; i++)
            {
                var StoredLength = psb.SlotSize - 0x1C;
                var stored = bytes.Slice(currofs, StoredLength);
                var party = bytes.Slice(currofs + StoredLength + 0x70, 0x1C);
                allpkm.Add(ArrayUtil.ConcatAll(stored, party));
                currofs += psb.SlotSize + psb.GapSize;
            }
            return ArrayUtil.ConcatAll(allpkm.ToArray());
        }

        public static byte[] ReadSlot(this PokeSysBotMini psb, int box, int slot)
        {
            var bytes = psb.com.ReadBytes(psb.GetSlotOffset(box, slot), psb.SlotSize + psb.GapSize);
            var StoredLength = psb.SlotSize - 0x1C;
            var stored = bytes.Slice(0, StoredLength);
            var party = bytes.Slice(StoredLength + 0x70, 0x1C);
            return ArrayUtil.ConcatAll(stored, party);
        }

        public static void SendSlot(this PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            var slotofs = psb.GetSlotOffset(box, slot);
            var StoredLength = psb.SlotSize - 0x1C;
            psb.com.WriteBytes(data.Slice(0, StoredLength), slotofs);
            psb.com.WriteBytes(data.SliceEnd(StoredLength), (slotofs + (ulong) StoredLength + 0x70));
        }

        public static void SendBox(this PokeSysBotMini psb, byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(psb.SlotSize);
            for (int i = 0; i < psb.SlotCount; i++)
                SendSlot(psb, pkmData[i], box, i);
        }
    }
}
