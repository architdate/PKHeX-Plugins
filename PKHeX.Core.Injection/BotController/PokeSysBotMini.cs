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

        private ulong GetBoxOffset(int box) => (ulong)BoxStart + (ulong)((SlotSize + GapSize) * SlotCount * box);
        private ulong GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (ulong)((SlotSize + GapSize) * slot);

        public byte[] ReadBox(int box, int len)
        {
            var allpkm = new List<byte[]>();
            if (!RamOffsets.UseVtable(Version))
            {
                var bytes = com.ReadBytes(GetBoxOffset(box), len);
                if (GapSize == 0)
                    return bytes;
                var currofs = 0;
                if (Version != LiveHeXVersion.LGPE_v102)
                {
                    for (int i = 0; i < SlotCount; i++)
                    {
                        var stored = bytes.Slice(currofs, SlotSize);
                        allpkm.Add(stored);
                        currofs += SlotSize + GapSize;
                    }
                    return ArrayUtil.ConcatAll(allpkm.ToArray());
                }
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
            if (com is not ICommunicatorNX sb)
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, SlotCount * 8);
            var pkmptrs = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray();
            foreach (var p in pkmptrs)
                allpkm.Add(sb.ReadBytesAbsolute(p + 0x20, SlotSize));
            return ArrayUtil.ConcatAll(allpkm.ToArray());
        }

        public byte[] ReadSlot(int box, int slot)
        {
            if (!RamOffsets.UseVtable(Version))
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
            if (com is not ICommunicatorNX sb)
                return new byte[SlotSize];
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, SlotCount * 8);
            var pkmptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[slot] ;
            return sb.ReadBytesAbsolute(pkmptr + 0x20, SlotSize);
        }
        public byte[] ReadOffset(ulong offset) => com.ReadBytes(offset, SlotSize);

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
        }

        public void SendSlot(byte[] data, int box, int slot)
        {
            if (!RamOffsets.UseVtable(Version))
            {
                var slotofs = GetSlotOffset(box, slot);
                if (Version == LiveHeXVersion.LGPE_v102)
                {
                    var StoredLength = SlotSize - 0x1C;
                    com.WriteBytes(data.Slice(0, StoredLength), slotofs);
                    com.WriteBytes(data.SliceEnd(StoredLength), (slotofs + (ulong)StoredLength + 0x70));
                    return;
                }
                com.WriteBytes(data, slotofs);
                return;
            }
            if (com is not ICommunicatorNX sb) return;
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, SlotCount * 8);
            var pkmptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[slot];
            sb.WriteBytesAbsolute(data, pkmptr + 0x20);
        }
    }
}