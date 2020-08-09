﻿using System;
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
        SM_v12,
        ORAS,
        XY,
    }

    public static class RamOffsets
    {
        public static LiveHeXVersion[] GetValidVersions(SaveFile sf)
        {
            if (sf is SAV7b) return new[] { LiveHeXVersion.LGPE_v102 };
            if (sf is SAV8SWSH) return new[] { LiveHeXVersion.SWSH_Orion, LiveHeXVersion.SWSH_Rigel1, LiveHeXVersion.SWSH_Rigel2 };
            if (sf is SAV7USUM) return new[] { LiveHeXVersion.UM_v12, LiveHeXVersion.US_v12 };
            if (sf is SAV7SM) return new[] { LiveHeXVersion.SM_v12 };
            if (sf is SAV6AO) return new[] { LiveHeXVersion.ORAS };
            if (sf is SAV6XY) return new[] { LiveHeXVersion.XY };
            return new[] { LiveHeXVersion.SWSH_Rigel2 };
        }

        public static ICommunicator GetCommunicator(LiveHeXVersion lv, InjectorCommunicationType ict)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => GetSwitchInterface(ict),
                LiveHeXVersion.SWSH_Orion => GetSwitchInterface(ict),
                LiveHeXVersion.SWSH_Rigel1 => GetSwitchInterface(ict),
                LiveHeXVersion.SWSH_Rigel2 => GetSwitchInterface(ict),
                LiveHeXVersion.UM_v12 => new NTRMini(),
                LiveHeXVersion.US_v12 => new NTRMini(),
                LiveHeXVersion.SM_v12 => new NTRMini(),
                LiveHeXVersion.ORAS => new NTRMini(),
                LiveHeXVersion.XY => new NTRMini(),
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
                LiveHeXVersion.SM_v12 => 0x330D9838,
                LiveHeXVersion.ORAS => 0x8C9E134,
                LiveHeXVersion.XY => 0x8C861C8,
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
                LiveHeXVersion.SM_v12 => 232,
                LiveHeXVersion.ORAS => 232,
                LiveHeXVersion.XY => 232,
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
                LiveHeXVersion.SM_v12 => 0,
                LiveHeXVersion.ORAS => 0,
                LiveHeXVersion.XY => 0,
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
                LiveHeXVersion.SM_v12 => 30,
                LiveHeXVersion.ORAS => 30,
                LiveHeXVersion.XY => 30,
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
                LiveHeXVersion.SM_v12 => 0xC0,
                LiveHeXVersion.ORAS => 0x170,
                LiveHeXVersion.XY => 0x170,
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
                LiveHeXVersion.SM_v12 => 0x330D67D0,
                LiveHeXVersion.ORAS => 0x8C81340,
                LiveHeXVersion.XY => 0x8C79C3C,
                _ => 0x45061108
            };
        }

        public static bool WriteBoxData(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.UM_v12 => true,
                LiveHeXVersion.US_v12 => true,
                LiveHeXVersion.SM_v12 => true,
                LiveHeXVersion.ORAS => true,
                LiveHeXVersion.XY => true,
                _ => false
            };
        }

        public static uint NFCValue = 0xE3A01000;

        public static uint NFCOffset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.UM_v12 => 0x3F3428,
                LiveHeXVersion.US_v12 => 0x3F3424,
                LiveHeXVersion.SM_v12 => 0x3E14C0,
                _ => 0
            };
        }

        private static ICommunicator GetSwitchInterface(InjectorCommunicationType ict)
        {
            // No conditional expression possible
            return ict switch
            {
                InjectorCommunicationType.SocketNetwork => new SysBotMini(),
                InjectorCommunicationType.USB => new UsbBotMini(),
                _ => new SysBotMini()
            };
        }
    }
}
