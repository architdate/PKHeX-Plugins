namespace RNGReporter
{
    public class IVPID
    {
        public uint PID;
        public uint HP;
        public uint ATK;
        public uint DEF;
        public uint SPA;
        public uint SPD;
        public uint SPE;

        public IVPID(Frame f)
        {
            PID = f.Pid;
            HP = f.Hp;
            ATK = f.Atk;
            DEF = f.Def;
            SPA = f.Spa;
            SPD = f.Spd;
            SPE = f.Spe;
        }

        public IVPID() { }
    }
}