namespace PKHeX.Core.Injection
{
    public static class RamOffsets
    {
        public static LiveHeXVersion[] GetValidVersions(SaveFile sf)
        {
            return sf switch
            {
                SAV8SWSH => new[] { LiveHeXVersion.SWSH_Orion, LiveHeXVersion.SWSH_Rigel1, LiveHeXVersion.SWSH_Rigel2, LiveHeXVersion.BDSP },
                SAV7b => new[] { LiveHeXVersion.LGPE_v102 },
                SAV7USUM => new[] { LiveHeXVersion.UM_v12, LiveHeXVersion.US_v12 },
                SAV7SM => new[] { LiveHeXVersion.SM_v12 },
                SAV6AO => new[] { LiveHeXVersion.ORAS },
                SAV6XY => new[] { LiveHeXVersion.XY },
                _ => new[] { LiveHeXVersion.SWSH_Rigel2 },
            };
        }

        public static bool IsLiveHeXSupported(SaveFile sav)
        {
            return sav switch
            {
                SAV8SWSH => true,
                SAV7b => true,
                SAV7USUM => true,
                SAV7SM => true,
                SAV6AO => true,
                SAV6XY => true,
                _ => false,
            };
        }

        public static ICommunicator GetCommunicator(LiveHeXVersion lv, InjectorCommunicationType ict)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => GetSwitchInterface(ict),
                LiveHeXVersion.LGPE_v102 => GetSwitchInterface(ict),
                LiveHeXVersion.SWSH_Orion => GetSwitchInterface(ict),
                LiveHeXVersion.SWSH_Rigel1 => GetSwitchInterface(ict),
                LiveHeXVersion.SWSH_Rigel2 => GetSwitchInterface(ict),
                LiveHeXVersion.UM_v12 => new NTRClient(),
                LiveHeXVersion.US_v12 => new NTRClient(),
                LiveHeXVersion.SM_v12 => new NTRClient(),
                LiveHeXVersion.ORAS => new NTRClient(),
                LiveHeXVersion.XY => new NTRClient(),
                _ => new SysBotMini(),
            };
        }

        public static int GetB1S1Offset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => 0x0,
                LiveHeXVersion.LGPE_v102 => 0x533675B0,
                LiveHeXVersion.SWSH_Orion => 0x4293D8B0,
                LiveHeXVersion.SWSH_Rigel1 => 0x4506D890,
                LiveHeXVersion.SWSH_Rigel2 => 0x45075880,
                LiveHeXVersion.UM_v12 => 0x33015AB0,
                LiveHeXVersion.US_v12 => 0x33015AB0,
                LiveHeXVersion.SM_v12 => 0x330D9838,
                LiveHeXVersion.ORAS => 0x8C9E134,
                LiveHeXVersion.XY => 0x8C861C8,
                _ => 0x4506D890,
            };
        }

        public static int GetSlotSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => 344,
                LiveHeXVersion.LGPE_v102 => 260,
                LiveHeXVersion.SWSH_Orion => 344,
                LiveHeXVersion.SWSH_Rigel1 => 344,
                LiveHeXVersion.SWSH_Rigel2 => 344,
                LiveHeXVersion.UM_v12 => 232,
                LiveHeXVersion.US_v12 => 232,
                LiveHeXVersion.SM_v12 => 232,
                LiveHeXVersion.ORAS => 232,
                LiveHeXVersion.XY => 232,
                _ => 344,
            };
        }

        public static int GetGapSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => 40,
                LiveHeXVersion.LGPE_v102 => 380,
                LiveHeXVersion.SWSH_Orion => 0,
                LiveHeXVersion.SWSH_Rigel1 => 0,
                LiveHeXVersion.SWSH_Rigel2 => 0,
                LiveHeXVersion.UM_v12 => 0,
                LiveHeXVersion.US_v12 => 0,
                LiveHeXVersion.SM_v12 => 0,
                LiveHeXVersion.ORAS => 0,
                LiveHeXVersion.XY => 0,
                _ => 0,
            };
        }

        public static int GetSlotCount(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => 30,
                LiveHeXVersion.LGPE_v102 => 25,
                LiveHeXVersion.SWSH_Orion => 30,
                LiveHeXVersion.SWSH_Rigel1 => 30,
                LiveHeXVersion.SWSH_Rigel2 => 30,
                LiveHeXVersion.UM_v12 => 30,
                LiveHeXVersion.US_v12 => 30,
                LiveHeXVersion.SM_v12 => 30,
                LiveHeXVersion.ORAS => 30,
                LiveHeXVersion.XY => 30,
                _ => 30,
            };
        }

        public static int GetTrainerBlockSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => 0x0,
                LiveHeXVersion.LGPE_v102 => 0x168,
                LiveHeXVersion.SWSH_Orion => 0x110,
                LiveHeXVersion.SWSH_Rigel1 => 0x110,
                LiveHeXVersion.SWSH_Rigel2 => 0x110,
                LiveHeXVersion.UM_v12 => 0xC0,
                LiveHeXVersion.US_v12 => 0xC0,
                LiveHeXVersion.SM_v12 => 0xC0,
                LiveHeXVersion.ORAS => 0x170,
                LiveHeXVersion.XY => 0x170,
                _ => 0x110,
            };
        }

        public static uint GetTrainerBlockOffset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => 0x0,
                LiveHeXVersion.LGPE_v102 => 0x53582030,
                LiveHeXVersion.SWSH_Orion => 0x42935E48,
                LiveHeXVersion.SWSH_Rigel1 => 0x45061108,
                LiveHeXVersion.SWSH_Rigel2 => 0x45068F18,
                LiveHeXVersion.UM_v12 => 0x33012818,
                LiveHeXVersion.US_v12 => 0x33012818,
                LiveHeXVersion.SM_v12 => 0x330D67D0,
                LiveHeXVersion.ORAS => 0x8C81340,
                LiveHeXVersion.XY => 0x8C79C3C,
                _ => 0x45061108,
            };
        }

        public static bool WriteBoxData(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => false,
                LiveHeXVersion.UM_v12 => true,
                LiveHeXVersion.US_v12 => true,
                LiveHeXVersion.SM_v12 => true,
                LiveHeXVersion.ORAS => true,
                LiveHeXVersion.XY => true,
                _ => false,
            };
        }

        public static (string, int) BoxOffsets(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BDSP => ("[[[main+4E27C50]+B8]+170]+20", 40),
                _ => (string.Empty, 0)
            };
        }

        public static object? GetOffsets(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_Rigel2 => Offsets8.Rigel2,
                _ => null,
            };
        }

        private static ICommunicator GetSwitchInterface(InjectorCommunicationType ict)
        {
            // No conditional expression possible
            return ict switch
            {
                InjectorCommunicationType.SocketNetwork => new SysBotMini(),
                InjectorCommunicationType.USB => new UsbBotMini(),
                _ => new SysBotMini(),
            };
        }
    }
}
