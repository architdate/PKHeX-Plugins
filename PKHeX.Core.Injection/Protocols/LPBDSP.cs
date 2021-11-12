using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKHeX.Core.Injection
{
    public static class LPBDSP
    {
        public static byte[] ReadBox(this PokeSysBotMini psb, int box, List<byte[]> allpkm)
        {
            if (psb.com is not ICommunicatorNX sb)
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(psb.Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = psb.com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);
            var pkmptrs = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray();
            foreach (var p in pkmptrs)
                allpkm.Add(sb.ReadBytesAbsolute(p + 0x20, psb.SlotSize));
            return ArrayUtil.ConcatAll(allpkm.ToArray());
        }

        public static byte[] ReadSlot(this PokeSysBotMini psb, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb)
                return new byte[psb.SlotSize];
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(psb.Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = psb.com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);
            var pkmptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[slot];
            return sb.ReadBytesAbsolute(pkmptr + 0x20, psb.SlotSize);
        }

        public static void SendSlot(this PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb) 
                return;
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(psb.Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = psb.com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);
            var pkmptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[slot];
            sb.WriteBytesAbsolute(data, pkmptr + 0x20);
        }

        public static void SendBox(this PokeSysBotMini psb, byte[] boxData, int box)
        {
            if (psb.com is not ICommunicatorNX sb)
                return;
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(psb.Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = psb.com.ReadBytes((ulong)addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);
            byte[][] pkmData = boxData.Split(psb.SlotSize);
            var pkmptrs = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray();
            for (int i = 0; i < psb.SlotCount; i++)
                sb.WriteBytesAbsolute(pkmData[i], pkmptrs[i] + 0x20);
        }
    }
}
