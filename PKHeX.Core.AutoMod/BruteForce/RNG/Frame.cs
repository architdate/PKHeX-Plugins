namespace RNGReporter
{
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
            get => pid;
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
            get => dv;
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
        public static Frame GenerateFrame(uint seed, FrameType frameType, uint number, uint rngResult,
            uint pid1, uint pid2, uint dv1, uint dv2,
            uint id, uint sid,
            uint offset)
        {
            //  Set up the ID and SID before we calculate
            //  the pid, as we are going to need this.
            return new Frame(frameType)
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
        }

        // for Methods 1, 2, 4
        public static Frame GenerateFrame(uint seed, FrameType frameType, uint number, uint rngResult,
            uint pid1, uint pid2, uint dv1, uint dv2,
            uint id, uint sid)
        {
            //  Set up the ID and SID before we calculate
            //  the pid, as we are going to need this.
            return new Frame(frameType)
            {
                Seed = seed,
                Number = number,
                RngResult = rngResult,
                id = id,
                sid = sid,
                Pid = (pid2 << 16) | pid1,
                Dv = (dv2 << 16) | dv1
            };
        }

        // for channel
        public static Frame GenerateFrame(uint seed, FrameType frameType, uint number, uint rngResult,
            uint pid1, uint pid2, uint dv1, uint dv2, uint dv3, uint dv4, uint dv5, uint dv6,
            uint id, uint sid)
        {
            if ((pid2 > 7 ? 0 : 1) != (pid1 ^ 40122 ^ sid))
                pid1 ^= 0x8000;

            return new Frame(frameType)
            {
                Seed = seed,
                Number = number,
                RngResult = rngResult,
                id = id,
                sid = sid,
                Pid = (pid1 << 16) | pid2,
                Hp = dv1,
                Atk = dv2,
                Def = dv3,
                Spa = dv4,
                Spd = dv5,
                Spe = dv6
            };
        }
    }
}