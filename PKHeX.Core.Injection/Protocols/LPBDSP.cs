using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PKHeX.Core.Injection
{
    public class LPBDSP(LiveHeXVersion lv, bool useCache) : InjectionBase(lv, useCache)
    {
        private static readonly LiveHeXVersion[] BrilliantDiamond =
        [
            LiveHeXVersion.BD_v100,
            LiveHeXVersion.BD_v110,
            LiveHeXVersion.BD_v111,
            LiveHeXVersion.BDSP_v112,
            LiveHeXVersion.BDSP_v113,
            LiveHeXVersion.BDSP_v120,
            LiveHeXVersion.BD_v130
        ];
        private static readonly LiveHeXVersion[] ShiningPearl =
        [
            LiveHeXVersion.SP_v100,
            LiveHeXVersion.SP_v110,
            LiveHeXVersion.SP_v111,
            LiveHeXVersion.BDSP_v112,
            LiveHeXVersion.BDSP_v113,
            LiveHeXVersion.BDSP_v120,
            LiveHeXVersion.SP_v130
        ];
        private static readonly LiveHeXVersion[] SupportedVersions = ArrayUtil.ConcatAll(
            BrilliantDiamond,
            ShiningPearl
        );

        public static LiveHeXVersion[] GetVersions() => SupportedVersions;

        private const int ITEM_BLOCK_SIZE = 0xBB80;
        private const int ITEM_BLOCK_SIZE_RAM = (0xBB80 / 0x10) * 0xC;

        private const int UG_ITEM_BLOCK_SIZE = 999 * 0xC;
        private const int UG_ITEM_BLOCK_SIZE_RAM = 999 * 0x8;

        private const int DAYCARE_BLOCK_SIZE = 0x2C0;
        private const int DAYCARE_BLOCK_SIZE_RAM = 0x8 * 4;

        private const int MYSTATUS_BLOCK_SIZE = 0x50;
        private const int MYSTATUS_BLOCK_SIZE_RAM = 0x34;

        public static readonly Dictionary<
            string,
            (Func<PokeSysBotMini, byte[]?>, Action<PokeSysBotMini, byte[]>)
        > FunctionMap =
            new()
            {
                { "Items", (GetItemBlock, SetItemBlock) },
                { "MyStatus", (GetMyStatusBlock, SetMyStatusBlock) },
                { "Underground", (GetUGItemBlock, SetUGItemBlock) },
                { "Daycare", (GetDaycareBlock, SetDaycareBlock) },
            };

        public override Dictionary<string, string> SpecialBlocks { get; } =
            new()
            {
                { "Items", "B_OpenItemPouch_Click" },
                { "Underground", "B_OpenUGSEditor_Click" }
            };

        public static readonly IEnumerable<Type> types = Assembly.GetAssembly(typeof(ICustomBlock))!
            .GetTypes()
            .Where(t => typeof(ICustomBlock).IsAssignableFrom(t) && !t.IsInterface);

        private static ulong[] GetPokemonPointers(PokeSysBotMini psb, int box)
        {
            var sb = (ICommunicatorNX)psb.com;
            var (ptr, count) = RamOffsets.BoxOffsets(psb.Version);
            var addr = psb.GetCachedPointer(sb, ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var b = psb.com.ReadBytes(addr, count * 8);
            var boxptr =
                Core.ArrayUtil
                    .EnumerateSplit(b, 8)
                    .Select(z => BitConverter.ToUInt64(z, 0))
                    .ToArray()[box] + 0x20; // add 0x20 to remove vtable bytes
            b = sb.ReadBytesAbsolute(boxptr, psb.SlotCount * 8);

            var pkmptrs = Core.ArrayUtil
                .EnumerateSplit(b, 8)
                .Select(z => BitConverter.ToUInt64(z, 0))
                .ToArray();
            return pkmptrs;
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetTrainerPointer(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130 => "[[[main+4C64DC0]+B8]+10]+E0",
                LiveHeXVersion.SP_v130 => "[[[main+4E7BE98]+B8]+10]+E0",
                LiveHeXVersion.BDSP_v120 => "[[[main+4E36C58]+B8]+10]+E0",
                LiveHeXVersion.BDSP_v113 => "[[[main+4E59E60]+B8]+10]+E0",
                LiveHeXVersion.BDSP_v112 => "[[[main+4E34DD0]+B8]+10]+E0",
                LiveHeXVersion.BD_v111 => "[[[main+4C1DCF8]+B8]+10]+E0",
                LiveHeXVersion.SP_v111 => "[[[main+4E34DD0]+B8]+10]+E0",
                _ => null
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetItemPointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130 => "[[[[main+4C64DC0]+B8]+10]+48]+20",
                LiveHeXVersion.SP_v130 => "[[[[main+4E7BE98]+B8]+10]+48]+20",
                LiveHeXVersion.BDSP_v120 => "[[[[main+4E36C58]+B8]+10]+48]+20",
                LiveHeXVersion.BDSP_v113 => "[[[[main+4E59E60]+B8]+10]+48]+20",
                LiveHeXVersion.BDSP_v112 => "[[[[main+4E34DD0]+B8]+10]+48]+20",
                LiveHeXVersion.BD_v111 => "[[[[main+4C1DCF8]+B8]+10]+48]+20",
                LiveHeXVersion.SP_v111 => "[[[[main+4E34DD0]+B8]+10]+48]+20",
                _ => null
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetUndergroundPointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130 => "[[[[main+4C64DC0]+B8]+10]+50]+20",
                LiveHeXVersion.SP_v130 => "[[[[main+4E7BE98]+B8]+10]+50]+20",
                LiveHeXVersion.BDSP_v120 => "[[[[main+4E36C58]+B8]+10]+50]+20",
                LiveHeXVersion.BDSP_v113 => "[[[[main+4E59E60]+B8]+10]+50]+20",
                LiveHeXVersion.BDSP_v112 => "[[[[main+4E34DD0]+B8]+10]+50]+20",
                LiveHeXVersion.BD_v111 => "[[[[main+4C1DCF8]+B8]+10]+50]+20",
                LiveHeXVersion.SP_v111 => "[[[[main+4E34DD0]+B8]+10]+50]+20",
                _ => null
            };
        }

        // relative to: PlayerWork.SaveData_TypeInfo
        private static string? GetDaycarePointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v130 => "[[[main+4C64DC0]+B8]+10]+450",
                LiveHeXVersion.SP_v130 => "[[[main+4E7BE98]+B8]+10]+450",
                LiveHeXVersion.BDSP_v120 => "[[[main+4E36C58]+B8]+10]+450",
                LiveHeXVersion.BDSP_v113 => "[[[main+4E59E60]+B8]+10]+450",
                LiveHeXVersion.BDSP_v112 => "[[[main+4E34DD0]+B8]+10]+450",
                LiveHeXVersion.BD_v111 => "[[[main+4C1DCF8]+B8]+10]+450",
                LiveHeXVersion.SP_v111 => "[[[main+4E34DD0]+B8]+10]+450",
                _ => null
            };
        }

        public override byte[] ReadBox(PokeSysBotMini psb, int box, int _, List<byte[]> allpkm)
        {
            if (psb.com is not ICommunicatorNX sb)
                return ArrayUtil.ConcatAll(allpkm.ToArray());
            var pkmptrs = GetPokemonPointers(psb, box);

            var offsets = pkmptrs.ToDictionary(p => p + 0x20, _ => psb.SlotSize);
            return sb.ReadBytesAbsoluteMulti(offsets);
        }

        public override byte[] ReadSlot(PokeSysBotMini psb, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb)
                return new byte[psb.SlotSize];
            var pkmptr = GetPokemonPointers(psb, box)[slot];
            return sb.ReadBytesAbsolute(pkmptr + 0x20, psb.SlotSize);
        }

        public override void SendSlot(PokeSysBotMini psb, byte[] data, int box, int slot)
        {
            if (psb.com is not ICommunicatorNX sb)
                return;
            var pkmptr = GetPokemonPointers(psb, box)[slot];
            sb.WriteBytesAbsolute(data, pkmptr + 0x20);
        }

        public override void SendBox(PokeSysBotMini psb, byte[] boxData, int box)
        {
            if (psb.com is not ICommunicatorNX sb)
                return;

            ReadOnlySpan<byte> bytes = boxData;
            byte[][] pkmData = bytes.Split(psb.SlotSize);
            var pkmptrs = GetPokemonPointers(psb, box);
            for (int i = 0; i < psb.SlotCount; i++)
                sb.WriteBytesAbsolute(pkmData[i], pkmptrs[i] + 0x20);
        }

        public static readonly Func<PokeSysBotMini, byte[]?> GetTrainerData = psb =>
        {
            var lv = psb.Version;
            var ptr = GetTrainerPointer(lv);
            if (ptr is null || psb.com is not ICommunicatorNX sb)
                return null;

            var retval = new byte[MYSTATUS_BLOCK_SIZE];
            var ram_block = psb.GetCachedPointer(sb, ptr);
            if (ram_block == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var trainer_name = ptr.ExtendPointer(0x14);
            var trainer_name_addr = psb.GetCachedPointer(sb, trainer_name);
            if (trainer_name_addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            psb.com.ReadBytes(trainer_name_addr, 0x1A).CopyTo(retval.AsSpan());

            var extra = psb.com.ReadBytes(ram_block, MYSTATUS_BLOCK_SIZE_RAM);
            // TID, SID, Money, Male
            extra.AsSpan(0x8, 0x9).CopyTo(retval.AsSpan(0x1C));
            // Region Code, Badge Count, TrainerView, ROMCode, GameClear
            extra.AsSpan(0x11, 0x5).CopyTo(retval.AsSpan(0x28));
            // BodyType, Fashion ID
            extra.AsSpan(0x16, 0x2).CopyTo(retval.AsSpan(0x30));
            // StarterType, DSPlayer, FollowIndex, X, Y, Height, Rotation
            extra.AsSpan(0x18).ToArray().CopyTo(retval, 0x34);

            return retval;
        };

        private static byte[]? GetItemBlock(PokeSysBotMini psb)
        {
            var ptr = GetItemPointers(psb.Version);
            if (ptr is null)
                return null;

            var nx = (ICommunicatorNX)psb.com;
            var addr = psb.GetCachedPointer(nx, ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var item_blk = psb.com.ReadBytes(addr, ITEM_BLOCK_SIZE_RAM);
            var items = Core.ArrayUtil
                .EnumerateSplit(item_blk, 0xC)
                .Select(z =>
                {
                    var retval = new byte[0x10];
                    var zSpan = z.AsSpan();
                    var rSpan = retval.AsSpan();
                    zSpan[..0x5].CopyTo(rSpan);
                    zSpan[0x5..0x6].CopyTo(rSpan[0x8..]);
                    zSpan[0xA..].CopyTo(rSpan[0xC..]);
                    return retval;
                })
                .ToArray();
            return ArrayUtil.ConcatAll(items);
        }

        private static void SetItemBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetItemPointers(psb.Version);
            if (ptr is null)
                return;

            var nx = (ICommunicatorNX)psb.com;
            var addr = psb.GetCachedPointer(nx, ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            data = data.AsSpan(0, ITEM_BLOCK_SIZE).ToArray();
            var items = Core.ArrayUtil
                .EnumerateSplit(data, 0x10)
                .Select(z =>
                {
                    var retval = new byte[0xC];
                    var zSpan = z.AsSpan();
                    var rSpan = retval.AsSpan();
                    zSpan[..0x5].CopyTo(rSpan);
                    zSpan[0x8..0x9].CopyTo(rSpan[0x5..]);
                    zSpan[0xC..0xE].CopyTo(rSpan[0xA..]);
                    return retval;
                })
                .ToArray();
            var payload = ArrayUtil.ConcatAll(items);
            psb.com.WriteBytes(payload, addr);
        }

        private static byte[]? GetUGItemBlock(PokeSysBotMini psb)
        {
            var ptr = GetUndergroundPointers(psb.Version);
            if (ptr is null)
                return null;

            var nx = (ICommunicatorNX)psb.com;
            var addr = psb.GetCachedPointer(nx, ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var item_blk = psb.com.ReadBytes(addr, UG_ITEM_BLOCK_SIZE_RAM);
            var extra_data = new byte[] { 0x0, 0x0, 0x0, 0x0 };
            var items = Core.ArrayUtil
                .EnumerateSplit(item_blk, 0x8)
                .Select(z => z.Concat(extra_data).ToArray())
                .ToArray();
            return ArrayUtil.ConcatAll(items);
        }

        private static void SetUGItemBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetUndergroundPointers(psb.Version);
            if (ptr is null)
                return;

            var nx = (ICommunicatorNX)psb.com;
            var addr = psb.GetCachedPointer(nx, ptr);
            if (addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            data = data.AsSpan(0, UG_ITEM_BLOCK_SIZE).ToArray();
            var items = Core.ArrayUtil
                .EnumerateSplit(data, 0xC)
                .Select(z => z.AsSpan(0, 0x8).ToArray())
                .ToArray();
            var payload = ArrayUtil.ConcatAll(items);
            psb.com.WriteBytes(payload, addr);
        }

        private static byte[]? GetMyStatusBlock(PokeSysBotMini psb) => GetTrainerData(psb);

        private static void SetMyStatusBlock(PokeSysBotMini psb, byte[] data)
        {
            var lv = psb.Version;
            var ptr = GetTrainerPointer(lv);
            if (ptr is null || psb.com is not ICommunicatorNX sb)
                return;

            data = data.AsSpan(0, MYSTATUS_BLOCK_SIZE).ToArray();
            var trainer_name = ptr.ExtendPointer(0x14);
            var trainer_name_addr = psb.GetCachedPointer(sb, trainer_name);
            if (trainer_name_addr == InjectionUtil.INVALID_PTR)
                throw new Exception("Invalid Pointer string.");

            var retval = new byte[MYSTATUS_BLOCK_SIZE_RAM];
            // TID, SID, Money, Male
            data.AsSpan(0x1C, 0x9).CopyTo(retval.AsSpan(0x8));
            // Region Code, Badge Count, TrainerView, ROMCode, GameClear
            data.AsSpan(0x28, 0x5).CopyTo(retval.AsSpan(0x11));
            // BodyType, Fashion ID
            data.AsSpan(0x30, 0x2).CopyTo(retval.AsSpan(0x16));
            // StarterType, DSPlayer, FollowIndex, X, Y, Height, Rotation
            data.AsSpan(0x34).ToArray().CopyTo(retval, 0x18);

            psb.com.WriteBytes(data.AsSpan(0, 0x1A), trainer_name_addr);
            psb.com.WriteBytes(retval.AsSpan(0x8).ToArray(), psb.GetCachedPointer(sb, ptr) + 0x8);
        }

        private static byte[]? GetDaycareBlock(PokeSysBotMini psb)
        {
            var ptr = GetDaycarePointers(psb.Version);
            if (ptr is null)
                return null;

            var nx = (ICommunicatorNX)psb.com;
            var addr = psb.GetCachedPointer(nx, ptr);
            var p1ptr = psb.GetCachedPointer(nx, ptr.ExtendPointer(0x20, 0x20));
            var p2ptr = psb.GetCachedPointer(nx, ptr.ExtendPointer(0x28, 0x20));
            var parent_one = psb.com.ReadBytes(p1ptr, 0x158);
            var parent_two = psb.com.ReadBytes(p2ptr, 0x158);
            var extra = psb.com.ReadBytes(addr + 0x8, 0x18);
            var extra_arr = Core.ArrayUtil.EnumerateSplit(extra, 0x8).ToArray();
            var block = new byte[DAYCARE_BLOCK_SIZE];

            parent_one.CopyTo(block, 0);
            parent_two.CopyTo(block, 0x158);
            extra_arr[0].AsSpan(0, 4).CopyTo(block.AsSpan(0x158 * 2));
            extra_arr[1].CopyTo(block, (0x158 * 2) + 0x4);
            extra_arr[2].AsSpan(0, 4).CopyTo(block.AsSpan((0x158 * 2) + 0x4 + 0x8));
            return block;
        }

        private static void SetDaycareBlock(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetDaycarePointers(psb.Version);
            if (ptr is null)
                return;

            var nx = (ICommunicatorNX)psb.com;
            var addr = psb.GetCachedPointer(nx, ptr);
            var parent_one_addr = psb.GetCachedPointer(nx, ptr.ExtendPointer(0x20, 0x20));
            var parent_two_addr = psb.GetCachedPointer(nx, ptr.ExtendPointer(0x28, 0x20));

            data = data.AsSpan(0, DAYCARE_BLOCK_SIZE).ToArray();
            psb.com.WriteBytes(data.AsSpan(0, 0x158), parent_one_addr);
            psb.com.WriteBytes(data.AsSpan(0x158, 0x158), parent_two_addr);

            var payload = new byte[DAYCARE_BLOCK_SIZE_RAM - 0x8];
            data.AsSpan(0x158 * 2, 4).CopyTo(payload.AsSpan());
            data.AsSpan((0x158 * 2) + 0x4, 0x8).CopyTo(payload.AsSpan( 0x8));
            data.AsSpan((0x158 * 2) + 0x4 + 0x8, 0x4).CopyTo(payload.AsSpan(0x8 * 2));
            psb.com.WriteBytes(payload, addr + 0x8);
        }

        public override bool ReadBlockFromString(
            PokeSysBotMini psb,
            SaveFile sav,
            string block,
            out List<byte[]>? read
        )
        {
            read = null;
            if (!FunctionMap.TryGetValue(block, out var value))
            {
                // Check for custom blocks
                foreach (Type t in types)
                {
                    if (t.Name != block)
                        continue;

                    var m = t.GetMethod("Getter", BindingFlags.Public | BindingFlags.Static);
                    if (m is null)
                        return false;

                    var funcout = (byte[]?)m.Invoke(null, new object[] { psb });
                    if (funcout is not null)
                        read = [funcout];
                    return true;
                }
                return false;
            }
            try
            {
                var data = (
                    sav.GetType().GetProperty(block) ?? throw new Exception("Invalid Block")
                ).GetValue(sav);

                if (data is IDataIndirect sb)
                {
                    var getter = value.Item1;
                    var funcout = getter.Invoke(psb);
                    if (funcout is null)
                        return false;

                    funcout.CopyTo(sb.Data, sb.Offset);
                    read = [funcout];
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

        public override void WriteBlockFromString(
            PokeSysBotMini psb,
            string block,
            byte[] data,
            object sb
        )
        {
            if (!FunctionMap.TryGetValue(block, out var value))
            {
                // Custom Blocks
                ((ICustomBlock)sb).Setter(psb, data);
                return;
            }
            var setter = value.Item2;
            var offset = ((IDataIndirect)sb).Offset;
            setter.Invoke(psb, data.AsSpan(offset).ToArray());
        }
    }
}
