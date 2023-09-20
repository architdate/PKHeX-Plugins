using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public class LPBasic : InjectionBase
    {
        private static readonly LiveHeXVersion[] SupportedVersions = { LiveHeXVersion.SWSH_v132, LiveHeXVersion.SWSH_v121, LiveHeXVersion.SWSH_v111,
                                                                       LiveHeXVersion.LGPE_v102, LiveHeXVersion.ORAS_v140, LiveHeXVersion.XY_v150,
                                                                       LiveHeXVersion.US_v120, LiveHeXVersion.UM_v120, LiveHeXVersion.SM_v120 };

        public static LiveHeXVersion[] GetVersions() => SupportedVersions;

        public static readonly BlockData[] Blocks_Rigel2 =
        {
            new() { Name = "KMyStatus", Display = "Trainer Data", SCBKey = 0xF25C070E, Offset = 0x45068F18 },
            new() { Name = "KItem", Display = "Items", SCBKey = 0x1177C2C4, Offset = 0x45067A98 },
            new() { Name = "KMisc", Display = "Miscellaneous", SCBKey = 0x1B882B09, Offset = 0x45072DF0 },
            new() { Name = "KTrainerCard", Display = "Trainer Card", SCBKey = 0x874DA6FA, Offset = 0x45127098 },
            new() { Name = "KFashionUnlock", Display = "Fashion", SCBKey = 0xD224F9AC, Offset = 0x450748E8 },
            new() { Name = "KRaidSpawnList", Display = "Raid", SCBKey = 0x9033eb7b, Offset = 0x450C8A70 },
            new() { Name = "KRaidSpawnListR1", Display = "RaidArmor", SCBKey = 0x158DA896, Offset = 0x450C94D8 },
            new() { Name = "KRaidSpawnListR2", Display = "RaidCrown", SCBKey = 0x148DA703, Offset = 0x450C9F40 },
            new() { Name = "KZukan", Display = "Pokedex Base", SCBKey = 0x4716C404, Offset = 0x45069120 },
            new() { Name = "KZukanR1", Display = "Pokedex Armor", SCBKey = 0x3F936BA9, Offset = 0x45069120 },
            new() { Name = "KZukanR2", Display = "Pokedex Crown", SCBKey = 0x3C9366F0, Offset = 0x45069120 },
        };

        // LiveHexVersion -> Blockname -> List of <SCBlock Keys, OffsetValues>
        public static readonly Dictionary<LiveHeXVersion, BlockData[]> SCBlocks = new()
        {
            { LiveHeXVersion.SWSH_v132, Blocks_Rigel2 },
        };

        public override Dictionary<string, string> SpecialBlocks { get; } = new()
        {
            { "Items", "B_OpenItemPouch_Click" },
            { "Raid", "B_OpenRaids_Click" },
            { "RaidArmor", "B_OpenRaids_Click" },
            { "RaidCrown", "B_OpenRaids_Click" },
            { "Pokedex Base", "B_OpenPokedex_Click" },
            { "Pokedex Armor", "B_OpenPokedex_Click" },
            { "Pokedex Crown", "B_OpenPokedex_Click" },
        };

        public LPBasic(LiveHeXVersion lv, bool useCache) : base(lv, useCache) { }

        public override byte[] ReadBox(PokeSysBotMini psb, int box, int len, List<byte[]> allpkm)
        {
            var bytes = psb.com.ReadBytes(psb.GetBoxOffset(box), len);
            if (psb.GapSize == 0)
                return bytes;
            var currofs = 0;
            for (int i = 0; i < psb.SlotCount; i++)
            {
                var stored = bytes.Slice(currofs, psb.SlotSize);
                allpkm.Add(stored);
                currofs += psb.SlotSize + psb.GapSize;
            }

            return ArrayUtil.ConcatAll(allpkm.ToArray());
        }

        public override byte[] ReadSlot(PokeSysBotMini psb, int box, int slot) => psb.com.ReadBytes(psb.GetSlotOffset(box, slot), psb.SlotSize + psb.GapSize);
        public override void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot) => psb.com.WriteBytes(data, psb.GetSlotOffset(box, slot));

        public override void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(psb.SlotSize);
            for (int i = 0; i < psb.SlotCount; i++)
                SendSlot(psb, pkmData[i], box, i);
        }

        public static readonly Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
        {
            var lv = psb.Version;
            var ofs = RamOffsets.GetTrainerBlockOffset(lv);
            var size = RamOffsets.GetTrainerBlockSize(lv);
            if (size <= 0 || ofs == 0)
                return null;
            var data = psb.com.ReadBytes(ofs, size);
            return data;
        };

        // Reflection method
        public override bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out List<byte[]>? read)
        {
            read = null;
            try
            {
                var offsets = SCBlocks[psb.Version].Where(z => z.Display == block);
                var props = sav.GetType().GetProperty("Blocks") ?? throw new Exception("Blocks don't exist");
                var allblocks = props.GetValue(sav);
                if (allblocks is not SCBlockAccessor scba)
                    return false;
                foreach (var sub in offsets)
                {
                    var scbkey = sub.SCBKey;
                    var offset = sub.Offset;
                    var scb = scba.GetBlock(scbkey);
                    var ram = psb.com.ReadBytes(offset, scb.Data.Length);
                    ram.CopyTo(scb.Data, 0);
                    if (read == null)
                    {
                        read = new List<byte[]> { ram };
                    }
                    else
                    {
                        read.Add(ram);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }

        public override void WriteBlocksFromSAV(PokeSysBotMini psb, string block, SaveFile sav)
        {
            var props = sav.GetType().GetProperty("Blocks") ?? throw new Exception("Blocks don't exist");
            var allblocks = props.GetValue(sav);
            if (allblocks is not SCBlockAccessor scba)
                return;
            var offsets = SCBlocks[psb.Version].Where(z => z.Display == block);
            foreach (var sub in offsets)
            {
                var scbkey = sub.SCBKey;
                var offset = sub.Offset;
                var scb = scba.GetBlock(scbkey);
                psb.com.WriteBytes(scb.Data, offset);
            }
        }
    }
}
