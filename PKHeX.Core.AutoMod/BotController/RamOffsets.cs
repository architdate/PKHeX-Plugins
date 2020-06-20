using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    public enum LiveHeXVersion
    {
        SWSH_Orion,
        SWSH_Rigel1,
        SWSH_Rigel2
    }

    public static class RamOffsets
    {
        public static int GetB1S1Offset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_Orion => 0x4293D8B0,
                LiveHeXVersion.SWSH_Rigel1 => 0x4506D890,
                _ => 0x4506D890
            };
        }

        public static int GetSlotSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_Orion => 344,
                LiveHeXVersion.SWSH_Rigel1 => 344,
                _ => 344
            };
        }

        public static int GetSlotCount(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_Orion => 30,
                LiveHeXVersion.SWSH_Rigel1 => 30,
                _ => 30
            };
        }

        public static int GetTrainerBlockSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_Orion => 0x110,
                LiveHeXVersion.SWSH_Rigel1 => 0x110,
                _ => 0x110
            };
        }

        public static uint GetTrainerBlockOffset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_Orion => 0x42935e48,
                LiveHeXVersion.SWSH_Rigel1 => 0x45061108,
                _ => 0x45061108
            };
        }
    }
}
