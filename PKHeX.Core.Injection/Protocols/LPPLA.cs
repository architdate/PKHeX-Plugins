using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PKHeX.Core.Injection
{
    public static class LPPLA
    {
        public static readonly LiveHeXVersion[] SupportedVersions = { LiveHeXVersion.LA_v100, LiveHeXVersion.LA_v101, LiveHeXVersion.LA_v102 };

        private const int MYSTATUS_BLOCK_SIZE = 0x80;

        public static readonly BlockData[] Blocks_v100 =
        {
            new() { Name = "MyStatus", Display = "Trainer Data", SCBKey = 0xF25C070E, Pointer = "[[main+4275470]+218]+68" },
            new() { Name = "KMoney", Display = "Money Data", SCBKey = 0x3279D927, Pointer = "[[main+4275470]+210]+6C", Type = SCTypeCode.UInt32 },

            new() { Name = "KItemRegular", Display = "Items", SCBKey = 0x9FE2790A, Pointer = "[[main+4275470]+230]+68" },
            new() { Name = "KItemKey", Display = "Items", SCBKey = 0x59A4D0C3, Pointer = "[[main+4275470]+230]+AF4" },
            new() { Name = "KItemStored", Display = "Items", SCBKey = 0x8E434F0D, Pointer = "[[main+4275470]+1E8]+68" },
            new() { Name = "KItemRecipe", Display = "Items", SCBKey = 0xF5D9F4A5, Pointer = "[[main+4275470]+230]+C84" },
            new() { Name = "KSatchelUpgrades", Display = "Items", SCBKey = 0x75CE2CF6, Pointer = "[[[[[main+4275470]+1D8]+1B8]+70]+270]+38", Type = SCTypeCode.UInt32 },

            new() { Name = "KZukan", Display = "Pokedex", SCBKey = 0x02168706, Pointer = "[[[[main+4275470]+248]+58]+18]+1C" },
        };

        public static readonly BlockData[] Blocks_v101 =
        {
            new() { Name = "MyStatus", Display = "Trainer Data", SCBKey = 0xF25C070E, Pointer = "[[main+427B470]+218]+68" },
            new() { Name = "KMoney", Display = "Money Data", SCBKey = 0x3279D927, Pointer = "[[main+427B470]+210]+6C", Type = SCTypeCode.UInt32 },

            new() { Name = "KItemRegular", Display = "Items", SCBKey = 0x9FE2790A, Pointer = "[[main+427B470]+230]+68" },
            new() { Name = "KItemKey", Display = "Items", SCBKey = 0x59A4D0C3, Pointer = "[[main+427B470]+230]+AF4" },
            new() { Name = "KItemStored", Display = "Items", SCBKey = 0x8E434F0D, Pointer = "[[main+427B470]+1E8]+68" },
            new() { Name = "KItemRecipe", Display = "Items", SCBKey = 0xF5D9F4A5, Pointer = "[[main+427B470]+230]+C84" },
            new() { Name = "KSatchelUpgrades", Display = "Items", SCBKey = 0x75CE2CF6, Pointer = "[[[[[main+427B470]+1D8]+1B8]+70]+270]+38", Type = SCTypeCode.UInt32 },

            new() { Name = "KZukan", Display = "Pokedex", SCBKey = 0x02168706, Pointer = "[[[[main+427B470]+248]+58]+18]+1C" },
        };

        public static readonly BlockData[] Blocks_v102 =
        {
            new() { Name = "MyStatus", Display = "Trainer Data", SCBKey = 0xF25C070E, Pointer = "[[main+427C470]+218]+68" },
            new() { Name = "KMoney", Display = "Money Data", SCBKey = 0x3279D927, Pointer = "[[main+427C470]+210]+6C", Type = SCTypeCode.UInt32 },

            new() { Name = "KItemRegular", Display = "Items", SCBKey = 0x9FE2790A, Pointer = "[[main+427C470]+230]+68" },
            new() { Name = "KItemKey", Display = "Items", SCBKey = 0x59A4D0C3, Pointer = "[[main+427C470]+230]+AF4" },
            new() { Name = "KItemStored", Display = "Items", SCBKey = 0x8E434F0D, Pointer = "[[main+427C470]+1E8]+68" },
            new() { Name = "KItemRecipe", Display = "Items", SCBKey = 0xF5D9F4A5, Pointer = "[[main+427C470]+230]+C84" },
            new() { Name = "KSatchelUpgrades", Display = "Items", SCBKey = 0x75CE2CF6, Pointer = "[[[[[main+427C470]+1D8]+1B8]+70]+270]+38", Type = SCTypeCode.UInt32 },

            new() { Name = "KZukan", Display = "Pokedex", SCBKey = 0x02168706, Pointer = "[[[[main+427C470]+248]+58]+18]+1C" },
        };

        // LiveHexVersion -> Blockname -> List of <SCBlock Keys, OffsetValues>
        public static readonly Dictionary<LiveHeXVersion, BlockData[]> SCBlocks = new()
        {
            { LiveHeXVersion.LA_v100, Blocks_v100 },
            { LiveHeXVersion.LA_v101, Blocks_v101 },
            { LiveHeXVersion.LA_v102, Blocks_v102 },
        };

        public static readonly Dictionary<string, string> SpecialBlocks = new()
        {
            { "Items", "B_OpenItemPouch_Click" },
            { "Pokedex", "B_OpenPokedex_Click" },
            //{ "Trainer Data", "B_OpenTrainerInfo_Click" },
        };

        private static string GetB1S1Pointer(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.LA_v100 => "[[main+4275470]+1F0]+68",
                LiveHeXVersion.LA_v101 => "[[main+427B470]+1F0]+68",
                LiveHeXVersion.LA_v102 => "[[main+427C470]+1F0]+68",
                _ => string.Empty
            };
        }

        public static byte[] ReadBox(PokeSysBotMini psb, int box, List<byte[]> allpkm)
        {
            if (psb.com is not ICommunicatorNX sb)
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            var lv = psb.Version;
            var b1s1 = sb.GetPointerAddress(GetB1S1Pointer(lv));
            var boxsize = RamOffsets.GetSlotCount(lv) * RamOffsets.GetSlotSize(lv);
            var boxstart = b1s1 + (ulong)(box * boxsize);
            return psb.com.ReadBytes(boxstart, boxsize);
        }

        public static byte[] ReadSlot(PokeSysBotMini psb, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb)
                return new byte[psb.SlotSize];
            var lv = psb.Version;
            var slotsize = RamOffsets.GetSlotSize(lv);
            var b1s1 = sb.GetPointerAddress(GetB1S1Pointer(lv));
            var boxsize = RamOffsets.GetSlotCount(lv) * slotsize;
            var boxstart = b1s1 + (ulong)(box * boxsize);
            var slotstart = boxstart + (ulong)(slot * slotsize);
            return psb.com.ReadBytes(slotstart, slotsize);
        }

        public static void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb)
                return;
            var lv = psb.Version;
            var slotsize = RamOffsets.GetSlotSize(lv);
            var b1s1 = sb.GetPointerAddress(GetB1S1Pointer(lv));
            var boxsize = RamOffsets.GetSlotCount(lv) * slotsize;
            var boxstart = b1s1 + (ulong)(box * boxsize);
            var slotstart = boxstart + (ulong)(slot * slotsize);
            psb.com.WriteBytes(data, slotstart);
        }

        public static void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            if (psb.com is not ICommunicatorNX sb)
                return;
            var lv = psb.Version;
            var b1s1 = sb.GetPointerAddress(GetB1S1Pointer(lv));
            var boxsize = RamOffsets.GetSlotCount(lv) * RamOffsets.GetSlotSize(lv);
            var boxstart = b1s1 + (ulong)(box * boxsize);
            psb.com.WriteBytes(boxData, boxstart);
        }

        public static bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out List<byte[]>? read)
        {
            read = null;
            if (psb.com is not ICommunicatorNX sb)
                return false;
            try
            {
                var offsets = SCBlocks[psb.Version].Where(z => z.Display == block);
                var allblocks = sav.GetType().GetProperty("Blocks").GetValue(sav);
                if (allblocks is not SCBlockAccessor scba)
                    return false;
                foreach (var sub in offsets)
                {
                    var scbkey = sub.SCBKey;
                    var offset = sub.Pointer;
                    var scb = scba.GetBlock(scbkey);
                    if (scb.Type == SCTypeCode.None && sub.Type != SCTypeCode.None)
                        ReflectUtil.SetValue(scb, "Type", sub.Type);
                    var ram = psb.com.ReadBytes(sb.GetPointerAddress(offset), scb.Data.Length);
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
            if (psb.com is not ICommunicatorNX sb)
                return;
            var allblocks = sav.GetType().GetProperty("Blocks").GetValue(sav);
            if (allblocks is not SCBlockAccessor scba)
                return;
            var offsets = SCBlocks[psb.Version].Where(z => z.Display == block);
            foreach (var sub in offsets)
            {
                var scbkey = sub.SCBKey;
                var offset = sub.Pointer;
                var scb = scba.GetBlock(scbkey);
                psb.com.WriteBytes(scb.Data, sb.GetPointerAddress(offset));
            }
        }

        public static Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
        {
            if (psb.com is not ICommunicatorNX sb)
                return null;
            var lv = psb.Version;
            var ptr = SCBlocks[lv].First(z => z.Name == "MyStatus").Pointer;
            var ofs = sb.GetPointerAddress(ptr);
            var size = MYSTATUS_BLOCK_SIZE;
            if (size <= 0 || ofs == 0)
                return null;
            var data = psb.com.ReadBytes(ofs, size);
            return data;
        };
    }
}
