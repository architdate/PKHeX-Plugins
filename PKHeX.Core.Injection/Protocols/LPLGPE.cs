using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public class LPLGPE(LiveHeXVersion lv, bool useCache) : InjectionBase(lv, useCache)
    {
        private static readonly LiveHeXVersion[] SupportedVersions = [LiveHeXVersion.LGPE_v102];

        public static LiveHeXVersion[] GetVersions() => SupportedVersions;

        public override byte[] ReadBox(PokeSysBotMini psb, int box, int len, List<byte[]> allpkm)
        {
            var bytes = psb.com.ReadBytes(psb.GetBoxOffset(box), len);
            if (psb.GapSize == 0)
                return bytes;
            var currofs = 0;
            for (int i = 0; i < psb.SlotCount; i++)
            {
                var StoredLength = psb.SlotSize - 0x1C;
                var stored = bytes.AsSpan(currofs, psb.SlotSize).ToArray();
                var party = bytes.AsSpan(StoredLength + 0x70, 0x1C).ToArray();
                allpkm.Add([.. stored, .. party]);
                currofs += psb.SlotSize + psb.GapSize;
            }
            return [..allpkm.SelectMany(z => z)];
        }

        public override byte[] ReadSlot(PokeSysBotMini psb, int box, int slot)
        {
            ReadOnlySpan<byte> bytes = psb.com.ReadBytes(psb.GetSlotOffset(box, slot), psb.SlotSize + psb.GapSize);
            var StoredLength = psb.SlotSize - 0x1C;
            var stored = bytes[..StoredLength];
            var party = bytes.Slice(StoredLength + 0x70, 0x1C);
            return [..stored, ..party];
        }

        public override void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            var slotofs = psb.GetSlotOffset(box, slot);
            var StoredLength = psb.SlotSize - 0x1C;
            psb.com.WriteBytes(data[..StoredLength], slotofs);
            psb.com.WriteBytes(data.AsSpan(StoredLength).ToArray(), slotofs + (ulong)StoredLength + 0x70);
        }

        public override void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(psb.SlotSize);
            for (int i = 0; i < psb.SlotCount; i++)
                SendSlot(psb, pkmData[i], box, i);
        }

        public static readonly Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
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
