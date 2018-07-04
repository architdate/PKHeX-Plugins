using System.Collections.Generic;

namespace Misc
{
    internal class GenericRng
    {
        //  This is the generic base that all of the other lcrngs will
        //  use. They will pass in a multiplier, and adder, and a seed.
        protected uint add;
        protected uint mult;

        public GenericRng(uint seed, uint mult, uint add)
        {
            Seed = seed;

            this.mult = mult;
            this.add = add;
        }

        public uint Seed { get; set; }

        public uint GetNext16BitNumber()
        {
            return GetNext32BitNumber() >> 16;
        }

        public virtual uint GetNext32BitNumber()
        {
            Seed = Seed * mult + add;

            return Seed;
        }

        public void GetNext32BitNumber(int num)
        {
            for (int i = 0; i < num; i++)
                Seed = Seed * mult + add;
        }
    }

    internal class PokeRng : GenericRng
    {
        public PokeRng(uint seed)
            : base(seed, 0x41c64e6d, 0x6073)
        {
        }
    }

    internal class PokeRngR : GenericRng
    {
        public PokeRngR(uint seed)
            : base(seed, 0xeeb9eb65, 0xa3561a1)
        {
        }
    }

    internal class XdRng : GenericRng
    {
        public XdRng(uint seed)
            : base(seed, 0x343FD, 0x269EC3)
        {
        }
    }

    internal class XdRngR : GenericRng
    {
        public XdRngR(uint seed)
            : base(seed, 0xB9B33155, 0xA170F641)
        {
        }
    }

    public enum FrameType
    {
        Method1,
        Method1Reverse,
        Method2,
        Method4,
        ColoXD,
        Channel
    };

    internal class Seed
    {
        //  Needs to hold all of the information about
        //  a seed that we have created from an IV and
        //  nature combo.

        //  Need to come up with a better name for this, as it
        //  cant seem to have the same name as the containing
        //  class :P
        public uint MonsterSeed { get; set; }

        public uint Pid { get; set; }

        public string Method { get; set; }

        public uint Sid { get; set; }
    }

    internal enum CompareType
    {
        None,
        Equal,
        GtEqual,
        LtEqual,
        NotEqual,
        Even,
        Odd,
        Hidden,
        HiddenEven,
        HiddenOdd,
        HiddenTrickRoom
    };

    internal class IVFilter
    {
        public CompareType atkCompare;
        public uint atkValue;
        public CompareType defCompare;
        public uint defValue;
        public CompareType hpCompare;
        public uint hpValue;
        public CompareType spaCompare;
        public uint spaValue;
        public CompareType spdCompare;
        public uint spdValue;
        public CompareType speCompare;
        public uint speValue;

        public IVFilter(uint hpValue, CompareType hpCompare, uint atkValue, CompareType atkCompare, uint defValue,
                        CompareType defCompare, uint spaValue, CompareType spaCompare, uint spdValue,
                        CompareType spdCompare, uint speValue, CompareType speCompare)
        {
            this.hpValue = hpValue;
            this.hpCompare = hpCompare;
            this.atkValue = atkValue;
            this.atkCompare = atkCompare;
            this.defValue = defValue;
            this.defCompare = defCompare;
            this.spaValue = spaValue;
            this.spaCompare = spaCompare;
            this.spdValue = spdValue;
            this.spdCompare = spdCompare;
            this.speValue = speValue;
            this.speCompare = speCompare;
        }

        public IVFilter()
        {
            hpValue = 0;
            hpCompare = CompareType.None;
            atkValue = 0;
            atkCompare = CompareType.None;
            defValue = 0;
            defCompare = CompareType.None;
            spaValue = 0;
            spaCompare = CompareType.None;
            spdValue = 0;
            spdCompare = CompareType.None;
            speValue = 0;
            speCompare = CompareType.None;
        }
    }

    public class Frame
    {
        /// <summary>
        ///     1 or 2 for the ability number, best we can do since we don't know what the monster is actually going to be.
        /// </summary>
        private uint ability;

        private uint dv;
        private uint id;
        private uint pid;
        private uint sid;

        internal Frame(FrameType frameType)
        {
            Shiny = false;
            Offset = 0;
            FrameType = frameType;
        }

        public uint RngResult { get; set; }

        public uint Seed { get; set; }

        public uint Number { get; set; }

        public uint Offset { get; set; }

        public FrameType FrameType { get; set; }

        public bool Shiny { get; private set; }

        //  The following are cacluated differently based
        //  on the creation method of the pokemon.

