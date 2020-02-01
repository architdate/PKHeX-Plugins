using System;

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

        public void SendBox(byte[] boxData, int box)
        {
            byte[][] pkmData = BufferSplit(boxData, SlotSize);
            for (int i = 0; i < SlotCount; i++)
                SendSlot(pkmData[i], box, i);
        }
        public void SendSlot(byte[] data, int box, int slot) => WriteBytes(data, GetSlotOffset(box, slot));

        public static byte[][] BufferSplit(byte[] buffer, int blockSize)
        {
            byte[][] blocks = new byte[(buffer.Length + blockSize - 1) / blockSize][];

            for (int i = 0, j = 0; i < blocks.Length; i++, j += blockSize)
            {
                blocks[i] = new byte[Math.Min(blockSize, buffer.Length - j)];
                Array.Copy(buffer, j, blocks[i], 0, blocks[i].Length);
            }

            return blocks;
        }
    }
}