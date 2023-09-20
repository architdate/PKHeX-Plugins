namespace PKHeX.Core.AutoMod
{
    public static class EasterEggs
    {
        private const int MaxSpeciesID1 = 151;
        private const int MaxSpeciesID2 = 251;
        private const int MaxSpeciesID3 = 386;
        private const int MaxSpeciesID4 = 493;
        private const int MaxSpeciesID5 = 649;
        private const int MaxSpeciesID6 = 721;
        private const int MaxSpeciesID7 = 807;
        private const int MaxSpeciesID8 = 890;
        private const int MaxSpeciesID9 = 1010;

        public static Species GetMemeSpecies(int gen, PKM format) =>
            gen switch
            {
                1 => Species.Diglett,
                2 => Species.Shuckle,
                3 => Species.Ludicolo,
                4 => Species.Bidoof,
                5 => Species.Stunfisk,
                6 => Species.Sliggoo,
                7 => Species.Cosmog,
                8 when format is PA8 => Species.Porygon,
                8 => Species.Meltan,
                9 => Species.Wiglett,
                _ => Species.Diglett,
            };

        public static int GetGeneration(int species) =>
            species switch
            {
                <= MaxSpeciesID1 => 1,
                <= MaxSpeciesID2 => 2,
                <= MaxSpeciesID3 => 3,
                <= MaxSpeciesID4 => 4,
                <= MaxSpeciesID5 => 5,
                <= MaxSpeciesID6 => 6,
                <= MaxSpeciesID7 => 7,
                <= MaxSpeciesID8 => 8,
                <= MaxSpeciesID9 => 9,
                _ => 1,
            };

        public static string GetMemeNickname(int gen, PKM format) =>
            gen switch
            {
                1 => "HOWDOIHAK",
                2 => "DONT FCKLE",
                3 => "CANTA",
                4 => "U R A DOOF",
                5 => "PANCAKE",
                6 => "SHOOT DAT GOO",
                7 => "GET IN BAG",
                8 when format is PA8 => "BAD DATA BOI",
                8 => "MATT'S NUT",
                9 => "WIGGLE",
                _ => "HOW DO I HAK",
            };
    }
}