        public uint Pid
        {
            get { return pid; }
            set
            {
                Nature = (value % 25);
                ability = (value & 1);

                //  figure out if we are shiny here
                uint tid = (id & 0xffff) | ((sid & 0xffff) << 16);
                uint a = value ^ tid;
                uint b = a & 0xffff;
                uint c = (a >> 16);
                uint d = b ^ c;
                if (d < 8)
                    Shiny = true;

                pid = value;
            }
        }

        public uint Dv
        {
            get { return dv; }
            set
            {
                //  Split up our DV
                var dv1 = (ushort)value;
                var dv2 = (ushort)(value >> 16);

                //  Get the actual Values
                Hp = (uint)dv1 & 0x1f;
                Atk = ((uint)dv1 & 0x3E0) >> 5;
                Def = ((uint)dv1 & 0x7C00) >> 10;

                Spe = (uint)dv2 & 0x1f;
                Spa = ((uint)dv2 & 0x3E0) >> 5;
                Spd = ((uint)dv2 & 0x7C00) >> 10;

                //  Set the actual dv value
                dv = value;
            }
        }

        public uint Nature { get; set; }

        public uint Hp { get; set; }

        public uint Atk { get; set; }

        public uint Def { get; set; }

        public uint Spa { get; set; }

        public uint Spd { get; set; }

        public uint Spe { get; set; }

        /// <summary>
        ///     Generic Frame creation where the values that are to be used for each part are passed in explicitly. There will be other methods to support splitting a list and then passing them to this for creation.
        /// </summary>
        public static Frame GenerateFrame(
            uint seed,
            FrameType frameType,
            uint number,
            uint rngResult,
            uint pid1,
            uint pid2,
            uint dv1,
            uint dv2,
            uint id,
            uint sid,
            uint offset)
        {
            var frame = new Frame(frameType)
            {
                Seed = seed,
                Number = number,
                RngResult = rngResult,
                Offset = offset,
                id = id,
                sid = sid,
                Pid = (pid2 << 16) | pid1,
                Dv = (dv2 << 16) | dv1
            };


            //  Set up the ID and SID before we calculate
            //  the pid, as we are going to need this.


            return frame;
        }

        // for Methods 1, 2, 4
        public static Frame GenerateFrame(
            uint seed,
            FrameType frameType,
            uint number,
            uint rngResult,
            uint pid1,
            uint pid2,
            uint dv1,
            uint dv2,
            uint id,
            uint sid)
        {
            var frame = new Frame(frameType)
            {
                Seed = seed,
                Number = number,
                RngResult = rngResult,
                id = id,
                sid = sid,
                Pid = (pid2 << 16) | pid1,
                Dv = (dv2 << 16) | dv1
            };
            //  Set up the ID and SID before we calculate
            //  the pid, as we are going to need this.


            return frame;
        }

        // for channel
        public static Frame GenerateFrame(
            uint seed,
            FrameType frameType,
            uint number,
            uint rngResult,
            uint pid1,
            uint pid2,
            uint dv1,
            uint dv2,
            uint dv3,
            uint dv4,
            uint dv5,
            uint dv6,
            uint id,
            uint sid)
        {
            if ((pid2 > 7 ? 0 : 1) != (pid1 ^ 40122 ^ sid))
                pid1 ^= 0x8000;

            var frame = new Frame(frameType)
            {
                Seed = seed,
                Number = number,
                RngResult = rngResult,
                id = id,
                sid = sid,
                Pid = (pid1 << 16) | pid2
            };

            frame.Hp = dv1;
            frame.Atk = dv2;
            frame.Def = dv3;
            frame.Spa = dv4;
            frame.Spd = dv5;
            frame.Spe = dv6;

            return frame;
        }
    }

    internal class FrameCompare
    {
        private readonly CompareType atkCompare;
        private readonly uint atkValue;
        private readonly CompareType defCompare;
        private readonly uint defValue;
        private readonly CompareType hpCompare;
        private readonly uint hpValue;
        private readonly CompareType spaCompare;
        private readonly uint spaValue;
        private readonly CompareType spdCompare;
        private readonly uint spdValue;
        private readonly CompareType speCompare;
        private readonly uint speValue;

        public FrameCompare(IVFilter ivBase, uint nature)
        {
            if (ivBase != null)
            {
                hpValue = ivBase.hpValue;
                hpCompare = ivBase.hpCompare;
                atkValue = ivBase.atkValue;
                atkCompare = ivBase.atkCompare;
                defValue = ivBase.defValue;
                defCompare = ivBase.defCompare;
                spaValue = ivBase.spaValue;
                spaCompare = ivBase.spaCompare;
                spdValue = ivBase.spdValue;
                spdCompare = ivBase.spdCompare;
                speValue = ivBase.speValue;
                speCompare = ivBase.speCompare;
            }
            Nature = nature;
        }

