using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public abstract class InjectionBase : PointerCache
    {
        public const decimal BotbaseVersion = 2.3m;

        private const ulong Ovlloader_ID = 0x420000000007e51a;
        private const ulong Dmnt_ID = 0x010000000000000d;

        private const string LetsGoPikachu_ID = "010003F003A34000";
        private const string LetsGoEevee_ID = "0100187003A36000";

        private const string Sword_ID = "0100ABF008968000";
        private const string Shield_ID = "01008DB008C2C000";

        private const string ShiningPearl_ID = "010018E011D92000";
        private const string BrilliantDiamond_ID = "0100000011D90000";

        private const string LegendsArceus_ID = "01001F5010DFA000";

        private const string Scarlet_ID = "0100A3D008C5C000";
        private const string Violet_ID = "01008F6008C5E000";

        private static readonly Dictionary<string, LiveHeXVersion[]> SupportedTitleVersions = new()
        {
            { LetsGoPikachu_ID, new[] { LiveHeXVersion.LGPE_v102 } },
            { LetsGoEevee_ID, new[] { LiveHeXVersion.LGPE_v102 } },

            { Sword_ID, new[] { LiveHeXVersion.SWSH_v111, LiveHeXVersion.SWSH_v121, LiveHeXVersion.SWSH_v132 } },
            { Shield_ID, new[] { LiveHeXVersion.SWSH_v111, LiveHeXVersion.SWSH_v121, LiveHeXVersion.SWSH_v132 } },

            { ShiningPearl_ID, new[] { LiveHeXVersion.SP_v100, LiveHeXVersion.SP_v110, LiveHeXVersion.BDSP_v112, LiveHeXVersion.BDSP_v113, LiveHeXVersion.BDSP_v120, LiveHeXVersion.SP_v130 } },
            { BrilliantDiamond_ID, new[] { LiveHeXVersion.BD_v100, LiveHeXVersion.BD_v110, LiveHeXVersion.BDSP_v112, LiveHeXVersion.BDSP_v113, LiveHeXVersion.BDSP_v120, LiveHeXVersion.BD_v130 } },

            { LegendsArceus_ID, new[] { LiveHeXVersion.LA_v100, LiveHeXVersion.LA_v101, LiveHeXVersion.LA_v102, LiveHeXVersion.LA_v111 } },

            { Scarlet_ID, new[] { LiveHeXVersion.SV_v101, LiveHeXVersion.SV_v110, LiveHeXVersion.SV_v120, LiveHeXVersion.SV_v130, LiveHeXVersion.SV_v131, LiveHeXVersion.SV_v132 } },
            { Violet_ID, new[] { LiveHeXVersion.SV_v101, LiveHeXVersion.SV_v110, LiveHeXVersion.SV_v120, LiveHeXVersion.SV_v130, LiveHeXVersion.SV_v131, LiveHeXVersion.SV_v132 } },
        };

        public virtual Dictionary<string, string> SpecialBlocks { get; } = new();

        public InjectionBase(LiveHeXVersion lv, bool useCache) : base(lv, useCache) { }

        protected static InjectionBase GetInjector(LiveHeXVersion version, bool useCache)
        {
            if (LPLGPE.GetVersions().Contains(version))
                return new LPLGPE(version, useCache);
            if (LPBDSP.GetVersions().Contains(version))
                return new LPBDSP(version, useCache);
            if (LPPointer.GetVersions().Contains(version))
                return new LPPointer(version, useCache);
            if (LPBasic.GetVersions().Contains(version))
                return new LPBasic(version, useCache);
            throw new NotImplementedException("Unknown LiveHeXVersion.");
        }

        public virtual byte[] ReadBox(PokeSysBotMini psb, int box, int len, List<byte[]> allpkm) { return Array.Empty<byte>(); }
        public virtual byte[] ReadSlot(PokeSysBotMini psb, int box, int slot) { return Array.Empty<byte>(); }

        public virtual void SendBox(PokeSysBotMini psb, byte[] boxData, int box) { }
        public virtual void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot) { }

        public virtual void WriteBlocksFromSAV(PokeSysBotMini psb, string block, SaveFile sav) { }

        public virtual void WriteBlockFromString(PokeSysBotMini psb, string block, byte[] data, object sb) { }
        public virtual bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out List<byte[]>? read)
        {
            read = null;
            return false;
        }

        public static bool SaveCompatibleWithTitle(SaveFile sav, string titleID) => sav switch
        {
            SAV9SV when titleID is Scarlet_ID or Violet_ID => true,
            SAV8LA when titleID is LegendsArceus_ID => true,
            SAV8BS when titleID is BrilliantDiamond_ID or ShiningPearl_ID => true,
            SAV8SWSH when titleID is Sword_ID or Shield_ID => true,
            SAV7b when titleID is LetsGoPikachu_ID or LetsGoEevee_ID => true,
            _ => false,
        };

        public static LiveHeXVersion GetVersionFromTitle(string titleID, string gameVersion)
        {
            if (!SupportedTitleVersions.TryGetValue(titleID, out var versions))
                return LiveHeXVersion.Unknown;

            versions = versions.Reverse().ToArray();
            var sanitized = gameVersion.Replace(".", "");
            foreach (var version in versions)
            {
                var name = Enum.GetName(typeof(LiveHeXVersion), version);
                if (name is null)
                    continue;

                name = name.Split('v')[1];
                if (name == sanitized)
                    return version;
            }
            return LiveHeXVersion.Unknown;
        }

        public static bool CheckRAMShift(PokeSysBotMini psb, out string msg)
        {
            msg = "";
            if (psb.com is not ICommunicatorNX nx)
                return false;

            if (nx.IsProgramRunning(Ovlloader_ID))
                msg += "Tesla overlay";

            if (nx.IsProgramRunning(Dmnt_ID))
                msg += msg != "" ? " and dmnt (cheats?)" : "Dmnt (cheats?)";

            bool detected = msg != "";
            msg += detected ? " detected.\n\nPlease remove or close the interfering applications and reboot your Switch." : "";
            return detected;
        }
    }
}
