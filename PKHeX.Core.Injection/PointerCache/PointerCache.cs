using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public abstract class PointerCache(LiveHeXVersion version, bool useCache = false)
    {
        private readonly Dictionary<LiveHeXVersion, Dictionary<string, ulong>> Cache = [];

        public ulong GetCachedPointer(ICommunicatorNX com, string ptr, bool relative = true)
        {
            ulong pointer = 0;
            bool hasEntry = Cache.TryGetValue(version, out var cache);
            bool hasPointer = cache is not null && cache.TryGetValue(ptr, out pointer);
            if (hasPointer && useCache)
                return pointer;

            pointer = com.GetPointerAddress(ptr, relative);
            if (!useCache)
                return pointer;

            if (!hasEntry)
                Cache.Add(version, new() { { ptr, pointer } });
            else
                Cache[version].Add(ptr, pointer);

            return pointer;
        }
    }
}