        public uint Nature { get; }

        public bool Compare(Frame frame)
        {
            if (Nature != frame.Nature)
                return false;

            if (!CompareIV(hpCompare, frame.Hp, hpValue))
                return false;

            if (!CompareIV(atkCompare, frame.Atk, atkValue))
                return false;

            if (!CompareIV(defCompare, frame.Def, defValue))
                return false;

            if (!CompareIV(spaCompare, frame.Spa, spaValue))
                return false;

            if (!CompareIV(spdCompare, frame.Spd, spdValue))
                return false;

            if (!CompareIV(speCompare, frame.Spe, speValue))
                return false;

            return true;
        }

        public bool CompareIV(CompareType compare, uint frameIv, uint testIv)
        {
            bool passed = true;

            //  Anything set not to compare is considered pass
            if (compare != CompareType.None)
            {
                switch (compare)
                {
                    case CompareType.Equal:
                        if (frameIv != testIv)
                            passed = false;
                        break;

                    case CompareType.GtEqual:
                        if (frameIv < testIv)
                            passed = false;
                        break;

                    case CompareType.LtEqual:
                        if (frameIv > testIv)
                            passed = false;
                        break;

                    case CompareType.NotEqual:
                        if (frameIv == testIv)
                            passed = false;
                        break;

                    case CompareType.Even:
                        if ((frameIv & 1) != 0)
                            passed = false;

                        break;

                    case CompareType.Odd:
                        if ((frameIv & 1) == 0)
                            passed = false;

                        break;

                    case CompareType.Hidden:
                        if ((((frameIv + 2) & 3) != 0) && (((frameIv + 5) & 3) != 0))
                            passed = false;
                        break;

                    case CompareType.HiddenEven:
                        if (((frameIv + 2) & 3) != 0)
                            passed = false;
                        break;

                    case CompareType.HiddenOdd:
                        if (((frameIv + 5) & 3) != 0)
                            passed = false;
                        break;
                }
            }

            return passed;
        }
    }

    internal class FrameGenerator
    {
        protected Frame frame;
        protected FrameType frameType = FrameType.Method1;
        protected List<Frame> frames;
        private uint lastseed;
        protected uint maxResults;
        protected List<uint> rngList;

        public FrameGenerator()
        {
            maxResults = 1000000;
            InitialFrame = 1;
            InitialSeed = 0;
        }

        public FrameType FrameType
        {
            get { return frameType; }
            set
            {
                frameType = value;
            }
        }

        public ulong InitialSeed { get; set; }

        public uint InitialFrame { get; set; }

        // by declaring these only once you get a huge performance boost

        // This method ensures that an RNG is only created once,
        // and not every time a Generate function is called

