namespace PKHeX.Core.AutoMod
{
    public class LiveHexController
    {
        private readonly ISaveFileProvider SAV;
        private readonly IPKMView Editor;
        public readonly PokeSysBotMini Bot = new PokeSysBotMini();

        public LiveHexController(ISaveFileProvider boxes, IPKMView editor)
        {
            SAV = boxes;
            Editor = editor;
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
            var len = SAV.SAV.BoxSlotCount * 344;
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
            var data = pkm.EncryptedPartyData;
            Bot.SendSlot(data, box, slot);
        }

        public void ReadActiveSlot(int box, int slot)
        {
            var data = Bot.ReadSlot(box, slot);
            var pkm = new PK8(data);
            Editor.PopulateFields(pkm);
        }
    }
}