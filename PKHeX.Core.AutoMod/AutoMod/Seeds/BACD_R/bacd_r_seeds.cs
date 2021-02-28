using System.Collections.Generic;
using PKHeX.Core;

namespace PKHeX.Core.AutoMod
{
    // There only exists 9 shiny seeds for wishmaker jirachi bacd_r, just have a list and gen based on nature
    public static class bacd_r_seeds
    {
        private static readonly Dictionary<Nature, uint> ShinyWishmakerSeeds = new Dictionary<Nature, uint>
        {
            { Nature.Bashful , 0x353d },
            { Nature.Careful , 0xf500 },
            { Nature.Docile  , 0xecdd },
            { Nature.Hasty   , 0x9359 },
            { Nature.Jolly   , 0xcf37 },
            { Nature.Lonely  , 0x7236 },
            { Nature.Naughty , 0xa030 },
            { Nature.Timid   , 0x7360 },
            { Nature.Serious , 0x3d60 },
        };

        public static uint GetShinyWishmakerSeed(Nature nature) => ShinyWishmakerSeeds.ContainsKey(nature) ? ShinyWishmakerSeeds[nature] : 0;
    }
}
