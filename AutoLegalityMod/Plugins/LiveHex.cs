using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public class LiveHex : AutoModPlugin
    {
        public override string Name => "Live PKHeX";
        public override int Priority => 1;

        public Socket Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
        private readonly string IP = "192.168.1.65";
        private readonly int Port = 6000;

        public void Connect()
        {
            Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Connection.Connect(IP, Port);
        }

        public void Disconnect()
        {
            Connection.Shutdown(SocketShutdown.Both);
        }
        public int Read(byte[] buffer) => Connection.Receive(buffer);
        public int Send(byte[] buffer) => Connection.Send(buffer);

        public bool Connected = false;

        public byte[] ReadBytes(uint myGiftAddress, int length)
        {
            var cmd = SwitchCommand.Peek(myGiftAddress, length);
            Send(cmd);

            // give it time to push data back
            Thread.Sleep((length / 8) + 200);
            var buffer = new byte[(length * 2) + 1];
            var _ = Read(buffer);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            var c1 = new ToolStripMenuItem("Load Box") { Image = Properties.Resources.uploadgpss };
            c1.Click += (s, e) => LoadABox();
            var c2 = new ToolStripMenuItem("Inject Mon") { Image = Properties.Resources.mgdbdownload };
            c2.Click += (s, e) => InjectAMon();
            var c3 = new ToolStripMenuItem("Disconnect") { Image = Properties.Resources.mgdbdownload };
            c3.Click += (s, e) => Disconnect();

            ctrl.DropDownItems.Add(c1);
            ctrl.DropDownItems.Add(c2);
            ctrl.DropDownItems.Add(c3);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Image = Properties.Resources.mgdbdownload;
        }

        private void LoadABox()
        {
            var loadboxform = new GUI.LoadBox();
            loadboxform.ShowDialog();
            LoadBox(loadboxform.Box);
        }

        private void InjectAMon()
        {
            var injectmonform = new GUI.InjectPKM();
            injectmonform.ShowDialog();
            InjectPokemon(injectmonform.boxval, injectmonform.slotval);
        }

        private void LoadBox(int box)
        {
            if (!Connected)
                Connect();
            Connected = true;
            var pokesize = 344;
            var boxsize = 30;
            var pklist = GrabPKM(0x4293D8B0 + (uint)((box - 1) * boxsize * pokesize), boxsize, pokesize);
            SetToBox(box, 1, pklist);
        }

        private void SetToBox(int box, int slot, PKM[] pklist)
        {
            // Set a pkm list to box
            var sav = SaveFileEditor.SAV;
            var BoxData = sav.BoxData;
            slot = (box - 1) * sav.BoxSlotCount + (slot - 1);
            for (int i = 0; i < pklist.Length; i++)
                BoxData[slot + i] = pklist[i];
            sav.BoxData = BoxData;
            SaveFileEditor.ReloadSlots();
        }

        private PKM[] GrabPKM(uint address, int count, int pokesize = 344)
        {
            int length = count * pokesize;
            var bytes = ReadBytes(address, length);
            byte[][] chunks = bytes
                .Select((s, i) => new { Value = s, Index = i })
                .GroupBy(x => x.Index / pokesize)
                .Select(grp => grp.Select(x => x.Value).ToArray())
                .ToArray();
            List<PKM> pklist = new List<PKM>();
            foreach (var pkbytes in chunks)
                pklist.Add(new PK8(pkbytes));
            return pklist.ToArray();
        }

        private void InjectPokemon(int box, int slot, uint b1s1 = 0x4293D8B0, int pokesize = 344)
        {
            var sav = SaveFileEditor.SAV;
            slot = (box - 1) * sav.BoxSlotCount + (slot - 1);
            uint address = b1s1 + (uint)(slot * pokesize);
            Send(SwitchCommand.Poke(address, PKMEditor.PreparePKM().EncryptedPartyData));
        }
    }
}