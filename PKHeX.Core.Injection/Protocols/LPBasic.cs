using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace PKHeX.Core.Injection
{
    public static class LPBasic
    {
        public static LiveHeXVersion[] SupportedVersions = new LiveHeXVersion[] { LiveHeXVersion.SWSH_Rigel2, LiveHeXVersion.SWSH_Rigel1, LiveHeXVersion.SWSH_Orion,
                                                                                  LiveHeXVersion.LGPE_v102, LiveHeXVersion.ORAS, LiveHeXVersion.XY, LiveHeXVersion.US_v12,
                                                                                  LiveHeXVersion.UM_v12, LiveHeXVersion.SM_v12 };
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
            byte[][] pkmData = boxData.Split(psb.SlotSize);
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
        public static bool ReadBlockFromString(PokeSysBotMini psb, SaveFile sav, string block, out byte[]? read)
        {
            read = null;
            var obj = RamOffsets.GetOffsets(psb.Version);
            if (obj == null)
                return false;
            try
            {
                var offset = obj.GetType().GetField(block).GetValue(obj);
                if (offset is uint and 0)
                    return false;
                var allblocks = sav.GetType().GetProperty("Blocks").GetValue(sav);
                var blockprop = allblocks.GetType().GetProperty(block);
                object data;
                if (allblocks is SCBlockAccessor scba && blockprop == null)
                {
                    var key = allblocks.GetType().GetField(block, BindingFlags.NonPublic | BindingFlags.Static).GetValue(allblocks);
                    data = scba.GetBlock((uint)key);
                }
                else
                {
                    data = blockprop.GetValue(allblocks);
                }

                if (data is SaveBlock sb)
                {
                    read = psb.com.ReadBytes((uint)offset, sb.Data.Length);
                    read.CopyTo(sb.Data, sb.Offset);
                }
                else if (data is SCBlock scb)
                {
                    read = psb.com.ReadBytes((ulong)offset, scb.Data.Length);
                    read.CopyTo(scb.Data, 0);
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

        public static void WriteBlockFromString(PokeSysBotMini psb, string block, byte[] data)
        {
            var obj = RamOffsets.GetOffsets(psb.Version);
            if (obj == null)
                return;
            var offset = obj.GetType().GetField(block).GetValue(obj);
            psb.com.WriteBytes(data, (uint)offset);
        }
    }
}

