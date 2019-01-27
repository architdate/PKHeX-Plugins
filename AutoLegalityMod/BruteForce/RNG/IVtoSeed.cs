using System.Collections.Generic;

namespace RNGReporter
{
    /// <summary>
    ///     This class is going to do an IV/PID/Seed calculation given a particular method (1, 2 or 3, or 4). Should use the same code to develop candidate IVs.
    /// </summary>
    internal static class IVtoSeed
    {
        //  We need a function to return a list of monster seeds,
        //  which will be updated to include a method.

        public static List<Seed> GetSeeds(
            uint hp,
            uint atk,
            uint def,
            uint spa,
            uint spd,
            uint spe,
            uint nature,
            uint tid,
            FrameType type)
        {
            var seeds = new List<Seed>();
            Dictionary<uint, uint> keys;
            var rng = new PokeRngR(0);
            var forward = new XdRng(0);
            var back = new XdRngR(0);

            uint first = (hp | (atk << 5) | (def << 10)) << 16;
            uint second = (spe | (spa << 5) | (spd << 10)) << 16;

            uint pid1, pid2, pid, seed;

            uint search1, search2;

            ulong t, kmax;

            switch (type)
            {
                case FrameType.Method1:
                    keys = new Dictionary<uint, uint>();
                    for (uint i = 0; i < 256; i++)
                    {
                        uint right = (0x41c64e6d * i) + 0x6073;
                        ushort val = (ushort)(right >> 16);

                        keys[val] = i;
                        keys[--val] = i;
                    }

                    search1 = second - (first * 0x41c64e6d);
                    search2 = second - ((first ^ 0x80000000) * 0x41c64e6d);
                    for (uint cnt = 0; cnt < 256; ++cnt, search1 -= 0xc64e6d00, search2 -= 0xc64e6d00)
                    {
                        uint test = search1 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if ((((rng.Seed * 0x41c64e6d) + 0x6073) & 0x7FFF0000) == second)
                            {
                                pid2 = rng.GetNext16BitNumber();
                                pid1 = rng.GetNext16BitNumber();
                                pid = (pid1 << 16) | pid2;
                                seed = rng.GetNext32BitNumber();
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 1",
                                        Pid = pid,
                                        MonsterSeed = seed,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }

                                pid ^= 0x80000000;
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 1",
                                        Pid = pid,
                                        MonsterSeed = seed ^ 0x80000000,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }
                            }
                        }

                        test = search2 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if ((((rng.Seed * 0x41c64e6d) + 0x6073) & 0x7FFF0000) == second)
                            {
                                pid2 = rng.GetNext16BitNumber();
                                pid1 = rng.GetNext16BitNumber();
                                pid = (pid1 << 16) | pid2;
                                seed = rng.GetNext32BitNumber();
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 1",
                                        Pid = pid,
                                        MonsterSeed = seed,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }

