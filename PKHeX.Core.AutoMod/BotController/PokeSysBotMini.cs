namespace PKHeX.Core.AutoMod
{
    public class PokeSysBotMini : SysBotMini
    {
        private const int BoxStart = 0x4293D8B0;
        private const int SlotSize = 344;
        private const int SlotCount = 30;

        private static uint GetBoxOffset(int box) => BoxStart + (uint)(SlotSize * SlotCount * box);
        private static uint GetSlotOffset(int box, int slot) => GetBoxOffset(box) + (uint)(SlotSize * slot);

        public byte[] ReadBox(int box, int len) => ReadBytes(GetBoxOffset(box), len);
        public byte[] ReadSlot(int box, int slot) => ReadBytes(GetSlotOffset(box, slot), 344);

        public void SendBox(byte[] boxData, int box) => WriteBytes(boxData, GetBoxOffset(box));
        public void SendSlot(byte[] data, int box, int slot) => WriteBytes(data, GetSlotOffset(box, slot));
    }
}