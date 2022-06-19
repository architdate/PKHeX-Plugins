using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PKHeX.Core.Injection
{
    public static class LPBDSP
    {
        public static readonly LiveHeXVersion[] BrilliantDiamond = { LiveHeXVersion.BD_v100, LiveHeXVersion.BD_v110, LiveHeXVersion.BD_v111, LiveHeXVersion.BDSP_v112, LiveHeXVersion.BDSP_v113, LiveHeXVersion.BDSP_v120, LiveHeXVersion.BD_v130 };
        public static readonly LiveHeXVersion[] ShiningPearl     = { LiveHeXVersion.SP_v100, LiveHeXVersion.SP_v110, LiveHeXVersion.SP_v111, LiveHeXVersion.BDSP_v112, LiveHeXVersion.BDSP_v113, LiveHeXVersion.BDSP_v120, LiveHeXVersion.SP_v130 };
        public static readonly LiveHeXVersion[] SupportedVersions = ArrayUtil.ConcatAll(BrilliantDiamond, ShiningPearl);

        private const int ITEM_BLOCK_SIZE = 0xBB80;
        private const int ITEM_BLOCK_SIZE_RAM = (0xBB80 / 0x10) * 0xC;

        private const int UG_ITEM_BLOCK_SIZE = 999 * 0xC;
        private const int UG_ITEM_BLOCK_SIZE_RAM = 999 * 0x8;

        private const int DAYCARE_BLOCK_SIZE = 0x2C0;
        private const int DAYCARE_BLOCK_SIZE_RAM = 0x8 * 4;

        private const int MYSTATUS_BLOCK_SIZE = 0x50;
        private const int MYSTATUS_BLOCK_SIZE_RAM = 0x34;

        public static readonly Dictionary<string, (Func<PokeSysBotMini, byte[]?>, Action<PokeSysBotMini, byte[]>)> FunctionMap = new ()
        {
            { "Items",          (GetItemBlock, SetItemBlock) },
            { "MyStatus",       (GetMyStatusBlock, SetMyStatusBlock) },
            { "Underground",    (GetUGItemBlock, SetUGItemBlock) },
            { "Daycare",        (GetDaycareBlock, SetDaycareBlock) },
        };

        public static readonly Dictionary<string, string> SpecialBlocks = new()
        {
            { "Items", "B_OpenItemPouch_Click" },
            { "Underground", "B_OpenUGSEditor_Click" }
        };

        public static readonly IEnumerable<Type> types = Assembly.GetAssembly(typeof(ICustomBlock)).GetTypes().Where(t => typeof(ICustomBlock).IsAssignableFrom(t) && !t.IsInterface);

        private static ulong[] GetPokemonPointers(this PokeSysBotMini psb, int box)
        {
            var sb = (ICommunicatorNX)psb.com;
            var (ptr, count) = RamOffsets.BoxOffsets(psb.Version);
            var addr = sb.GetPointerAddress(ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");
            var b = psb.com.ReadBytes(addr, count * 8);
            var boxptr = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);
            var pkmptrs = Core.ArrayUtil.EnumerateSplit(b, 8).Select(z => BitConverter.ToUInt64(z, 0)).ToArray();
            return pkmptrs;
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetTrainerPointer(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130   => "[[[main+4C64DC0]+B8]+10]+E0",
                LiveHeXVersion.SP_v130   => "[[[main+4E7BE98]+B8]+10]+E0",
                LiveHeXVersion.BDSP_v120 => "[[[main+4E36C58]+B8]+10]+E0",
                LiveHeXVersion.BDSP_v113 => "[[[main+4E59E60]+B8]+10]+E0",
                LiveHeXVersion.BDSP_v112 => "[[[main+4E34DD0]+B8]+10]+E0",
                LiveHeXVersion.BD_v111   => "[[[main+4C1DCF8]+B8]+10]+E0",
                LiveHeXVersion.SP_v111   => "[[[main+4E34DD0]+B8]+10]+E0",
                _ => null
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetItemPointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130   => "[[[[main+4C64DC0]+B8]+10]+48]+20",
                LiveHeXVersion.SP_v130   => "[[[[main+4E7BE98]+B8]+10]+48]+20",
                LiveHeXVersion.BDSP_v120 => "[[[[main+4E36C58]+B8]+10]+48]+20",
                LiveHeXVersion.BDSP_v113 => "[[[[main+4E59E60]+B8]+10]+48]+20",
                LiveHeXVersion.BDSP_v112 => "[[[[main+4E34DD0]+B8]+10]+48]+20",
                LiveHeXVersion.BD_v111   => "[[[[main+4C1DCF8]+B8]+10]+48]+20",
                LiveHeXVersion.SP_v111   => "[[[[main+4E34DD0]+B8]+10]+48]+20",
                _ => null
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetUndergroundPointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130   => "[[[[main+4C64DC0]+B8]+10]+50]+20",
                LiveHeXVersion.SP_v130   => "[[[[main+4E7BE98]+B8]+10]+50]+20",
                LiveHeXVersion.BDSP_v120 => "[[[[main+4E36C58]+B8]+10]+50]+20",
                LiveHeXVersion.BDSP_v113 => "[[[[main+4E59E60]+B8]+10]+50]+20",
                LiveHeXVersion.BDSP_v112 => "[[[[main+4E34DD0]+B8]+10]+50]+20",
                LiveHeXVersion.BD_v111   => "[[[[main+4C1DCF8]+B8]+10]+50]+20",
                LiveHeXVersion.SP_v111   => "[[[[main+4E34DD0]+B8]+10]+50]+20",
                _ => null
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetDaycarePointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130   => "[[[main+4C64DC0]+B8]+10]+450",
                LiveHeXVersion.SP_v130   => "[[[main+4E7BE98]+B8]+10]+450",
                LiveHeXVersion.BDSP_v120 => "[[[main+4E36C58]+B8]+10]+450",
                LiveHeXVersion.BDSP_v113 => "[[[main+4E59E60]+B8]+10]+450",
                LiveHeXVersion.BDSP_v112 => "[[[main+4E34DD0]+B8]+10]+450",
                LiveHeXVersion.BD_v111   => "[[[main+4C1DCF8]+B8]+10]+450",
                LiveHeXVersion.SP_v111   => "[[[main+4E34DD0]+B8]+10]+450",
                _ => null
            };
        }

        public static byte[] ReadBox(PokeSysBotMini psb, int box, List<byte[]> allpkm)
        {
            if (psb.com is not ICommunicatorNX sb)
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            var pkmptrs = psb.GetPokemonPointers(box);

            var offsets = pkmptrs.ToDictionary(p => p + 0x20, _ => psb.SlotSize);
            return sb.ReadBytesAbsoluteMulti(offsets);
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
            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(psb.SlotSize);
            var pkmptrs = psb.GetPokemonPointers(box);
            for (int i = 0; i < psb.SlotCount; i++)
                sb.WriteBytesAbsolute(pkmData[i], pkmptrs[i] + 0x20);
        }

        public static readonly Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
        {
            var lv = psb.Version;
            var ptr = GetTrainerPointer(lv);
            if (ptr == null || psb.com is not ICommunicatorNX sb)
                return null;
            var retval = new byte[MYSTATUS_BLOCK_SIZE];
            var ram_block = sb.GetPointerAddress(ptr);
            if (ram_block == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var trainer_name = ptr.ExtendPointer(0x14);
            var trainer_name_addr = sb.GetPointerAddress(trainer_name);
            if (trainer_name_addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");
            psb.com.ReadBytes(trainer_name_addr, 0x1A).CopyTo(retval);

            var extra = psb.com.ReadBytes(ram_block, MYSTATUS_BLOCK_SIZE_RAM);
            // TID, SID, Money, Male
            extra.Slice(0x8, 0x9).CopyTo(retval, 0x1C);
            // Region Code, Badge Count, TrainerView, ROMCode, GameClear
            extra.Slice(0x11, 0x5).CopyTo(retval, 0x28);
            // BodyType, Fashion ID
            extra.Slice(0x16, 0x2).CopyTo(retval, 0x30);
            // StarterType, DSPlayer, FollowIndex, X, Y, Height, Rotation
            extra.SliceEnd(0x18).CopyTo(retval, 0x34);

            return retval;
        };

        public static byte[]? GetItemBlock(PokeSysBotMini psb)
        {
            var ptr = GetItemPointers(psb.Version);
            if (ptr == null)
                return null;
            var nx = (ICommunicatorNX)psb.com;
            var addr = nx.GetPointerAddress(ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");
            var item_blk = psb.com.ReadBytes(addr, ITEM_BLOCK_SIZE_RAM);
            var items = Core.ArrayUtil.EnumerateSplit(item_blk, 0xC).Select(z => {
                var retval = new byte[0x10];
                z.Slice(0, 0x5).CopyTo(retval);
                z.Slice(0x5, 0x1).CopyTo(retval, 0x8);
                z.SliceEnd(0xA).CopyTo(retval, 0xC);
                return retval;
            }).ToArray();
            return ArrayUtil.ConcatAll(items);
        }

        public static void SetItemBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetItemPointers(psb.Version);
            if (ptr == null)
                return;
            var nx = (ICommunicatorNX)psb.com;
            var addr = nx.GetPointerAddress(ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");
            data = data.Slice(0, ITEM_BLOCK_SIZE);
            var items = Core.ArrayUtil.EnumerateSplit(data, 0x10).Select(z => {
                var retval = new byte[0xC];
                z.Slice(0, 0x5).CopyTo(retval);
                z.Slice(0x8, 0x1).CopyTo(retval, 0x5);
                z.Slice(0xC, 0x2).CopyTo(retval, 0xA);
                return retval;
            }).ToArray();
            var payload = ArrayUtil.ConcatAll(items);
            psb.com.WriteBytes(payload, addr);
        }

        public static byte[]? GetUGItemBlock(PokeSysBotMini psb)
        {
            var ptr = GetUndergroundPointers(psb.Version);
            if (ptr == null)
                return null;
            var nx = (ICommunicatorNX)psb.com;
            var addr = nx.GetPointerAddress(ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");
            var item_blk = psb.com.ReadBytes(addr, UG_ITEM_BLOCK_SIZE_RAM);
            var extra_data = new byte[] { 0x0, 0x0, 0x0, 0x0 };
            var items = Core.ArrayUtil.EnumerateSplit(item_blk, 0x8).Select(z => z.Concat(extra_data).ToArray()).ToArray();
            return ArrayUtil.ConcatAll(items);
        }

        public static void SetUGItemBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetUndergroundPointers(psb.Version);
            if (ptr == null)
                return;
            var nx = (ICommunicatorNX)psb.com;
            var addr = nx.GetPointerAddress(ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");
            data = data.Slice(0, UG_ITEM_BLOCK_SIZE);
            var items = Core.ArrayUtil.EnumerateSplit(data, 0xC).Select(z => z.Slice(0, 0x8)).ToArray();
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
            data = data.Slice(0, MYSTATUS_BLOCK_SIZE);
            var trainer_name = ptr.ExtendPointer(0x14);
            var trainer_name_addr = sb.GetPointerAddress(trainer_name);
            if (trainer_name_addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var retval = new byte[MYSTATUS_BLOCK_SIZE_RAM];
            // TID, SID, Money, Male
            data.Slice(0x1C, 0x9).CopyTo(retval, 0x8);
            // Region Code, Badge Count, TrainerView, ROMCode, GameClear
            data.Slice(0x28, 0x5).CopyTo(retval, 0x11);
            // BodyType, Fashion ID
            data.Slice(0x30, 0x2).CopyTo(retval, 0x16);
            // StarterType, DSPlayer, FollowIndex, X, Y, Height, Rotation
            data.SliceEnd(0x34).CopyTo(retval, 0x18);

            psb.com.WriteBytes(data.Slice(0, 0x1A), trainer_name_addr);
            psb.com.WriteBytes(retval.SliceEnd(0x8), sb.GetPointerAddress(ptr) + 0x8);
        }

        public static byte[]? GetDaycareBlock(PokeSysBotMini psb)
        {
            var ptr = GetDaycarePointers(psb.Version);
            if (ptr == null)
                return null;
            var nx = (ICommunicatorNX)psb.com;
            var addr = nx.GetPointerAddress(ptr);
            var parent_one = psb.com.ReadBytes(nx.GetPointerAddress(ptr.ExtendPointer(0x20, 0x20)), 0x158);
            var parent_two = psb.com.ReadBytes(nx.GetPointerAddress(ptr.ExtendPointer(0x28, 0x20)), 0x158);
            var extra = psb.com.ReadBytes(addr + 0x8, 0x18);
            var extra_arr = Core.ArrayUtil.EnumerateSplit(extra, 0x8).ToArray();
            var block = new byte[DAYCARE_BLOCK_SIZE];
            parent_one.CopyTo(block, 0);
            parent_two.CopyTo(block, 0x158);
            extra_arr[0].Slice(0, 4).CopyTo(block, 0x158 * 2);
            extra_arr[1].CopyTo(block, (0x158 * 2) + 0x4);
            extra_arr[2].Slice(0, 4).CopyTo(block, (0x158 * 2) + 0x4 + 0x8);
            return block;
        }

        public static void SetDaycareBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetDaycarePointers(psb.Version);
            if (ptr == null)
                return;
            var nx = (ICommunicatorNX)psb.com;
            var addr = nx.GetPointerAddress(ptr);
            var parent_one_addr = nx.GetPointerAddress(ptr.ExtendPointer(0x20, 0x20));
            var parent_two_addr = nx.GetPointerAddress(ptr.ExtendPointer(0x28, 0x20));
            data = data.Slice(0, DAYCARE_BLOCK_SIZE);
            psb.com.WriteBytes(data.Slice(0, 0x158), parent_one_addr);
            psb.com.WriteBytes(data.Slice(0x158, 0x158), parent_two_addr);
            var payload = new byte[DAYCARE_BLOCK_SIZE_RAM - 0x8];
            data.Slice(0x158 * 2, 4).CopyTo(payload);
            data.Slice((0x158 * 2) + 0x4, 0x8).CopyTo(payload, 0x8);
            data.Slice((0x158 * 2) + 0x4 + 0x8, 0x4).CopyTo(payload, 0x8 * 2);
            psb.com.WriteBytes(payload, addr + 0x8);
        }

        public static bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out List<byte[]>? read)
        {
            read = null;
            if (!FunctionMap.ContainsKey(block))
            {
                // Check for custom blocks
                foreach (Type t in types)
                {
                    if (t.Name != block)
                        continue;
                    var m = t.GetMethod("Getter", BindingFlags.Public | BindingFlags.Static);
                    if (m == null)
                        return false;
                    var funcout = (byte[]?)m.Invoke(null, new object[] { psb });
                    if (funcout != null)
                        read = new List<byte[]> { funcout };
                    return true;
                }
                return false;
            }
            try
            {
                var data = sav.GetType().GetProperty(block).GetValue(sav);

                if (data is IDataIndirect sb)
                {
                    var getter = FunctionMap[block].Item1;
                    var funcout = getter.Invoke(psb);
                    if (funcout == null)
                        return false;
                    funcout.CopyTo(sb.Data, sb.Offset);
                    read = new List<byte[]> { funcout };
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
            {
                // Custom Blocks
                ((ICustomBlock)sb).Setter(psb, data);
                return;
            }
            var setter = FunctionMap[block].Item2;
            var offset = ((IDataIndirect)sb).Offset;
            setter.Invoke(psb, data.SliceEnd(offset));
        }
    }
}
