using System;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public static class InjectionUtil
    {
        public static ulong GetPointerAddress(this ICommunicatorNX sb, string ptr, bool heaprealtive=true)
        {
            while (ptr.Contains("]]"))
                ptr = ptr.Replace("]]", "]+0]");
            uint? finadd = null;
            if (!ptr.EndsWith("]"))
            {
                finadd = Util.GetHexValue(ptr.Split('+').Last());
                ptr = ptr.Substring(0, ptr.LastIndexOf('+'));
            }
            var jumps = ptr.Replace("main", "").Replace("[", "").Replace("]", "").Split(new[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
            if (jumps.Length == 0) 
                return 0;

            var initaddress = Util.GetHexValue(jumps[0].Trim());
            ulong address = BitConverter.ToUInt64(sb.ReadBytesMain(initaddress, 0x8), 0);
            foreach (var j in jumps)
            {
                var val = Util.GetHexValue(j.Trim());
                if (val == initaddress)
                    continue;
                address = BitConverter.ToUInt64(sb.ReadBytesAbsolute(address + val, 0x8), 0);
            }
            if (finadd != null) address += (ulong)finadd;
            if (heaprealtive)
            {
                ulong heap = sb.GetHeapBase();
                address -= heap;
            }
            return address;
        }
    }
}
