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
        private const int MaxSpeciesID_7 = 802;
        private const int MaxSpeciesID_8 = 890;

        public static Species IllegalPKMMemeSpecies(int gen)
        {
            return gen switch
            {
                1 => Species.Diglett,
                2 => Species.Shuckle,
                3 => Species.Ludicolo,
                4 => Species.Bidoof,
                5 => Species.Stunfisk,
                6 => Species.Sliggoo,
                7 => Species.Cosmog,
                8 => Species.Meltan,
                _ => Species.Diglett
            };
        }

        public static int GetGeneration(int species)
        {
            if (species <= MaxSpeciesID_1) return 1;
            if (species <= MaxSpeciesID_2) return 2;
            if (species <= MaxSpeciesID_3) return 3;
            if (species <= MaxSpeciesID_4) return 4;
            if (species <= MaxSpeciesID_5) return 5;
            if (species <= MaxSpeciesID_6) return 6;
            if (species <= MaxSpeciesID_7) return 7;
            if (species <= MaxSpeciesID_8) return 8;
            return 1;
        }

        public static string IllegalPKMMemeNickname(int gen)
        {
            return gen switch
            {
                1 => "HOWDOIHAK",
                2 => "DONT FCKLE",
                3 => "CANTA",
                4 => "U R A DOOF",
                5 => "PANCAKE",
                6 => "SHOOT DAT GOO",
                7 => "GET IN BAG",
                8 => "MATT'S NUT",
                _ => "HOW DO I HAK"
            };
        }
    }
}
