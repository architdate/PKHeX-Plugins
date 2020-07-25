using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    public enum LiveHeXVersion
    {
        LGPE_v102,
        SWSH_Orion,
        SWSH_Rigel1,
        SWSH_Rigel2
    }

    public static class RamOffsets
    {
        public static LiveHeXVersion[] GetValidVersions(SaveFile sf)
        {
            if (sf is SAV7b) return new[] { LiveHeXVersion.LGPE_v102 };
            if (sf is SAV8SWSH) return new[] { LiveHeXVersion.SWSH_Orion, LiveHeXVersion.SWSH_Rigel1, LiveHeXVersion.SWSH_Rigel2 };
            return new[] { LiveHeXVersion.SWSH_Rigel2 };
        }

        public static ICommunicator GetCommunicator(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => new SysBotMini(),
                LiveHeXVersion.SWSH_Orion => new SysBotMini(),
                LiveHeXVersion.SWSH_Rigel1 => new SysBotMini(),
                LiveHeXVersion.SWSH_Rigel2 => new SysBotMini(),
                _ => new NTRMini()
            };
        }

        public static int GetB1S1Offset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x53586c30,
                LiveHeXVersion.SWSH_Orion => 0x4293D8B0,
                LiveHeXVersion.SWSH_Rigel1 => 0x4506D890,
                _ => 0x4506D890
            };
        }

        public static int GetSlotSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 260,
                LiveHeXVersion.SWSH_Orion => 344,
                LiveHeXVersion.SWSH_Rigel1 => 344,
                _ => 344
            };
        }

        public static int GetGapSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0,
                LiveHeXVersion.SWSH_Orion => 0,
                LiveHeXVersion.SWSH_Rigel1 => 0,
                _ => 0
            };
        }

        public static int GetSlotCount(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 25,
                LiveHeXVersion.SWSH_Orion => 30,
                LiveHeXVersion.SWSH_Rigel1 => 30,
                _ => 30
            };
        }

        public static int GetTrainerBlockSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x168,
                LiveHeXVersion.SWSH_Orion => 0x110,
                LiveHeXVersion.SWSH_Rigel1 => 0x110,
                _ => 0x110
            };
        }

        public static uint GetTrainerBlockOffset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x533211BC,
                LiveHeXVersion.SWSH_Orion => 0x42935e48,
                LiveHeXVersion.SWSH_Rigel1 => 0x45061108,
                _ => 0x45061108
            };
        }
    }
}
