using System;
using System.IO;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace pkmn_ntr.Helpers
{
    public class readMemRequest
    {
        public string fileName;
        public bool isCallback;

        public readMemRequest(string fileName_)
        {
            this.fileName = fileName_;
            this.isCallback = false;
        }

        public readMemRequest()
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
        private object syncLock = new object();
        public int heartbeatSendable;
        public event EventHandler<DataReadyEventArgs> DataReady;
        public event EventHandler Connected;
        public event EventHandler<InfoReadyEventArgs> InfoReady;

        protected virtual void OnDataReady(DataReadyEventArgs e)
        {
            DataReady?.Invoke(this, e);
        }

        protected virtual void OnConnected(EventArgs e)
        {
            Connected?.Invoke(this, e);
        }

        protected virtual void OnInfoReady(InfoReadyEventArgs e)
        {
            InfoReady?.Invoke(this, e);
        }


        public delegate void logHandler(string msg);
        public event logHandler onLogArrival;
        UInt32 currentSeq;
        public Dictionary<UInt32, readMemRequest> pendingReadMem = new Dictionary<UInt32, readMemRequest>();
        public volatile int progress = -1;


        int readNetworkStream(NetworkStream stream, byte[] buf, int length)
        {
            int index = 0;
            bool useProgress = false;

            if (length > 100000)
            {
                useProgress = true;
            }
            do
            {
                if (useProgress)
                {
                    progress = (int)(((double)(index) / length) * 100);
                }
                int len = stream.Read(buf, index, length - index);
                if (len == 0)
                {
                    return 0;
                }
                index += len;
            }
            while (index < length);
            progress = -1;
            return length;
        }

        void packetRecvThreadStart()
        {
            byte[] buf = new byte[84];
            UInt32[] args = new UInt32[16];
            int ret;
            NetworkStream stream = netStream;

            while (true)
            {
                try
                {
                    ret = readNetworkStream(stream, buf, buf.Length);
                    if (ret == 0)
                    {
                        break;
                    }
                    int t = 0;
                    UInt32 magic = BitConverter.ToUInt32(buf, t);
                    t += 4;
                    UInt32 seq = BitConverter.ToUInt32(buf, t);
                    t += 4;
                    UInt32 type = BitConverter.ToUInt32(buf, t);
                    t += 4;
                    UInt32 cmd = BitConverter.ToUInt32(buf, t);
                    for (int i = 0; i < args.Length; i++)
                    {
                        t += 4;
                        args[i] = BitConverter.ToUInt32(buf, t);
                    }
                    t += 4;
                    UInt32 dataLen = BitConverter.ToUInt32(buf, t);
                    if (cmd != 0)
                    {
                        log(String.Format("packet: cmd = {0}, dataLen = {1}", cmd, dataLen));
                    }

                    if (magic != 0x12345678)
                    {
                        log(String.Format("broken protocol: magic = {0}, seq = {1}", magic, seq));
                        break;
                    }

                    if (cmd == 0)
                    {
                        if (dataLen != 0)
                        {
                            byte[] dataBuf = new byte[dataLen];
                            readNetworkStream(stream, dataBuf, dataBuf.Length);
                            string logMsg = Encoding.UTF8.GetString(dataBuf);
                            OnInfoReady(new InfoReadyEventArgs(logMsg));
                            log(logMsg);
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
                        readNetworkStream(stream, dataBuf, dataBuf.Length);
                        handlePacket(cmd, seq, dataBuf);
                    }
                }
                catch (Exception e)
                {
                    log(e.Message);
                    break;
                }
            }

            log("Server disconnected.");
            disconnect(false);
        }

        string byteToHex(byte[] datBuf, int type)
        {
            string r = "";
            for (int i = 0; i < datBuf.Length; i++)
            {
                r += datBuf[i].ToString("X2") + " ";
            }
            return r;
        }

        void handleReadMem(UInt32 seq, byte[] dataBuf)
        {
            readMemRequest requestDetails;
            if (!pendingReadMem.TryGetValue(seq, out requestDetails))
            {
                log("seq not in pending readmems, ignored");
                return;
            }
            pendingReadMem.Remove(seq);

            if (requestDetails.fileName != null)
            {
                string fileName = requestDetails.fileName;
                FileStream fs = new FileStream(fileName, FileMode.Create);
                fs.Write(dataBuf, 0, dataBuf.Length);
                fs.Close();
                log("dump saved into " + fileName + " successfully");
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
                log(byteToHex(dataBuf, 0));
            }

        }

        void handlePacket(UInt32 cmd, UInt32 seq, byte[] dataBuf)
        {
            if (cmd == 9)
            {
                handleReadMem(seq, dataBuf);
            }
        }

        public void setServer(String serverHost, int serverPort)
        {
            host = serverHost;
            port = serverPort;
        }


        public void connectToServer()
        {
            if (tcp != null)
            {
                disconnect();
            }
            try
            {
                tcp = new TcpClient();
                tcp.NoDelay = true;
                tcp.Connect(host, port);
                currentSeq = 0;
                netStream = tcp.GetStream();
                heartbeatSendable = 1;
                packetRecvThread = new Thread(new ThreadStart(packetRecvThreadStart));
                packetRecvThread.Start();
                log("Server connected.");
                OnConnected(null);
            }
            catch
            {
                MessageBox.Show("Could not connect, make sure the IP is correct, you're running NTR and you're online in-game!", "Connection Failed");
            }

        }

        public void disconnect(bool waitPacketThread = true)
        {
            try
            {
                if (tcp != null)
                {
                    tcp.Close();
                }
                if (waitPacketThread)
                {
                    if (packetRecvThread != null)
                    {
                        packetRecvThread.Join();
                    }
                }
            }
            catch (Exception ex)
            {
                log(ex.Message);
            }
            tcp = null;
        }

        public void sendPacket(UInt32 type, UInt32 cmd, UInt32[] args, UInt32 dataLen)
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
                UInt32 arg = 0;
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

        public void sendReadMemPacket(UInt32 addr, UInt32 size, UInt32 pid, string fileName)
        {
            sendEmptyPacket(9, pid, addr, size);
            pendingReadMem.Add(currentSeq, new readMemRequest(fileName));
        }

        public uint sendReadMemPacket(UInt32 addr, UInt32 size, UInt32 pid)
        {
            sendEmptyPacket(9, pid, addr, size);
            pendingReadMem.Add(currentSeq, new readMemRequest());
            return currentSeq;
        }

        public void sendWriteMemPacket(UInt32 addr, UInt32 pid, byte[] buf)
        {
            UInt32[] args = new UInt32[16];
            args[0] = pid;
            args[1] = addr;
            args[2] = (UInt32)buf.Length;
            sendPacket(1, 10, args, args[2]);
            netStream.Write(buf, 0, buf.Length);
        }

        public void sendWriteMemPacketByte(UInt32 addr, UInt32 pid, byte buf)
        {
            UInt32[] args = new UInt32[16];
            args[0] = pid;
            args[1] = addr;
            args[2] = (UInt32)1;
            sendPacket(1, 10, args, args[2]);
            netStream.WriteByte(buf);
        }


        public void sendHeartbeatPacket()
        {
            if (tcp != null)
            {
                lock (syncLock)
                {
                    if (heartbeatSendable == 1)
                    {
                        heartbeatSendable = 0;
                        sendPacket(0, 0, null, 0);
                    }
                }
            }

        }

        public void sendHelloPacket()
        {
            sendPacket(0, 3, null, 0);
        }

        public void sendReloadPacket()
        {
            sendPacket(0, 4, null, 0);
        }

        public void sendEmptyPacket(UInt32 cmd, UInt32 arg0 = 0, UInt32 arg1 = 0, UInt32 arg2 = 0)
        {
            UInt32[] args = new UInt32[16];

            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            sendPacket(0, cmd, args, 0);
        }



        public void sendSaveFilePacket(string fileName, byte[] fileData)
        {
            byte[] fileNameBuf = new byte[0x200];
            Encoding.UTF8.GetBytes(fileName).CopyTo(fileNameBuf, 0);
            sendPacket(1, 1, null, (UInt32)(fileNameBuf.Length + fileData.Length));
            netStream.Write(fileNameBuf, 0, fileNameBuf.Length);
            netStream.Write(fileData, 0, fileData.Length);
        }

        public void log(String msg)
        {
            if (onLogArrival != null)
            {
                onLogArrival.Invoke(msg);
            }
            try
            {

                Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);

            }

        }
    }
}
