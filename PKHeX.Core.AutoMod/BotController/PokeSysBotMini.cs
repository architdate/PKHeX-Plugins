using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.AutoMod.LiveHeXVersion;

namespace PKHeX.Core.AutoMod
{
    public class PokeSysBotMini
    {
        public static int BoxStart = 0x4506D890;
        public static int SlotSize = 344;
        public static int SlotCount = 30;
        public static int GapSize = 0;
        public LiveHeXVersion Version;
        public ICommunicator com;
        public bool Connected => com.Connected;

        public PokeSysBotMini(LiveHeXVersion lv)
        {
            Version = lv;
            com = RamOffsets.GetCommunicator(lv);
            BoxStart = RamOffsets.GetB1S1Offset(lv);
            SlotSize = RamOffsets.GetSlotSize(lv);
            SlotCount = RamOffsets.GetSlotCount(lv);
            GapSize = RamOffsets.GetGapSize(lv);
        }

        private static uint GetBoxOffset(int box) => (uint)BoxStart + (uint)((SlotSize + GapSize) * SlotCount * box);
        private static uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)(SlotSize * slot);

        public byte[] ReadBox(int box, int len)
        {
            var bytes = com.ReadBytes(GetBoxOffset(box), len);
            if (GapSize == 0)
                return bytes;
            var allpkm = new List<byte[]>();
            var retval = new byte[0];
            var currofs = 0;
            for (int i = 0; i < SlotCount; i++)
            {
                allpkm.Add(bytes.Slice(currofs, SlotSize));
                currofs += SlotSize + GapSize;
            }
            foreach (var x in allpkm)
            {
                retval = retval.Concat(x).ToArray();
            }
            return retval;
        }

        public byte[] ReadSlot(int box, int slot) => com.ReadBytes(GetSlotOffset(box, slot), SlotSize);
        public byte[] ReadOffset(uint offset) => com.ReadBytes(offset, SlotSize);

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
        }

        public void SendSlot(byte[] data, int box, int slot) => com.WriteBytes(data, GetSlotOffset(box, slot));
    }
}