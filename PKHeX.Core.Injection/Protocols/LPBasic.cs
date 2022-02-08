using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PKHeX.Core.Injection
{
    public static class LPBasic
    {
        public static LiveHeXVersion[] SupportedVersions = { LiveHeXVersion.SWSH_Rigel2, LiveHeXVersion.SWSH_Rigel1, LiveHeXVersion.SWSH_Orion,
                                                                                  LiveHeXVersion.LGPE_v102, LiveHeXVersion.ORAS, LiveHeXVersion.XY, LiveHeXVersion.US_v12,
                                                                                  LiveHeXVersion.UM_v12, LiveHeXVersion.SM_v12 };

        public static readonly BlockData[] Blocks_Rigel2 =
        {
            new() { Name = "KMyStatus", Display = "Trainer Data", SCBKey = 0xF25C070E, Offset = 0x45068F18 },
            new() { Name = "KItem", Display = "Items", SCBKey = 0x1177C2C4, Offset = 0x45067A98 },
            new() { Name = "KMisc", Display = "Miscellaneous", SCBKey = 0x1B882B09, Offset = 0x45072DF0 },
            new() { Name = "KTrainerCard", Display = "Trainer Card", SCBKey = 0x874DA6FA, Offset = 0x45127098 },
            new() { Name = "KFashionUnlock", Display = "Fashion", SCBKey = 0xD224F9AC, Offset = 0x450748E8 }
        };

        // LiveHexVersion -> Blockname -> List of <SCBlock Keys, OffsetValues>
        public static readonly Dictionary<LiveHeXVersion, BlockData[]> SCBlocks = new()
        {
            { LiveHeXVersion.SWSH_Rigel2, Blocks_Rigel2 },
        };

        public static readonly Dictionary<string, string> SpecialBlocks = new()
        {
            { "Items", "B_OpenItemPouch_Click" },
            { "Raids", "B_OpenRaids_Click" }
        };

        public static byte[] ReadBox(PokeSysBotMini psb, int box, int len, List<byte[]> allpkm)
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

        public static byte[] ReadSlot(PokeSysBotMini psb, int box, int slot) => psb.com.ReadBytes(psb.GetSlotOffset(box, slot), psb.SlotSize + psb.GapSize);

        public static void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot) => psb.com.WriteBytes(data, psb.GetSlotOffset(box, slot));

        public static void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(psb.SlotSize);
            for (int i = 0; i < psb.SlotCount; i++)
                SendSlot(psb, pkmData[i], box, i);
        }

        public static Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
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
        public static bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out List<byte[]>? read)
        {
            read = null;
            try
            {
                var offsets = SCBlocks[psb.Version].Where(z => z.Display == block);
                var allblocks = sav.GetType().GetProperty("Blocks").GetValue(sav);
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
                    else read.Add(ram);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }

        public static void WriteBlocksFromSAV(PokeSysBotMini psb, string block, SaveFile sav)
        {
            var allblocks = sav.GetType().GetProperty("Blocks").GetValue(sav);
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

