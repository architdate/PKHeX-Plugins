﻿using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public abstract class PointerCache
    {
        private readonly LiveHeXVersion Version;
        private readonly Dictionary<LiveHeXVersion, Dictionary<string, ulong>> Cache;

        public PointerCache(LiveHeXVersion version)
        {
            Version = version;
            Cache = new();
        }

        public ulong GetCachedPointer(ICommunicatorNX com, string ptr, bool relative = true)
        {
            ulong pointer = 0;
            bool hasEntry = Cache.TryGetValue(Version, out var cache);
            bool hasPointer = cache is not null && cache.TryGetValue(ptr, out pointer);
            if (hasPointer)
                return pointer;

            pointer = com.GetPointerAddress(ptr, relative);
            if (!hasEntry)
                Cache.Add(Version, new() { { ptr, pointer } });
            else Cache[Version].Add(ptr, pointer);

            return pointer;
        }
    }
}
