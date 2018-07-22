using System;
using System.IO;

namespace pkmn_ntr.Helpers
{
    public class ScriptHelper
    {
        public void bpadd(uint addr, string type = "code.once")
        {
            uint num = 0;
            if (type == "code")
            {
                num = 1;
            }
            if (type == "code.once")
            {
                num = 2;
            }
            if (num != 0)
            {
                WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(11, num, addr, 1);
            }
        }

        public void remoteplay()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(901);
            WonderTradeBot.WonderTradeBot.ntrClient.log("Will be disconnected in 10 seconds to enhance performance.");
            disconnect();
        }

        public void bpdis(uint id)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(11, id, 0, 3);
        }

        public void bpena(uint id)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(11, id, 0, 2);
        }

        public void resume()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(11, 0, 0, 4);
        }

        public void connect(string host, int port)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.setServer(host, port);
            WonderTradeBot.WonderTradeBot.ntrClient.connectToServer();
        }

        public void reload()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendReloadPacket();
        }

        public void listprocess()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(5);
        }

        public void listthread(int pid)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(7, (uint)pid);
        }

        public void attachprocess(int pid, uint patchAddr = 0)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(6, (uint)pid, patchAddr);
        }

        public void queryhandle(int pid)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(12, (uint)pid);
        }

        public void memlayout(int pid)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendEmptyPacket(8, (uint)pid);
        }

        public void disconnect()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.disconnect();
        }

        public void sayhello()
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendHelloPacket();
        }

        public void data(uint addr, uint size = 0x100, int pid = -1, string filename = null)
        {
            if (filename == null && size > 1024)
            {
                size = 1024;
            }
            WonderTradeBot.WonderTradeBot.ntrClient.sendReadMemPacket(addr, size, (uint)pid, filename);
        }

        public uint data(uint addr, uint size = 0x100, int pid = -1)
        {
            return WonderTradeBot.WonderTradeBot.ntrClient.sendReadMemPacket(addr, size, (uint)pid);
        }

        public void write(uint addr, byte[] buf, int pid = -1)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendWriteMemPacket(addr, (uint)pid, buf);
        }

        public void writebyte(uint addr, byte buf, int pid = -1)
        {
            WonderTradeBot.WonderTradeBot.ntrClient.sendWriteMemPacketByte(addr, (uint)pid, buf);
        }

        public void sendfile(String localPath, String remotePath)
        {
            FileStream fs = new FileStream(localPath, FileMode.Open);
            byte[] buf = new byte[fs.Length];
            fs.Read(buf, 0, buf.Length);
            fs.Close();
            WonderTradeBot.WonderTradeBot.ntrClient.sendSaveFilePacket(remotePath, buf);
        }
    }
}
