namespace RNGReporter
{
    internal class Seed
    {
        internal Seed(string method) => Method = method;

        //  Needs to hold all of the information about
        //  a seed that we have created from an IV and
        //  nature combo.

        //  Need to come up with a better name for this, as it
        //  cant seem to have the same name as the containing
        //  class :P
        public uint MonsterSeed { get; set; }

        public uint Pid { get; set; }

        public readonly string Method;

        public uint Sid { get; set; }
    }
}