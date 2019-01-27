namespace RNGReporter
{
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
}