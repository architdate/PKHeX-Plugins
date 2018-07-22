using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace pkmn_ntr.Helpers
{
    public class ReadMemRequest
    {
        public string fileName;
        public bool isCallback;

        public ReadMemRequest(string fileName_)
        {
            this.fileName = fileName_;
            this.isCallback = false;
        }

        public ReadMemRequest()
        {
            this.fileName = null;
            this.isCallback = true;
        }
    };

    public class DataReadyEventArgs : EventArgs
    {
        public uint seq;
        public byte[] data;

        public DataReadyEventArgs(uint seq_, byte[] data_)
        {
            this.seq = seq_;
            this.data = data_;
        }
    }

    public class InfoReadyEventArgs : EventArgs
    {
        public string info;

        public InfoReadyEventArgs(string info_)
        {
            this.info = info_;
        }
    }

    public class NTR
    {
        public String host;
        public int port;
        public TcpClient tcp;
        public NetworkStream netStream;
        public Thread packetRecvThread;
        private readonly object syncLock = new object();
        public int heartbeatSendable;
        public event EventHandler<DataReadyEventArgs> DataReady;
        public event EventHandler Connected;
        public event EventHandler<InfoReadyEventArgs> InfoReady;

        protected virtual void OnDataReady(DataReadyEventArgs e) => DataReady?.Invoke(this, e);
        protected virtual void OnConnected(EventArgs e) => Connected?.Invoke(this, e);
        protected virtual void OnInfoReady(InfoReadyEventArgs e) => InfoReady?.Invoke(this, e);

        public delegate void LogHandler(string msg);
        public event LogHandler OnLogArrival;
        private uint currentSeq;
        public Dictionary<uint, ReadMemRequest> pendingReadMem = new Dictionary<uint, ReadMemRequest>();
        public volatile int progress = -1;

        private int ReadNetworkStream(NetworkStream stream, byte[] buf, int length)
        {
            int index = 0;
            bool useProgress = false;

            if (length > 100000)
                useProgress = true;

            do
            {
                if (useProgress)
                    progress = (int)(((double)(index) / length) * 100);

                int len = stream.Read(buf, index, length - index);
                if (len == 0)
                    return 0;

                index += len;
            }
            while (index < length);
            progress = -1;
            return length;
        }

        private void PacketRecvThreadStart()
        {
            byte[] buf = new byte[84];
            uint[] args = new uint[16];
            int ret;
            NetworkStream stream = netStream;

            while (true)
            {
                try
                {
                    ret = ReadNetworkStream(stream, buf, buf.Length);
                    if (ret == 0)
                        break;

                    int t = 0;
                    uint magic = BitConverter.ToUInt32(buf, t);
                    t += 4;
                    uint seq = BitConverter.ToUInt32(buf, t);
                    t += 4;
                    uint type = BitConverter.ToUInt32(buf, t);
                    t += 4;
                    uint cmd = BitConverter.ToUInt32(buf, t);
                    for (int i = 0; i < args.Length; i++)
                    {
                        t += 4;
                        args[i] = BitConverter.ToUInt32(buf, t);
                    }
                    t += 4;
                    uint dataLen = BitConverter.ToUInt32(buf, t);
                    if (cmd != 0)
                    {
                        Log(String.Format("packet: cmd = {0}, dataLen = {1}", cmd, dataLen));
                    }

                    if (magic != 0x12345678)
                    {
                        Log(String.Format("broken protocol: magic = {0}, seq = {1}", magic, seq));
                        break;
                    }

                    if (cmd == 0)
                    {
                        if (dataLen != 0)
                        {
                            byte[] dataBuf = new byte[dataLen];
                            ReadNetworkStream(stream, dataBuf, dataBuf.Length);
                            string logMsg = Encoding.UTF8.GetString(dataBuf);
                            OnInfoReady(new InfoReadyEventArgs(logMsg));
                            Log(logMsg);
                        }
                        lock (syncLock)
                        {
                            heartbeatSendable = 1;
                        }
                        continue;
                    }
                    if (dataLen != 0)
                    {
                        byte[] dataBuf = new byte[dataLen];
                        ReadNetworkStream(stream, dataBuf, dataBuf.Length);
                        HandlePacket(cmd, seq, dataBuf);
                    }
                }
                catch (Exception e)
                {
                    Log(e.Message);
                    break;
                }
            }

            Log("Server disconnected.");
            Disconnect(false);
        }

        private string ByteToHex(byte[] datBuf) => string.Join(" ", datBuf.Select(z => $"{z:X2}"));

        private void HandleReadMem(uint seq, byte[] dataBuf)
        {
            if (!pendingReadMem.TryGetValue(seq, out ReadMemRequest requestDetails))
            {
                Log("seq not in pending readmems, ignored");
                return;
            }
            pendingReadMem.Remove(seq);

            if (requestDetails.fileName != null)
            {
                string fileName = requestDetails.fileName;
                FileStream fs = new FileStream(fileName, FileMode.Create);
                fs.Write(dataBuf, 0, dataBuf.Length);
                fs.Close();
                Log("dump saved into " + fileName + " successfully");
                return;
            }
            else if (requestDetails.isCallback)
            {
                //Copies the data, truncates if necessary
                byte[] dataBufCopy = new byte[dataBuf.Length];
                dataBuf.CopyTo(dataBufCopy, 0);
                DataReadyEventArgs e = new DataReadyEventArgs(seq, dataBufCopy);
                OnDataReady(e);
            }
            else
            {
                Log(ByteToHex(dataBuf));
            }
        }

        private void HandlePacket(uint cmd, uint seq, byte[] dataBuf)
        {
            switch (cmd)
            {
                case 9:
                    HandleReadMem(seq, dataBuf);
                    break;
            }
        }

        public void SetServer(String serverHost, int serverPort)
        {
            host = serverHost;
            port = serverPort;
        }

        public void ConnectToServer()
        {
            if (tcp != null)
                Disconnect();

            try
            {
                tcp = new TcpClient {NoDelay = true};
                tcp.Connect(host, port);
                currentSeq = 0;
                netStream = tcp.GetStream();
                heartbeatSendable = 1;
                packetRecvThread = new Thread(new ThreadStart(PacketRecvThreadStart));
                packetRecvThread.Start();
                Log("Server connected.");
                OnConnected(null);
            }
            catch
            {
                MessageBox.Show("Could not connect, make sure the IP is correct, you're running NTR and you're online in-game!", "Connection Failed");
            }
        }

        public void Disconnect(bool waitPacketThread = true)
        {
            try
            {
                tcp?.Close();
                if (waitPacketThread)
                    packetRecvThread?.Join();
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            tcp = null;
        }

        public void SendPacket(uint type, uint cmd, uint[] args, uint dataLen)
        {
            int t = 0;
            currentSeq += 1000;
            byte[] buf = new byte[84];
            BitConverter.GetBytes(0x12345678).CopyTo(buf, t);
            t += 4;
            BitConverter.GetBytes(currentSeq).CopyTo(buf, t);
            t += 4;
            BitConverter.GetBytes(type).CopyTo(buf, t);
            t += 4;
            BitConverter.GetBytes(cmd).CopyTo(buf, t);
            for (int i = 0; i < 16; i++)
            {
                t += 4;
                uint arg = 0;
                if (args != null)
                {
                    arg = args[i];
                }
                BitConverter.GetBytes(arg).CopyTo(buf, t);
            }
            t += 4;
            BitConverter.GetBytes(dataLen).CopyTo(buf, t);
            netStream.Write(buf, 0, buf.Length);
        }

        public void SendReadMemPacket(uint addr, uint size, uint pid, string fileName)
        {
            SendEmptyPacket(9, pid, addr, size);
            pendingReadMem.Add(currentSeq, new ReadMemRequest(fileName));
        }

        public uint SendReadMemPacket(uint addr, uint size, uint pid)
        {
            SendEmptyPacket(9, pid, addr, size);
            pendingReadMem.Add(currentSeq, new ReadMemRequest());
            return currentSeq;
        }

        public void SendWriteMemPacket(uint addr, uint pid, byte[] buf)
        {
            var args = new uint[16];
            args[0] = pid;
            args[1] = addr;
            args[2] = (uint)buf.Length;
            SendPacket(1, 10, args, args[2]);
            netStream.Write(buf, 0, buf.Length);
        }

        public void SendWriteMemPacketByte(uint addr, uint pid, byte buf)
        {
            uint[] args = new uint[16];
            args[0] = pid;
            args[1] = addr;
            args[2] = 1;
            SendPacket(1, 10, args, args[2]);
            netStream.WriteByte(buf);
        }

        public void SendHeartbeatPacket()
        {
            if (tcp == null)
                return;
            lock (syncLock)
            {
                if (heartbeatSendable != 1)
                    return;

                heartbeatSendable = 0;
                SendPacket(0, 0, null, 0);
            }
        }

        public void SendHelloPacket() => SendPacket(0, 3, null, 0);

        public void SendReloadPacket() => SendPacket(0, 4, null, 0);

        public void SendEmptyPacket(uint cmd, uint arg0 = 0, uint arg1 = 0, uint arg2 = 0)
        {
            uint[] args = new uint[16];

            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            SendPacket(0, cmd, args, 0);
        }

        public void SendSaveFilePacket(string fileName, byte[] fileData)
        {
            byte[] fileNameBuf = new byte[0x200];
            Encoding.UTF8.GetBytes(fileName).CopyTo(fileNameBuf, 0);
            SendPacket(1, 1, null, (uint)(fileNameBuf.Length + fileData.Length));
            netStream.Write(fileNameBuf, 0, fileNameBuf.Length);
            netStream.Write(fileData, 0, fileData.Length);
        }

        public void Log(String msg)
        {
            OnLogArrival?.Invoke(msg);
            try { Console.WriteLine(msg); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
