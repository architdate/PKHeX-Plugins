namespace PKHeX.Core.Injection
{
    public static class RamOffsets
    {
        public static LiveHeXVersion[] GetValidVersions(SaveFile sf)
        {
            return sf switch
            {
                SAV9SV => new[] { LiveHeXVersion.SV_v101, LiveHeXVersion.SV_v110, LiveHeXVersion.SV_v120, LiveHeXVersion.SV_v130, LiveHeXVersion.SV_v131, LiveHeXVersion.SV_v132 },
                SAV8LA => new[] { LiveHeXVersion.LA_v100 , LiveHeXVersion.LA_v101, LiveHeXVersion.LA_v102, LiveHeXVersion.LA_v111 },
                SAV8BS => new[] { LiveHeXVersion.BD_v100, LiveHeXVersion.SP_v100, LiveHeXVersion.BD_v110, LiveHeXVersion.SP_v110, LiveHeXVersion.BD_v111, LiveHeXVersion.SP_v111, LiveHeXVersion.BDSP_v112, LiveHeXVersion.BDSP_v113, LiveHeXVersion.BDSP_v120, LiveHeXVersion.BD_v130, LiveHeXVersion.SP_v130 },
                SAV8SWSH => new[] { LiveHeXVersion.SWSH_v111, LiveHeXVersion.SWSH_v121, LiveHeXVersion.SWSH_v132 },
                SAV7b => new[] { LiveHeXVersion.LGPE_v102 },
                SAV7USUM => new[] { LiveHeXVersion.UM_v120, LiveHeXVersion.US_v120 },
                SAV7SM => new[] { LiveHeXVersion.SM_v120 },
                SAV6AO => new[] { LiveHeXVersion.ORAS_v140 },
                SAV6XY => new[] { LiveHeXVersion.XY_v150 },
                _ => new[] { LiveHeXVersion.SWSH_v132 },
            };
        }

        public static bool IsLiveHeXSupported(SaveFile sav) => sav switch
        {
            SAV9SV => true,
            SAV8LA => true,
            SAV8BS => true,
            SAV8SWSH => true,
            SAV7b => true,
            SAV7USUM => true,
            SAV7SM => true,
            SAV6AO => true,
            SAV6XY => true,
            _ => false,
        };

        public static bool IsSwitchTitle(SaveFile sav) => sav switch
        {
            SAV7USUM => false,
            SAV7SM => false,
            SAV6AO => false,
            SAV6XY => false,
            _ => true,
        };

        public static ICommunicator GetCommunicator(SaveFile sav, InjectorCommunicationType ict)
        {
            bool isSwitch = IsSwitchTitle(sav);
            if (isSwitch)
                return GetSwitchInterface(ict);
            return new NTRClient();
        }

        public static int GetB1S1Offset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x533675B0,
                LiveHeXVersion.SWSH_v111 => 0x4293D8B0,
                LiveHeXVersion.SWSH_v121 => 0x4506D890,
                LiveHeXVersion.SWSH_v132 => 0x45075880,
                LiveHeXVersion.UM_v120 => 0x33015AB0,
                LiveHeXVersion.US_v120 => 0x33015AB0,
                LiveHeXVersion.SM_v120 => 0x330D9838,
                LiveHeXVersion.ORAS_v140 => 0x8C9E134,
                LiveHeXVersion.XY_v150 => 0x8C861C8,
                _ => 0x0,
            };
        }

        public static int GetSlotSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LA_v111 => 360,
                LiveHeXVersion.LA_v102 => 360,
                LiveHeXVersion.LA_v101 => 360,
                LiveHeXVersion.LA_v100 => 360,
                LiveHeXVersion.LGPE_v102 => 260,
                LiveHeXVersion.UM_v120 => 232,
                LiveHeXVersion.US_v120 => 232,
                LiveHeXVersion.SM_v120 => 232,
                LiveHeXVersion.ORAS_v140 => 232,
                LiveHeXVersion.XY_v150 => 232,
                _ => 344,
            };
        }

        public static int GetGapSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 380,
                _ => 0,
            };
        }

        public static int GetSlotCount(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 25,
                _ => 30,
            };
        }

        public static int GetTrainerBlockSize(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130 => 0x50,
                LiveHeXVersion.SP_v130 => 0x50,
                LiveHeXVersion.BDSP_v120 => 0x50,
                LiveHeXVersion.BDSP_v113 => 0x50,
                LiveHeXVersion.BDSP_v112 => 0x50,
                LiveHeXVersion.BD_v111 => 0x50,
                LiveHeXVersion.BD_v110 => 0x50,
                LiveHeXVersion.BD_v100 => 0x50,
                LiveHeXVersion.SP_v111 => 0x50,
                LiveHeXVersion.SP_v110 => 0x50,
                LiveHeXVersion.SP_v100 => 0x50,
                LiveHeXVersion.LGPE_v102 => 0x168,
                LiveHeXVersion.SWSH_v111 => 0x110,
                LiveHeXVersion.SWSH_v121 => 0x110,
                LiveHeXVersion.SWSH_v132 => 0x110,
                LiveHeXVersion.UM_v120 => 0xC0,
                LiveHeXVersion.US_v120 => 0xC0,
                LiveHeXVersion.SM_v120 => 0xC0,
                LiveHeXVersion.ORAS_v140 => 0x170,
                LiveHeXVersion.XY_v150 => 0x170,
                _ => 0x110,
            };
        }

        public static uint GetTrainerBlockOffset(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LGPE_v102 => 0x53582030,
                LiveHeXVersion.SWSH_v111 => 0x42935E48,
                LiveHeXVersion.SWSH_v121 => 0x45061108,
                LiveHeXVersion.SWSH_v132 => 0x45068F18,
                LiveHeXVersion.UM_v120 => 0x33012818,
                LiveHeXVersion.US_v120 => 0x33012818,
                LiveHeXVersion.SM_v120 => 0x330D67D0,
                LiveHeXVersion.ORAS_v140 => 0x8C81340,
                LiveHeXVersion.XY_v150 => 0x8C79C3C,
                _ => 0x0,
            };
        }

        public static bool WriteBoxData(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LA_v111 => true,
                LiveHeXVersion.LA_v102 => true,
                LiveHeXVersion.LA_v101 => true,
                LiveHeXVersion.LA_v100 => true,
                LiveHeXVersion.UM_v120 => true,
                LiveHeXVersion.US_v120 => true,
                LiveHeXVersion.SM_v120 => true,
                LiveHeXVersion.ORAS_v140 => true,
                LiveHeXVersion.XY_v150 => true,
                _ => false,
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        public static (string, int) BoxOffsets(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130   => ("[[[[main+4C64DC0]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.SP_v130   => ("[[[[main+4E7BE98]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.BDSP_v120 => ("[[[[main+4E36C58]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.BDSP_v113 => ("[[[[main+4E59E60]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.BDSP_v112 => ("[[[[main+4E34DD0]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.BD_v111   => ("[[[[main+4C1DCF8]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.BD_v110   => ("[[[main+4E27C50]+B8]+170]+20", 40),
                LiveHeXVersion.BD_v100   => ("[[[main+4C0ABD8]+520]+C0]+5E0", 40),
                LiveHeXVersion.SP_v111   => ("[[[[main+4E34DD0]+B8]+10]+A0]+20", 40),
                LiveHeXVersion.SP_v110   => ("[[[main+4E27C50]+B8]+170]+20", 40),      // untested
                LiveHeXVersion.SP_v100   => ("[[[main+4C0ABD8]+520]+C0]+5E0", 40),     // untested
                _ => (string.Empty, 0)
            };
        }

        public static object? GetOffsets(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.SWSH_v132 => Offsets8.Rigel2,
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
