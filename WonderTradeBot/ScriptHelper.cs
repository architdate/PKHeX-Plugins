using System;
using System.IO;

namespace pkmn_ntr.Helpers
{
    public class ScriptHelper
    {
        public void Bpadd(uint addr, string type = "code.once")
        {
            uint num = 0;
            switch (type)
            {
                case "code":
                    num = 1;
                    break;
                case "code.once":
                    num = 2;
                    break;
                default:
                    return;
            }
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(11, num, addr, 1);
        }

        public void RemotePlay()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(901);
            WonderTradeBot.WonderTradeBot.ntrClient.Log("Will be disconnected in 10 seconds to enhance performance.");
            Disconnect();
        }

        public void Bpdis(uint id)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(11, id, 0, 3);
        }

        public void Bpena(uint id)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(11, id, 0, 2);
        }

        public void Resume()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(11, 0, 0, 4);
        }

        public void Connect(string host, int port)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SetServer(host, port);
            WonderTradeBot.WonderTradeBot.ntrClient.ConnectToServer();
        }

        public void Reload()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendReloadPacket();
        }

        public void ListProcess()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(5);
        }

        public void ListThread(int pid)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(7, (uint)pid);
        }

        public void AttachProcess(int pid, uint patchAddr = 0)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(6, (uint)pid, patchAddr);
        }

        public void QueryHandle(int pid)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(12, (uint)pid);
        }

        public void MemLayout(int pid)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendEmptyPacket(8, (uint)pid);
        }

        public void Disconnect()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.Disconnect();
        }

        public void SayHello()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendHelloPacket();
        }

        public void ReadData(uint addr, uint size = 0x100, int pid = -1, string filename = null)
        {
            if (filename == null && size > 1024)
            {
                size = 1024;
            }
            WonderTradeBot.WonderTradeBot.ntrClient.SendReadMemPacket(addr, size, (uint)pid, filename);
        }

        public uint ReadData(uint addr, uint size = 0x100, int pid = -1)
        {
            return WonderTradeBot.WonderTradeBot.ntrClient.SendReadMemPacket(addr, size, (uint)pid);
        }

        public void WriteData(uint addr, byte[] buf, int pid = -1)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendWriteMemPacket(addr, (uint)pid, buf);
        }

        public void WriteByte(uint addr, byte buf, int pid = -1)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.SendWriteMemPacketByte(addr, (uint)pid, buf);
        }

        public void SendFile(String localPath, String remotePath)
        {
            FileStream fs = new FileStream(localPath, FileMode.Open);
            byte[] buf = new byte[fs.Length];
            fs.Read(buf, 0, buf.Length);
            fs.Close();
            WonderTradeBot.WonderTradeBot.ntrClient.SendSaveFilePacket(remotePath, buf);
        }
    }
}