                                pid ^= 0x80000000;
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 1",
                                        Pid = pid,
                                        MonsterSeed = seed ^ 0x80000000,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }
                            }
                        }
                    }

                    break;

                case FrameType.Method2:
                    keys = new Dictionary<uint, uint>();
                    for (uint i = 0; i < 256; i++)
                    {
                        uint right = (0x41c64e6d * i) + 0x6073;
                        ushort val = (ushort)(right >> 16);

                        keys[val] = i;
                        keys[--val] = i;
                    }

                    search1 = second - (first * 0x41c64e6d);
                    search2 = second - ((first ^ 0x80000000) * 0x41c64e6d);
                    for (uint cnt = 0; cnt < 256; ++cnt, search1 -= 0xc64e6d00, search2 -= 0xc64e6d00)
                    {
                        uint test = search1 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if ((((rng.Seed * 0x41c64e6d) + 0x6073) & 0x7FFF0000) == second)
                            {
                                rng.GetNext32BitNumber();
                                pid2 = rng.GetNext16BitNumber();
                                pid1 = rng.GetNext16BitNumber();
                                pid = (pid1 << 16) | pid2;
                                seed = rng.GetNext32BitNumber();
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 2",
                                        Pid = pid,
                                        MonsterSeed = seed,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }

                                pid ^= 0x80000000;
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 2",
                                        Pid = pid,
                                        MonsterSeed = seed ^ 0x80000000,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }
                            }
                        }

                        test = search2 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if ((((rng.Seed * 0x41c64e6d) + 0x6073) & 0x7FFF0000) == second)
                            {
                                pid2 = rng.GetNext16BitNumber();
                                pid1 = rng.GetNext16BitNumber();
                                pid = (pid1 << 16) | pid2;
                                seed = rng.GetNext32BitNumber();
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 2",
                                        Pid = pid,
                                        MonsterSeed = seed,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }

                                pid ^= 0x80000000;
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 2",
                                        Pid = pid,
                                        MonsterSeed = seed ^ 0x80000000,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }
                            }
                        }
                    }

                    break;

                case FrameType.Method4:
                    keys = new Dictionary<uint, uint>();
                    for (uint i = 0; i < 256; i++)
                    {
                        uint right = (0xc2a29a69 * i) + 0xe97e7b6a;
                        ushort val = (ushort)(right >> 16);

                        keys[val] = i;
                        keys[--val] = i;
                    }

                    search1 = second - (first * 0x41c64e6d);
                    search2 = second - ((first ^ 0x80000000) * 0x41c64e6d);
                    for (uint cnt = 0; cnt < 256; ++cnt, search1 -= 0xc64e6d00, search2 -= 0xc64e6d00)
                    {
                        uint test = search1 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if ((((rng.Seed * 0x41c64e6d) + 0x6073) & 0x7FFF0000) == second)
                            {
                                pid2 = rng.GetNext16BitNumber();
                                pid1 = rng.GetNext16BitNumber();
                                pid = (pid1 << 16) | pid2;
                                seed = rng.GetNext32BitNumber();
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 4",
                                        Pid = pid,
                                        MonsterSeed = seed,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }

                                pid ^= 0x80000000;
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 4",
                                        Pid = pid,
                                        MonsterSeed = seed ^ 0x80000000,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }
                            }
                        }

                        test = search2 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if ((((rng.Seed * 0x41c64e6d) + 0x6073) & 0x7FFF0000) == second)
                            {
                                pid2 = rng.GetNext16BitNumber();
                                pid1 = rng.GetNext16BitNumber();
                                pid = (pid1 << 16) | pid2;
                                seed = rng.GetNext32BitNumber();
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 4",
                                        Pid = pid,
                                        MonsterSeed = seed,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }

                                pid ^= 0x80000000;
                                if (pid % 25 == nature)
                                {
                                    var newSeed = new Seed
                                    {
                                        Method = "Method 4",
                                        Pid = pid,
                                        MonsterSeed = seed ^ 0x80000000,
                                        Sid = (tid ^ pid1 ^ pid2)
                                    };
                                    seeds.Add(newSeed);
                                }
                            }
                        }
                    }

                    break;

                case FrameType.ColoXD:

                    t = (second - (0x343fd * first) - 0x259ec4) & 0xFFFFFFFF;
                    kmax = (0x343fabc02 - t) / 0x80000000;

                    for (ulong k = 0; k <= kmax; k++, t += 0x80000000)
                    {
                        if ((t % 0x343fd) < 0x10000)
                        {
                            forward.Seed = back.Seed = (uint)(first | (t / 0x343fd));
                            forward.GetNext32BitNumber(2);
                            pid1 = forward.GetNext16BitNumber();
                            pid2 = forward.GetNext16BitNumber();
                            pid = (pid1 << 16) | pid2;
                            seed = back.GetNext32BitNumber();
                            if (pid % 25 == nature)
                            {
                                var newSeed = new Seed
                                {
                                    Method = "Colosseum/XD",
                                    Pid = pid,
                                    MonsterSeed = seed,
                                    Sid = (tid ^ pid1 ^ pid2)
                                };
                                seeds.Add(newSeed);
                            }

                            pid ^= 0x80008000;
                            if (pid % 25 == nature)
                            {
                                var newSeed = new Seed
                                {
                                    Method = "Colosseum/XD",
                                    Pid = pid,
                                    MonsterSeed = seed ^ 0x80000000,
                                    Sid = (tid ^ pid1 ^ pid2)
                                };
                                seeds.Add(newSeed);
                            }
                        }
                    }

                    break;

                case FrameType.Channel:
                    first = hp << 27;

                    t = ((spd << 27) - (0x284A930D * first) - 0x9A974C78) & 0xFFFFFFFF;
                    kmax = ((0x142549847b56cf2 - t) / 0x100000000);

                    for (uint k = 0; k <= kmax; k++, t += 0x100000000)
                    {
                        if ((t % 0x284A930D) >= 0x8000000)
                            continue;

                        forward.Seed = back.Seed = first | (uint)(t / 0x284A930D);
                        if (forward.GetNext32BitNumber() >> 27 != atk)
                            continue;

                        if (forward.GetNext32BitNumber() >> 27 != def)
                            continue;

                        if (forward.GetNext32BitNumber() >> 27 != spe)
                            continue;

                        if (forward.GetNext32BitNumber() >> 27 != spa)
                            continue;

                        back.GetNext32BitNumber(3);
                        pid2 = back.GetNext16BitNumber();
                        pid1 = back.GetNext16BitNumber();
                        uint sid = back.GetNext16BitNumber();
                        pid = (pid1 << 16) | pid2;
                        if ((pid2 > 7 ? 0 : 1) != (pid1 ^ sid ^ 40122))
                            pid ^= 0x80000000;
                        if (pid % 25 == nature)
                        {
                            var newSeed = new Seed
                            {
                                Method = "Channel",
                                Pid = pid,
                                MonsterSeed = back.GetNext32BitNumber(),
                                Sid = (tid ^ pid1 ^ pid2)
                            };
                            seeds.Add(newSeed);
                        }
                    }

                    break;
            }

            return seeds;
        }
    }
}