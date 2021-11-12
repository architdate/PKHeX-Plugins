using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core.Injection
{
    public static class LPBasic
    {
        public static byte[] ReadBox(this PokeSysBotMini psb, int box, int len, List<byte[]> allpkm)
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

        public static byte[] ReadSlot(this PokeSysBotMini psb, int box, int slot) => psb.com.ReadBytes(psb.GetSlotOffset(box, slot), psb.SlotSize + psb.GapSize);

        public static void SendSlot(this PokeSysBotMini psb, byte[] data, int box, int slot) => psb.com.WriteBytes(data, psb.GetSlotOffset(box, slot));

        public static void SendBox(this PokeSysBotMini psb, byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(psb.SlotSize);
            for (int i = 0; i < psb.SlotCount; i++)
                SendSlot(psb, pkmData[i], box, i);
        }
    }
}

