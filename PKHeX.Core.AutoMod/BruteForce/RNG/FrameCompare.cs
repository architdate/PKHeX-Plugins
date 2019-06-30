namespace RNGReporter
{
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

        public static bool CompareIV(CompareType compare, uint frameIv, uint testIv)
        {
            //  Anything set not to compare is considered pass
            switch (compare)
            {
                case CompareType.Equal:      return frameIv == testIv;
                case CompareType.GtEqual:    return frameIv >= testIv;
                case CompareType.LtEqual:    return frameIv <= testIv;
                case CompareType.NotEqual:   return frameIv != testIv;
                case CompareType.Even:       return (frameIv & 1) == 0;
                case CompareType.Odd:        return (frameIv & 1) == 1;
                case CompareType.HiddenEven: return ((frameIv + 2) & 3) == 0;
                case CompareType.HiddenOdd:  return ((frameIv + 5) & 3) == 0;
                case CompareType.Hidden:     return ((frameIv + 2) & 3) == 0
                                                 || ((frameIv + 5) & 3) == 0;
                default:
                    return true;
            }
        }
    }
}