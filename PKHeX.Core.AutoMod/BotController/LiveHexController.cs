namespace PKHeX.Core.AutoMod
{
    public class LiveHexController
    {
        private readonly ISaveFileProvider SAV;
        private readonly IPKMView Editor;
        public PokeSysBotMini Bot;

        public LiveHexController(ISaveFileProvider boxes, IPKMView editor)
        {
            SAV = boxes;
            Editor = editor;
            var ValidVers = RamOffsets.GetValidVersions(boxes.SAV);
            Bot = new PokeSysBotMini(ValidVers[0]);
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
            var pkm = PKMConverter.GetPKMfromBytes(data);
            if (pkm != null)
                Editor.PopulateFields(pkm);
        }

        public bool ReadOffset(uint offset)
        {
            var data = Bot.ReadOffset(offset);
            var pkm = PKMConverter.GetPKMfromBytes(data);

            // Since data might not actually exist at the user-specified offset, double check that the pkm data is valid.
            if (pkm == null || !pkm.ChecksumValid)
                return false;
            Editor.PopulateFields(pkm);
            return true;
        }

        public byte[] ReadRAM(uint offset, int size) => Bot.com.ReadBytes(offset, size);

        public void WriteRAM(uint offset, byte[] data) => Bot.com.WriteBytes(data, offset);
    }
}