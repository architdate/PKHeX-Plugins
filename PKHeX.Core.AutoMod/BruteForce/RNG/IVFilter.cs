namespace RNGReporter
{
    internal class IVFilter
    {
        public uint atkValue;
        public uint defValue;
        public uint hpValue;
        public uint spaValue;
        public uint spdValue;
        public uint speValue;
        public CompareType atkCompare;
        public CompareType defCompare;
        public CompareType hpCompare;
        public CompareType spaCompare;
        public CompareType spdCompare;
        public CompareType speCompare;

        public IVFilter(
            uint hpValue, CompareType hpCompare,
            uint atkValue, CompareType atkCompare,
            uint defValue, CompareType defCompare,
            uint spaValue, CompareType spaCompare,
            uint spdValue, CompareType spdCompare,
            uint speValue, CompareType speCompare)
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
        }
    }
}