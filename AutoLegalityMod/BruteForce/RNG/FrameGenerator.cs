using System.Collections.Generic;

namespace RNGReporter
{
    internal class FrameGenerator
    {
        protected Frame frame;
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

        public FrameType FrameType { get; set; }
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

            if (FrameType == FrameType.ColoXD)
            {
                var rng = new XdRng((uint)InitialSeed);
                rngList = new List<uint>();

                for (uint cnt = 1; cnt < InitialFrame; cnt++)
                    rng.GetNext32BitNumber();

                for (uint cnt = 0; cnt < 12; cnt++)
                    rngList.Add(rng.GetNext16BitNumber());

                for (uint cnt = 0; cnt < maxResults; cnt++, rngList.RemoveAt(0), rngList.Add(rng.GetNext16BitNumber()))
                {
                    switch (FrameType)
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
                    switch (FrameType)
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
}