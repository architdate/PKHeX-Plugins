using System;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public static class InjectionUtil
    {
        public const ulong INVALID_PTR = 0;
        public static ulong GetPointerAddress(this ICommunicatorNX sb, string ptr, bool heaprealtive=true)
        {
            if (string.IsNullOrWhiteSpace(ptr) || ptr.IndexOfAny(new[] { '-', '/', '*' }) != -1)
                return INVALID_PTR;
            while (ptr.Contains("]]"))
                ptr = ptr.Replace("]]", "]+0]");
            uint finadd = 0;
            if (!ptr.EndsWith("]"))
            {
                finadd = Util.GetHexValue(ptr.Split('+').Last());
                ptr = ptr[..ptr.LastIndexOf('+')];
            }
            var jumps = ptr.Replace("main", "").Replace("[", "").Replace("]", "").Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
            if (jumps.Length == 0)
                return INVALID_PTR;

            var initaddress = Util.GetHexValue(jumps[0].Trim());
            ulong address = BitConverter.ToUInt64(sb.ReadBytesMain(initaddress, 0x8), 0);
            foreach (var j in jumps)
            {
                var val = Util.GetHexValue(j.Trim());
                if (val == initaddress)
                    continue;
                address = BitConverter.ToUInt64(sb.ReadBytesAbsolute(address + val, 0x8), 0);
            }
            address += finadd;
            if (heaprealtive)
            {
                ulong heap = sb.GetHeapBase();
                address -= heap;
            }
            return address;
        }

        public static string ExtendPointer(this string pointer, params uint[] jumps)
        {
            foreach (var jump in jumps)
                pointer = $"[{pointer}]+{jump:X}";
            return pointer;
        }

        public static ulong SearchSaveKey(this ICommunicatorNX sb, string saveblocks, uint key)
        {
            var ptr = sb.GetPointerAddress(saveblocks, heaprealtive:false);
            var dt = sb.ReadBytesAbsolute(ptr + 8, 16);
            var start = BitConverter.ToUInt64(dt.AsSpan()[..8]);
            var end = BitConverter.ToUInt64(dt.AsSpan()[8..]);

            while (start < end)
            {
                var block_ct = (end - start) / 32;
                var mid = start + (block_ct >> 1) * 32;
                var found = BitConverter.ToUInt32(sb.ReadBytesAbsolute(mid, 4));
                if (found == key)
                    return mid;
                if (found >= key)
                    end = mid;
                else
                    start = mid + 32;
            }
            return start;
        }
    }
}
