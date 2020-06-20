using static PKHeX.Core.AutoMod.LiveHeXVersion;

namespace PKHeX.Core.AutoMod
{
    public class PokeSysBotMini : SysBotMini
    {
        public static int BoxStart = 0x4506D890;
        public static int SlotSize = 344;
        public static int SlotCount = 30;

        public PokeSysBotMini(LiveHeXVersion lv)
        {
            BoxStart = RamOffsets.GetB1S1Offset(lv);
            SlotSize = RamOffsets.GetSlotSize(lv);
            SlotCount = RamOffsets.GetSlotCount(lv);
        }

        private static uint GetBoxOffset(int box) => (uint)BoxStart + (uint)(SlotSize * SlotCount * box);
        private static uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)(SlotSize * slot);

        public byte[] ReadBox(int box, int len) => ReadBytes(GetBoxOffset(box), len);
        public byte[] ReadSlot(int box, int slot) => ReadBytes(GetSlotOffset(box, slot), SlotSize);
        public byte[] ReadOffset(uint offset) => ReadBytes(offset, SlotSize);

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = boxData.Split(SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
        }

        public void SendSlot(byte[] data, int box, int slot) => WriteBytes(data, GetSlotOffset(box, slot));
    }
}