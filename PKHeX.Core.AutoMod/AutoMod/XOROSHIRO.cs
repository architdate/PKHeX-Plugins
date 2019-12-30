using System.Runtime.CompilerServices;

namespace PKHeX.Core.AutoMod
{
    public class XOROSHIRO
    {
        public const ulong XOROSHIRO_CONST = 0x82A2B175229D6A5B;

        private ulong s0, s1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        public XOROSHIRO(ulong seed)
        {
            s0 = seed;
            s1 = XOROSHIRO_CONST;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Next()
        {
            var _s0 = s0;
            var _s1 = s1;
            ulong result = _s0 + _s1;

            _s1 ^= _s0;
            // Final calculations and store back to fields
            s0 = RotateLeft(_s0, 24) ^ _s1 ^ (_s1 << 16);
            s1 = RotateLeft(_s1, 37);

            return result;
        }

        public void Reset(ulong seed)
        {
            s0 = seed;
            s1 = XOROSHIRO_CONST;
        }

        public ulong NextInt(ulong MOD = 0xFFFFFFFF)
        {
            ulong mask = GetBitmask(MOD);
            ulong res;
            do
            {
                res = Next() & mask;
            } while (res >= MOD);
            return res;
        }

        /// <summary>
        /// Next Power of Two
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong GetBitmask(ulong x)
        {
            x--; // comment out to always take the next biggest power of two, even if x is already a power of two
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x;
        }
    }
}