using System;

namespace PKHeX.Core.Injection
{
    public class LiveHeXController
    {
        private readonly ISaveFileProvider SAV;
        public readonly IPKMView Editor;
        public PokeSysBotMini Bot;

        public LiveHeXController(ISaveFileProvider boxes, IPKMView editor, InjectorCommunicationType ict)
        {
            SAV = boxes;
            Editor = editor;
            var ValidVers = RamOffsets.GetValidVersions(boxes.SAV);
            Bot = new PokeSysBotMini(ValidVers[0], ict);
        }

        public void ChangeBox(int box)
        {
            if (!Bot.Connected)
                return;

            var sav = SAV.SAV;
            if ((uint)box >= sav.BoxCount)
                return;

            ReadBox(box);
        }

        public void ReadBox(int box)
        {
            var sav = SAV.SAV;
            var len = SAV.SAV.BoxSlotCount * (RamOffsets.GetSlotSize(Bot.Version) + RamOffsets.GetGapSize(Bot.Version));
            var data = Bot.ReadBox(box, len);
            sav.SetBoxBinary(data, box);
            SAV.ReloadSlots();
        }

        public void WriteBox(int box)
        {
            var boxData = SAV.SAV.GetBoxBinary(box);
            Bot.SendBox(boxData, box);
        }

        public void WriteActiveSlot(int box, int slot)
        {
            var pkm = Editor.PreparePKM();
            pkm.ResetPartyStats();
            var data = RamOffsets.WriteBoxData(Bot.Version) ? pkm.EncryptedBoxData : pkm.EncryptedPartyData;
            Bot.SendSlot(data, box, slot);
        }

        public void ReadActiveSlot(int box, int slot)
        {
            var data = Bot.ReadSlot(box, slot);
            var pkm = SAV.SAV.GetDecryptedPKM(data);
            Editor.PopulateFields(pkm);
        }

        public bool ReadOffset(uint offset)
        {
            var data = Bot.ReadOffset(offset);
            var pkm = SAV.SAV.GetDecryptedPKM(data);

            // Since data might not actually exist at the user-specified offset, double check that the pkm data is valid.
            if (!pkm.ChecksumValid)
                return false;

            Editor.PopulateFields(pkm);
            return true;
        }

        public byte[]? ReadBlock<TSaveFile, TOffset>(Func<SaveFile, SaveBlock> getBlock, Func<TOffset, uint> getOfs) where TSaveFile: SaveFile where TOffset : class
        {
            var obj = (TOffset)RamOffsets.GetOffsets(Bot.Version);
            var sav = (TSaveFile)SAV.SAV;
            var data = getBlock(sav);
            if (data == null)
                return null;
            var ofs = getOfs(obj);

            var read = ReadRAM(ofs, data.Data.Length);
            read.CopyTo(data.Data, data.Offset);
            // Usage: ReadBlock<SAV8, Offsets8>(z => ((ISaveBlock8Main)z).MyStatus, z => z.MyStatus);
            return read;
        }

        // Reflection method
        public byte[] ReadBlockFromString(SaveFile sav, string block)
        {
            var obj = RamOffsets.GetOffsets(Bot.Version);
            var offset = obj.GetType().GetField(block).GetValue(obj);
            var data = (SaveBlock)sav.GetType().GetField(block).GetValue(sav);
            var read = ReadRAM((uint)offset, data.Data.Length);
            read.CopyTo(data.Data, data.Offset);
            return read;
        }

        public void WriteBlockFromString(string block, byte[] data)
        {
            var obj = RamOffsets.GetOffsets(Bot.Version);
            var offset = obj.GetType().GetField(block).GetValue(obj);
            WriteRAM((uint)offset, data);
        }

        public byte[] ReadRAM(uint offset, int size) => Bot.com.ReadBytes(offset, size);

        public void WriteRAM(uint offset, byte[] data) => Bot.com.WriteBytes(data, offset);
    }
}