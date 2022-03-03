namespace PKHeX.Core.AutoMod
{
    public static class EasterEggs
    {
        private const int MaxSpeciesID_1 = 151;
        private const int MaxSpeciesID_2 = 251;
        private const int MaxSpeciesID_3 = 386;
        private const int MaxSpeciesID_4 = 493;
        private const int MaxSpeciesID_5 = 649;
        private const int MaxSpeciesID_6 = 721;
        private const int MaxSpeciesID_7 = 807;
        private const int MaxSpeciesID_8 = 890;

        public static Species GetMemeSpecies(int gen, PKM format) => gen switch
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
            _ => Species.Diglett,
        };

        public static int GetGeneration(int species) => species switch
        {
            <= MaxSpeciesID_1 => 1,
            <= MaxSpeciesID_2 => 2,
            <= MaxSpeciesID_3 => 3,
            <= MaxSpeciesID_4 => 4,
            <= MaxSpeciesID_5 => 5,
            <= MaxSpeciesID_6 => 6,
            <= MaxSpeciesID_7 => 7,
            <= MaxSpeciesID_8 => 8,
            _ => 1,
        };

        public static string GetMemeNickname(int gen, PKM format) => gen switch
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
            _ => "HOW DO I HAK",
        };
    }
}
