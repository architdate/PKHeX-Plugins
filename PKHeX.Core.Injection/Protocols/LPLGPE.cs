using System;
using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public static class LPLGPE
    {
        public static readonly LiveHeXVersion[] SupportedVersions = { LiveHeXVersion.LGPE_v102 };
        public static byte[] ReadBox(PokeSysBotMini psb, int box, int len, List<byte[]> allpkm)
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

        public static byte[] ReadSlot(PokeSysBotMini psb, int box, int slot)
        {
            var bytes = psb.com.ReadBytes(psb.GetSlotOffset(box, slot), psb.SlotSize + psb.GapSize);
            var StoredLength = psb.SlotSize - 0x1C;
            var stored = bytes.Slice(0, StoredLength);
            var party = bytes.Slice(StoredLength + 0x70, 0x1C);
            return ArrayUtil.ConcatAll(stored, party);
        }

        public static void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            var slotofs = psb.GetSlotOffset(box, slot);
            var StoredLength = psb.SlotSize - 0x1C;
            psb.com.WriteBytes(data.Slice(0, StoredLength), slotofs);
            psb.com.WriteBytes(data.AsSpan(StoredLength).ToArray(), slotofs + (ulong) StoredLength + 0x70);
        }

        public static void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(psb.SlotSize);
            for (int i = 0; i < psb.SlotCount; i++)
                SendSlot(psb, pkmData[i], box, i);
        }

        public static Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
        {
            var lv = psb.Version;
            var ofs = RamOffsets.GetTrainerBlockOffset(lv);
            var size = RamOffsets.GetTrainerBlockSize(lv);
            if (size <= 0 || ofs == 0)
                return null;
            var data = psb.com.ReadBytes(ofs, size);
            return data;
        };
    }
}