        public List<Frame> Generate(
            FrameCompare frameCompare,
            uint id,
            uint sid)
        {
            frames = new List<Frame>();

            if (frameType == FrameType.ColoXD)
            {
                var rng = new XdRng((uint)InitialSeed);
                rngList = new List<uint>();

                for (uint cnt = 1; cnt < InitialFrame; cnt++)
                    rng.GetNext32BitNumber();

                for (uint cnt = 0; cnt < 12; cnt++)
                    rngList.Add(rng.GetNext16BitNumber());

                for (uint cnt = 0; cnt < maxResults; cnt++, rngList.RemoveAt(0), rngList.Add(rng.GetNext16BitNumber()))
                {
                    switch (frameType)
                    {
                        case FrameType.ColoXD:
                            frame = Frame.GenerateFrame(
                                0,
                                FrameType.ColoXD,
                                cnt + InitialFrame,
                                rngList[0],
                                rngList[4],
                                rngList[3],
                                rngList[0],
                                rngList[1],
                                id, sid);

                            break;

                        case FrameType.Channel:
                            frame = Frame.GenerateFrame(
                                0,
                                FrameType.Channel,
                                cnt + InitialFrame,
                                rngList[0],
                                rngList[1],
                                rngList[2],
                                (rngList[6]) >> 11,
                                (rngList[7]) >> 11,
                                (rngList[8]) >> 11,
                                (rngList[10]) >> 11,
                                (rngList[11]) >> 11,
                                (rngList[9]) >> 11,
                                40122, rngList[0]);

                            break;
                    }

                    if (frameCompare.Compare(frame))
                    {
                        frames.Add(frame); break;
                    }
                }
            }
            else
            {
                //  We are going to grab our initial set of rngs here and
                //  then start our loop so that we can iterate as many
                //  times as we have to.
                var rng = new PokeRng((uint)InitialSeed);
                rngList = new List<uint>();

                for (uint cnt = 1; cnt < InitialFrame; cnt++)
                    rng.GetNext32BitNumber();

                for (uint cnt = 0; cnt < 20; cnt++)
                    rngList.Add(rng.GetNext16BitNumber());

                lastseed = rng.Seed;

                for (uint cnt = 0; cnt < maxResults; cnt++, rngList.RemoveAt(0), rngList.Add(rng.GetNext16BitNumber()))
                {
                    switch (frameType)
                    {
                        case FrameType.Method1:
                            frame =
                                Frame.GenerateFrame(
                                    0,
                                    FrameType.Method1,
                                    cnt + InitialFrame,
                                    rngList[0],
                                    rngList[0],
                                    rngList[1],
                                    rngList[2],
                                    rngList[3],
                                    id, sid, cnt);

                            break;

                        case FrameType.Method1Reverse:
                            frame =
                                Frame.GenerateFrame(
                                    0,
                                    FrameType.Method1Reverse,
                                    cnt + InitialFrame,
                                    rngList[0],
                                    rngList[1],
                                    rngList[0],
                                    rngList[2],
                                    rngList[3],
                                    id, sid, cnt);

                            break;

                        case FrameType.Method2:
                            frame =
                                Frame.GenerateFrame(
                                    0,
                                    FrameType.Method2,
                                    cnt + InitialFrame,
                                    rngList[0],
                                    rngList[0],
                                    rngList[1],
                                    rngList[3],
                                    rngList[4],
                                    id, sid, cnt);

                            break;

                        case FrameType.Method4:
                            frame =
                                Frame.GenerateFrame(
                                    0,
                                    FrameType.Method4,
                                    cnt + InitialFrame,
                                    rngList[0],
                                    rngList[0],
                                    rngList[1],
                                    rngList[2],
                                    rngList[4],
                                    id, sid, cnt);

                            break;
                    }

                    //  Now we need to filter and decide if we are going
                    //  to add this to our collection for display to the
                    //  user.

                    if (frameCompare.Compare(frame))
                    {
                        frames.Add(frame); break;
                    }
                }
            }

            return frames;
        }
    }

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
                        uint right = 0x41c64e6d * i + 0x6073;
                        ushort val = (ushort)(right >> 16);

