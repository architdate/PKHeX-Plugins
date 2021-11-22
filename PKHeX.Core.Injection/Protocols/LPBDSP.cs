using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PKHeX.Core.Injection
{
    public static class LPBDSP
    {
        public static LiveHeXVersion[] BrilliantDiamond = new LiveHeXVersion[] { LiveHeXVersion.BD_v100, LiveHeXVersion.BD_v110, LiveHeXVersion.BD_v111 };
        public static LiveHeXVersion[] ShiningPearl     = new LiveHeXVersion[] { LiveHeXVersion.SP_v100, LiveHeXVersion.SP_v110, LiveHeXVersion.SP_v111 };
        public static LiveHeXVersion[] SupportedVersions = ArrayUtil.ConcatAll(BrilliantDiamond, ShiningPearl);

        private const int ITEM_BLOCK_SIZE = 0xBB80;
        private const int ITEM_BLOCK_SIZE_RAM = (0xBB80 / 0x10) * 0xC;

#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        public static Dictionary<string, (Func<PokeSysBotMini, byte[]?>, Action<PokeSysBotMini, byte[]>)> FunctionMap = new ()
        {
            { "Items",      (GetItemBlock, SetItemBlock) },
            { "MyStatus",   (GetMyStatusBlock, SetMyStatusBlock) },
        };
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        private static ulong[] GetPokemonPointers(this PokeSysBotMini psb, int box)
        {
            var sb = (ICommunicatorNX)psb.com;
            (string ptr, int count) boxes = RamOffsets.BoxOffsets(psb.Version);
            var addr = InjectionUtil.GetPointerAddress(sb, boxes.ptr);
            var b = psb.com.ReadBytes(addr, boxes.count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);
            var pkmptrs = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray();
            return pkmptrs;
        }

        private static string? GetTrainerPointer(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v111 => "[[main+4C12B78]+B8]+1B0",
                LiveHeXVersion.SP_v111 => "[[main+4E29C50]+B8]+1B0",
                _ => null
            };
        }

        private static string? GetItemPointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v111 => "[[[main+4C12B78]+B8]+118]+20",
                LiveHeXVersion.SP_v111 => "[[[main+4E29C50]+B8]+118]+20",
                _ => null
            };
        }

        public static byte[] ReadBox(PokeSysBotMini psb, int box, List<byte[]> allpkm)
        {
            var use_legacy = false;
            if (psb.com is not ICommunicatorNX sb)
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            var pkmptrs = psb.GetPokemonPointers(box);
            if (use_legacy)
            {
                foreach (var p in pkmptrs)
                    allpkm.Add(sb.ReadBytesAbsolute(p + 0x20, psb.SlotSize));
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            }
            else
            {
                Dictionary<ulong, int> offsets = new();
                foreach (var p in pkmptrs)
                    offsets.Add(p + 0x20, psb.SlotSize);
                return sb.ReadBytesAbsoluteMulti(offsets);
            }
        }

        public static byte[] ReadSlot(PokeSysBotMini psb, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb)
                return new byte[psb.SlotSize];
            var pkmptr = psb.GetPokemonPointers(box)[slot];
            return sb.ReadBytesAbsolute(pkmptr + 0x20, psb.SlotSize);
        }

        public static void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb) 
                return;
            var pkmptr = psb.GetPokemonPointers(box)[slot];
            sb.WriteBytesAbsolute(data, pkmptr + 0x20);
        }

        public static void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            if (psb.com is not ICommunicatorNX sb)
                return;
            byte[][] pkmData = boxData.Split(psb.SlotSize);
            var pkmptrs = psb.GetPokemonPointers(box);
            for (int i = 0; i < psb.SlotCount; i++)
                sb.WriteBytesAbsolute(pkmData[i], pkmptrs[i] + 0x20);
        }

        public static Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
        {
            var lv = psb.Version;
            var ptr = GetTrainerPointer(lv);
            if (ptr == null || psb.com is not ICommunicatorNX sb)
                return null;
            var size = RamOffsets.GetTrainerBlockSize(lv);
            var retval = new byte[size];
            var trainer_name = ptr + "]+14";
            var trainer_name_addr = InjectionUtil.GetPointerAddress(sb, trainer_name);
            psb.com.ReadBytes(trainer_name_addr, 0x1A).CopyTo(retval);
            psb.com.ReadBytes(InjectionUtil.GetPointerAddress(sb, ptr) + 0x8, size - 0x1A - 0x2).CopyTo(retval, 0x1A + 0x2);

            // manually set ROM Code to avoid throwing exceptions (bad ram possibly)
            retval[0x2B] = BrilliantDiamond.Contains(lv) ? (byte)0 : (byte)1;

            return retval;
        };

        public static byte[]? GetItemBlock(PokeSysBotMini psb)
        {
            var ptr = GetItemPointers(psb.Version);
            if (ptr == null)
                return null;
            var nx = (ICommunicatorNX)psb.com;
            var addr = InjectionUtil.GetPointerAddress(nx, ptr);
            var item_blk = psb.com.ReadBytes(addr, ITEM_BLOCK_SIZE_RAM);
            var extra_data = new byte[] { 0x0, 0x0, 0xFF, 0xFF };
            var items = Core.ArrayUtil.EnumerateSplit(item_blk, 0xC).Select(z => z.Concat(extra_data).ToArray()).ToArray();
            return ArrayUtil.ConcatAll(items);
        }

        public static void SetItemBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetItemPointers(psb.Version);
            if (ptr == null)
                return;
            var nx = (ICommunicatorNX)psb.com;
            var addr = InjectionUtil.GetPointerAddress(nx, ptr);
            data = data.Slice(0, ITEM_BLOCK_SIZE);
            var items = Core.ArrayUtil.EnumerateSplit(data, 0x10).Select(z => z.Slice(0, 0xC)).ToArray();
            var payload = ArrayUtil.ConcatAll(items);
            psb.com.WriteBytes(payload, addr);
        }

        public static byte[]? GetMyStatusBlock(PokeSysBotMini psb) => GetTrainerData(psb);

        public static void SetMyStatusBlock(PokeSysBotMini psb, byte[] data)
        {
            var lv = psb.Version;
            var ptr = GetTrainerPointer(lv);
            if (ptr == null || psb.com is not ICommunicatorNX sb)
                return;
            var size = RamOffsets.GetTrainerBlockSize(lv);
            data = data.Slice(0, size);
            var trainer_name = ptr + "]+14";
            var trainer_name_addr = InjectionUtil.GetPointerAddress(sb, trainer_name);
            psb.com.WriteBytes(data.Slice(0, 0x1A), trainer_name_addr);
            psb.com.WriteBytes(data.SliceEnd(0x1A + 0x2), InjectionUtil.GetPointerAddress(sb, ptr) + 0x8);
        }

        public static bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out byte[]? read)
        {
            read = null;
            if (!FunctionMap.ContainsKey(block))
                return false;
            try
            {
                var data = sav.GetType().GetProperty(block).GetValue(sav);

                if (data is SaveBlock sb)
                {
                    var getter = FunctionMap[block].Item1;
                    read = getter.Invoke(psb);
                    if (read == null)
                        return false;
                    read.CopyTo(sb.Data, sb.Offset);
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.StackTrace);
                return false;
            }
        }

        public static void WriteBlockFromString(PokeSysBotMini psb, string block, byte[] data, object sb)
        {
            if (!FunctionMap.ContainsKey(block))
                return;
            var setter = FunctionMap[block].Item2;
            var offset = ((SaveBlock)sb).Offset;
            setter.Invoke(psb, data.SliceEnd(offset));
        }
    }
}
