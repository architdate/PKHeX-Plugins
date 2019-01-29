namespace RNGReporter
{
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
}