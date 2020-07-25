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
        SWSH_Rigel2,
        UM_v12,
        US_v12,
        MN_v12,
        SN_v12,
        AS,
        OR,
        X,
        Y
    }

    public static class RamOffsets
    {
        public static LiveHeXVersion[] GetValidVersions(SaveFile sf)
        {
            if (sf is SAV7b) return new[] { LiveHeXVersion.LGPE_v102 };
            if (sf is SAV8SWSH) return new[] { LiveHeXVersion.SWSH_Orion, LiveHeXVersion.SWSH_Rigel1, LiveHeXVersion.SWSH_Rigel2 };
            if (sf is SAV7USUM) return new[] { LiveHeXVersion.UM_v12, LiveHeXVersion.US_v12 };
            if (sf is SAV7SM) return new[] { LiveHeXVersion.MN_v12, LiveHeXVersion.SN_v12 };
            if (sf is SAV6AO) return new[] { LiveHeXVersion.OR, LiveHeXVersion.AS };
            if (sf is SAV6XY) return new[] { LiveHeXVersion.X, LiveHeXVersion.Y };
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
                LiveHeXVersion.UM_v12 => new NTRMini(),
                LiveHeXVersion.US_v12 => new NTRMini(),
                LiveHeXVersion.MN_v12 => new NTRMini(),
                LiveHeXVersion.SN_v12 => new NTRMini(),
                LiveHeXVersion.AS => new NTRMini(),
                LiveHeXVersion.OR => new NTRMini(),
                LiveHeXVersion.X => new NTRMini(),
                LiveHeXVersion.Y => new NTRMini(),
                _ => new SysBotMini()
            };
        }

        public static int GetB1S1Offset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x53586c30,
                LiveHeXVersion.SWSH_Orion => 0x4293D8B0,
                LiveHeXVersion.SWSH_Rigel1 => 0x4506D890,
                LiveHeXVersion.UM_v12 => 0x33015AB0,
                LiveHeXVersion.US_v12 => 0x33015AB0,
                LiveHeXVersion.MN_v12 => 0x330D9838,
                LiveHeXVersion.SN_v12 => 0x330D9838,
                LiveHeXVersion.AS => 0x8C9E134,
                LiveHeXVersion.OR => 0x8C9E134,
                LiveHeXVersion.X => 0x8C861C8,
                LiveHeXVersion.Y => 0x8C861C8,
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
                LiveHeXVersion.UM_v12 => 232,
                LiveHeXVersion.US_v12 => 232,
                LiveHeXVersion.MN_v12 => 232,
                LiveHeXVersion.SN_v12 => 232,
                LiveHeXVersion.AS => 232,
                LiveHeXVersion.OR => 232,
                LiveHeXVersion.X => 232,
                LiveHeXVersion.Y => 232,
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
                LiveHeXVersion.UM_v12 => 0,
                LiveHeXVersion.US_v12 => 0,
                LiveHeXVersion.MN_v12 => 0,
                LiveHeXVersion.SN_v12 => 0,
                LiveHeXVersion.AS => 0,
                LiveHeXVersion.OR => 0,
                LiveHeXVersion.X => 0,
                LiveHeXVersion.Y => 0,
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
                LiveHeXVersion.UM_v12 => 30,
                LiveHeXVersion.US_v12 => 30,
                LiveHeXVersion.MN_v12 => 30,
                LiveHeXVersion.SN_v12 => 30,
                LiveHeXVersion.AS => 30,
                LiveHeXVersion.OR => 30,
                LiveHeXVersion.X => 30,
                LiveHeXVersion.Y => 30,
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
                LiveHeXVersion.UM_v12 => 0xC0,
                LiveHeXVersion.US_v12 => 0xC0,
                LiveHeXVersion.MN_v12 => 0xC0,
                LiveHeXVersion.SN_v12 => 0xC0,
                LiveHeXVersion.AS => 0x170,
                LiveHeXVersion.OR => 0x170,
                LiveHeXVersion.X => 0x170,
                LiveHeXVersion.Y => 0x170,
                _ => 0x110
            };
        }

        public static uint GetTrainerBlockOffset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x533211BC,
                LiveHeXVersion.SWSH_Orion => 0x42935E48,
                LiveHeXVersion.SWSH_Rigel1 => 0x45061108,
                LiveHeXVersion.UM_v12 => 0x33012818,
                LiveHeXVersion.US_v12 => 0x33012818,
                LiveHeXVersion.MN_v12 => 0x330D67D0,
                LiveHeXVersion.SN_v12 => 0x330D67D0,
                LiveHeXVersion.AS => 0x8C81340,
                LiveHeXVersion.OR => 0x8C81340,
                LiveHeXVersion.X => 0x8C79C3C,
                LiveHeXVersion.Y => 0x8C79C3C,
                _ => 0x45061108
            };
        }
    }
}
