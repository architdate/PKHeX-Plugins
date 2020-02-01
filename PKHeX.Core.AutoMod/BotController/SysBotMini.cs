using System.Net.Sockets;
using System.Threading;

namespace PKHeX.Core.AutoMod
{
    public class SysBotMini
    {
        public string IP = "192.168.1.65";
        public int Port = 6000;

        public Socket Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);

        public bool Connected = false;

        public void Connect()
        {
            Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Connection.Connect(IP, Port);
            Connected = true;
        }

        public void Disconnect()
        {
            Connection.Disconnect(false);
            Connected = false;
        }

        public int Read(byte[] buffer) => Connection.Receive(buffer);
        public int Send(byte[] buffer) => Connection.Send(buffer);

        public byte[] ReadBytes(uint myGiftAddress, int length)
        {
            var cmd = SwitchCommand.Peek(myGiftAddress, length);
            Send(cmd);

            // give it time to push data back
            Thread.Sleep((length / 256) + 100);
            var buffer = new byte[(length * 2) + 1];
            var _ = Read(buffer);
            return Decoder.ConvertHexByteStringToBytes(buffer);
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            Send(SwitchCommand.Poke(offset, data));

            // give it time to push data back
            Thread.Sleep((data.Length / 256) + 100);
        }
    }
}