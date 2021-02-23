namespace PKHeX.Core.Injection
{
    public class Offsets8
    {
        public uint BoxInfo;
        public uint PartyInfo;
        public uint Items;
        public uint MyStatus;
        public uint Misc;
        public uint Zukan;
        public uint BoxLayout;
        public uint Played;
        public uint Fused;
        public uint Daycare;
        public uint Records;
        public uint TrainerCard;
        public uint Fashion;
        public uint Raid;
        public uint RaidArmor;
        public uint RaidCrown;
        public uint TitleScreen;
        public uint TeamIndexes;
        public uint FameTime;

        public static Offsets8 Rigel2 = new() { MyStatus = 0x45068F18 };
    }

}
