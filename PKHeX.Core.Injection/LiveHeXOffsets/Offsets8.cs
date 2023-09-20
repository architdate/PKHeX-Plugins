namespace PKHeX.Core.Injection
{
    public class Offsets8
    {
        public uint BoxInfo;
        public uint PartyInfo;
        public uint Items;
        public uint MyStatus;
        public uint Misc;
        public uint KZukan; // SCBlocks
        public uint KZukanR1; // SCBlocks
        public uint KZukanR2; // SCBlocks
        public uint KRentalTeam1; // SCBlocks

        // public uint KRentalTeam2;       // SCBlocks
        public uint KRentalTeam3; // SCBlocks
        public uint KRentalTeam4; // SCBlocks
        public uint KRentalTeam5; // SCBlocks
        public uint KRentalTeam6; // SCBlocks
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

        /// <summary>
        /// Rigel2 offsets in the ram that correspond to the same data that is found in the saveblock
        /// </summary>
        public static readonly Offsets8 Rigel2 =
            new()
            {
                MyStatus = 0x45068F18,
                Items = 0x45067A98,
                Raid = 0x450C8A70,
                RaidArmor = 0x450C94D8,
                RaidCrown = 0x450C9F40,
                Misc = 0x45072DF0,
                TrainerCard = 0x45127098,
                Fashion = 0x450748E8,
                KZukan = 0x45069120,
                KZukanR1 = 0x4506DC20,
                KZukanR2 = 0x450703B0,
            };
    }
}