                        keys[val] = i;
                        keys[--val] = i;
                    }

                    search1 = second - first * 0x41c64e6d;
                    search2 = second - (first ^ 0x80000000) * 0x41c64e6d;
                    for (uint cnt = 0; cnt < 256; ++cnt, search1 -= 0xc64e6d00, search2 -= 0xc64e6d00)
                    {
                        uint test = search1 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if (((rng.Seed * 0x41c64e6d + 0x6073) & 0x7FFF0000) == second)
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
                            if (((rng.Seed * 0x41c64e6d + 0x6073) & 0x7FFF0000) == second)
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
                        uint right = 0x41c64e6d * i + 0x6073;
                        ushort val = (ushort)(right >> 16);

                        keys[val] = i;
                        keys[--val] = i;
                    }

                    search1 = second - first * 0x41c64e6d;
                    search2 = second - (first ^ 0x80000000) * 0x41c64e6d;
                    for (uint cnt = 0; cnt < 256; ++cnt, search1 -= 0xc64e6d00, search2 -= 0xc64e6d00)
                    {
                        uint test = search1 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if (((rng.Seed * 0x41c64e6d + 0x6073) & 0x7FFF0000) == second)
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
                            if (((rng.Seed * 0x41c64e6d + 0x6073) & 0x7FFF0000) == second)
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
                        uint right = 0xc2a29a69 * i + 0xe97e7b6a;
                        ushort val = (ushort)(right >> 16);

                        keys[val] = i;
                        keys[--val] = i;
                    }

                    search1 = second - first * 0x41c64e6d;
                    search2 = second - (first ^ 0x80000000) * 0x41c64e6d;
                    for (uint cnt = 0; cnt < 256; ++cnt, search1 -= 0xc64e6d00, search2 -= 0xc64e6d00)
                    {
                        uint test = search1 >> 16;

                        if (keys.ContainsKey(test))
                        {
                            rng.Seed = (first | (cnt << 8) | keys[test]);
                            if (((rng.Seed * 0x41c64e6d + 0x6073) & 0x7FFF0000) == second)
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
                            if (((rng.Seed * 0x41c64e6d + 0x6073) & 0x7FFF0000) == second)
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

                    t = ((second - 0x343fd * first) - 0x259ec4) & 0xFFFFFFFF;
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
                            seed = back.GetNext32BitNumber(); ;
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

                    t = (((spd << 27) - (0x284A930D * first)) - 0x9A974C78) & 0xFFFFFFFF;
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

    public class IVtoPIDGenerator
    {
        public static string[] M1PID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Method1);

            if (seeds.Count == 0)
            {
                return new string[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[0].Sid.ToString();
            return ans;
        }

        public static string[] M2PID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Method2);

            if (seeds.Count == 0)
            {
                return new string[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[1].Sid.ToString();
            return ans;
        }

        public static string[] M4PID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Method4);

            if (seeds.Count == 0)
            {
                return new string[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[1].Sid.ToString();
            return ans;
        }

        public static string[] XDPID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.ColoXD);

            if (seeds.Count == 0)
            {
                return new string[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[0].Sid.ToString();
            return ans;
        }

        public static string[] ChannelPID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Channel);

            if (seeds.Count == 0)
            {
                return new string[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[0].Sid.ToString();
            return ans;
        }

        public string[] generateWishmkr(uint targetNature)
        {
            uint finalPID = 0;
            uint finalHP = 0;
            uint finalATK = 0;
            uint finalDEF = 0;
            uint finalSPA = 0;
            uint finalSPD = 0;
            uint finalSPE = 0;
            for (uint x = 0; x <= 0xFFFF; x++)
            {
                uint pid1 = forward(x);
                uint pid2 = forward(pid1);
                uint pid = (pid1 & 0xFFFF0000) | (pid2 >> 16);
                uint nature = pid % 25;

                if (nature == targetNature)
                {
                    uint ivs1 = forward(pid2);
                    uint ivs2 = forward(ivs1);
                    ivs1 >>= 16;
                    ivs2 >>= 16;
                    uint[] ivs = CreateIVs(ivs1, ivs2);
                    if (ivs != null)
                    {
                        finalPID = pid;
                        finalHP = ivs[0];
                        finalATK = ivs[1];
                        finalDEF = ivs[2];
                        finalSPA = ivs[3];
                        finalSPD = ivs[4];
                        finalSPE = ivs[5];
                        break;
                    }
                }
            }
            return new string[] { finalPID.ToString("X"), finalHP.ToString(), finalATK.ToString(), finalDEF.ToString(), finalSPA.ToString(), finalSPD.ToString(), finalSPE.ToString() };
        }

        private uint forward(uint seed)
        {
            return seed * 0x41c64e6d + 0x6073;
        }

        private uint[] CreateIVs(uint iv1, uint ivs2)
        {
            uint[] ivs = new uint[6];

            for (int x = 0; x < 3; x++)
            {
                ivs[x] = (iv1 >> (x * 5)) & 31;
            }

            ivs[3] = (ivs2 >> 5) & 31;
            ivs[4] = (ivs2 >> 10) & 31;
            ivs[5] = ivs2 & 31;

            return ivs;
        }

        private static IVFilter Hptofilter(string hiddenpower)
        {
            if (hiddenpower == "dark")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "dragon")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "ice")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "psychic")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "electric")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "grass")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "water")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "fire")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "steel")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "ghost")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "bug")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "rock")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "ground")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "poison")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "flying")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "fighting")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven);
            }
            else
            {
                return new IVFilter();
            }
        }

        public static string[] GetIVPID(uint nature, string hiddenpower, bool XD = false, string method = "")
        {
            var generator = new FrameGenerator();
            if (XD || method == "XD")
                generator = new FrameGenerator{FrameType = FrameType.ColoXD};
            if (method == "M2")
                generator = new FrameGenerator{FrameType = FrameType.Method2};
            if (method == "BACD_R")
            {
                generator = new FrameGenerator{FrameType = FrameType.Method1Reverse};
                IVtoPIDGenerator bacdr = new IVtoPIDGenerator();
                return bacdr.generateWishmkr(nature);
            }
            FrameCompare frameCompare = new FrameCompare(Hptofilter(hiddenpower), nature);
            List<Frame> frames = generator.Generate(frameCompare, 0, 0);
            //Console.WriteLine("Num frames: " + frames.Count);
            return new string[] { frames[0].Pid.ToString("X"), frames[0].Hp.ToString(), frames[0].Atk.ToString(), frames[0].Def.ToString(), frames[0].Spa.ToString(), frames[0].Spd.ToString(), frames[0].Spe.ToString() };
        }
    }
}