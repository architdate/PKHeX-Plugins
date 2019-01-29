using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    public static class BruteTables
    {
        public static readonly int[] Legendaries =
        {
            144, 145, 146, 150, 151, 243, 244, 245, 249, 250, 251, 377, 378, 379, 380, 381, 382, 383, 384, 385,
            386, 480, 481, 482, 483, 484, 485, 486, 487, 488, 491, 492, 493, 494, 638, 639, 640, 642, 641, 643,
            644, 645, 646, 647, 648, 649, 716, 717, 718, 719, 720, 721, 785, 786, 787, 788, 789, 790, 791, 792,
            793, 794, 795, 796, 797, 798, 799, 800, 801, 802, 803, 804, 805, 806, 807, 132
        };

        public static readonly int[] EventSpecies = { 719, 649, 720, 385, 647, 648, 721, 801, 802, 807 };

        public static readonly GameVersion[] GameVersionList =
        {
            GameVersion.GP, GameVersion.GE, GameVersion.GO,
            GameVersion.UM, GameVersion.US, GameVersion.MN, GameVersion.SN,
            GameVersion.AS, GameVersion.OR, GameVersion.X, GameVersion.Y,
            GameVersion.B, GameVersion.B2, GameVersion.W, GameVersion.W2,
            GameVersion.D, GameVersion.P, GameVersion.Pt, GameVersion.HG, GameVersion.SS,
            GameVersion.R, GameVersion.S, GameVersion.E, GameVersion.FR, GameVersion.LG, GameVersion.CXD,
            GameVersion.RD, GameVersion.GN, GameVersion.BU, GameVersion.YW, GameVersion.GD,
            GameVersion.SV, GameVersion.C
        };

        public static readonly HashSet<int> UltraBeastBall = new HashSet<int>
        {
            793,
            794,
            795,
            796,
            797,
            798,
            799,
            805,
            806,
        };

        public static int GetRNGListIndex(PIDType Method)
        {
            switch (Method)
            {
                case PIDType.Method_2:
                    return 0;
                case PIDType.BACD_R:
                    return 1;
                default:
                    return -1;
            }
        }

        public static readonly Dictionary<int, int[]>[] WC3RNGList =
        {
            new Dictionary<int, int[]>
            { // M2
                {043, new[]{073}}, // Oddish with Leech Seed
                {044, new[]{073}}, // Gloom
                {045, new[]{073}}, // Vileplume
                {182, new[]{073}}, // Belossom
                {052, new[]{080}}, // Meowth with Petal Dance
                {053, new[]{080}}, //Persian
                {060, new[]{186}}, // Poliwag with Sweet Kiss
                {061, new[]{186}},
                {062, new[]{186}},
                {186, new[]{186}},
                {069, new[]{298}}, // Bellsprout with Teeter Dance
                {070, new[]{298}},
                {071, new[]{298}},
                {083, new[]{273, 281}}, // Farfetch'd with Wish & Yawn
                {096, new[]{273, 187}}, // Drowzee with Wish & Belly Drum
                {097, new[]{273, 187}},
                {102, new[]{273, 230}}, // Exeggcute with Wish & Sweet Scent
                {103, new[]{273, 230}},
                {108, new[]{273, 215}}, // Lickitung with Wish & Heal Bell
                {463, new[]{273, 215}},
                {113, new[]{273, 230}}, // Chansey with Wish & Sweet Scent
                {242, new[]{273, 230}},
                {115, new[]{273, 281}}, // Kangaskhan with Wish & Yawn
                {054, new[]{300}}, // Psyduck with Mud Sport
                {055, new[]{300}},
                {172, new[]{266, 058}}, // Pichu with Follow me
                {025, new[]{266, 058}},
                {026, new[]{266, 058}},
                {174, new[]{321}}, // Igglybuff with Tickle
                {039, new[]{321}},
                {040, new[]{321}},
                {222, new[]{300}}, // Corsola with Mud Sport
                {276, new[]{297}}, // Taillow with Feather Dance
                {277, new[]{297}},
                {283, new[]{300}}, // Surskit with Mud Sport
                {284, new[]{300}},
                {293, new[]{298}}, // Whismur with Teeter Dance
                {294, new[]{298}},
                {295, new[]{298}},
                {300, new[]{205, 006}}, // Skitty with Rollout or Payday
                {301, new[]{205, 006}},
                {311, new[]{346}}, // Plusle with Water Sport
                {312, new[]{300}}, // Minun with Mud Sport
                {325, new[]{253}}, // Spoink with Uproar
                {326, new[]{253}},
                {327, new[]{047}}, // Spinda with Sing
                {331, new[]{227}}, // Cacnea with Encore
                {332, new[]{227}},
                {341, new[]{346}}, // Corphish with Water Sport
                {342, new[]{346}},
                {360, new[]{321}}, // Wynaut with Tickle
                {202, new[]{321}},
                // Pokemon Box Events (M2)
                {263, new[]{245}}, // Zigzagoon with Extreme Speed
                {264, new[]{245}},
                {333, new[]{206}}, // False Swipe Swablu
                {334, new[]{206}},
                // Pay Day Skitty and evolutions (Accounted for with Rollout Skitty)
                // Surf Pichu and evolutions (Accounted for with Follow Me Pichu)
            },
            new Dictionary<int, int[]>
            { // BACD_R
                {172, new[]{298, 273} }, // Pichu with Teeter Dance
                {025, new[]{298, 273} },
                {026, new[]{298, 273} },
                {280, new[]{204, 273} }, // Ralts with Charm
                {281, new[]{204, 273} },
                {282, new[]{204, 273} },
                {475, new[]{204, 273} },
                {359, new[]{180, 273} }, // Absol with Spite
                {371, new[]{334, 273} }, // Bagon with Iron Defense
                {372, new[]{334, 273} },
                {373, new[]{334, 273} },
                {385, new[]{034, 273} }
            }
        };
    }
}