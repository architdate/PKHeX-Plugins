﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PKHeX.Core.Injection
{
    public class ReadMemRequest
    {
        public readonly string FileName;
        public readonly bool IsCallback;

        public ReadMemRequest()
        {
            FileName = null;
            IsCallback = true;
        }
    }

    public class DataReadyWaiting
    {
        public readonly byte[] Data;
        public object Arguments;
        public delegate void DataHandler(object dataArguments);
        public readonly DataHandler Handler;

        public DataReadyWaiting(byte[] data, DataHandler handler, object arguments)
        {
            Data = data;
            Handler = handler;
            Arguments = arguments;
        }
    }

    public class DataReadyEventArgs : EventArgs
    {
        public readonly uint Seq;
        public readonly byte[] Data;

        public DataReadyEventArgs(uint seq, byte[] data)
        {
            Seq = seq;
            Data = data;
        }
    }

    public class InfoReadyEventArgs : EventArgs
    {
        public readonly string Info;

        public InfoReadyEventArgs(string info)
        {
            Info = info;
        }
    }

    public sealed class NTR
    {
        private string _host;
        public bool IsConnected;
        private int _port;
        private TcpClient _tcp;
        private NetworkStream _netStream;
        private Thread _packetRecvThread;
        private Thread _heartbeatThread;
        private readonly object _syncLock = new object();
        private int _heartbeatSendable;
        public event EventHandler<DataReadyEventArgs> DataReady;
        public event EventHandler Connected;
        public event EventHandler<InfoReadyEventArgs> InfoReady;
        public readonly Dictionary<uint, DataReadyWaiting> WaitingForData = new Dictionary<uint, DataReadyWaiting>();

        public delegate void LogDelegate(string l);
        public LogDelegate DelLastLog;
        public string Lastlog = "";

        public int PID = -1;

        public void lastLog(string l) => Lastlog = l;
        private void OnDataReady(DataReadyEventArgs e) => DataReady?.Invoke(this, e);
        private void OnConnected(EventArgs e) => Connected?.Invoke(this, e);
        private void OnInfoReady(InfoReadyEventArgs e) => InfoReady?.Invoke(this, e);
        public void AddWaitingForData(uint newkey, DataReadyWaiting newvalue) => WaitingForData.Add(newkey, newvalue);

        public delegate void LogHandler(string msg);
        public event LogHandler OnLogArrival;
        uint _currentSeq;
        private readonly Dictionary<uint, ReadMemRequest> pendingReadMem = new Dictionary<uint, ReadMemRequest>();
        private volatile int _progress = -1;

        public void ListProcess() => SendEmptyPacket(5);
        public uint Data(uint addr, uint size = 0x100, int pid = -1) => SendReadMemPacket(addr, size, (uint)pid);
        public void Write(uint addr, byte[] buf, int pid = -1) => SendWriteMemPacket(addr, (uint)pid, buf);

        public void Connect(string host, int port)
        {
            SetServer(host, port);
            ConnectToServer();
        }


        private int ReadNetworkStream(NetworkStream stream, byte[] buf, int length)
        {
            var index = 0;
            var useProgress = length > 100000;

            do
            {
                if (useProgress)
                {
                    _progress = (int)(((double)(index) / length) * 100);
                }
                var len = stream.Read(buf, index, length - index);
                if (len == 0)
                {
                    return 0;
                }
                index += len;
            }
            while (index < length);
            _progress = -1;
            return length;
        }

        private void SendHeartBeat()
        {
            var hbstarted = false;
            while (true)
            {
                Thread.Sleep(1000);
                if (IsConnected)
                {
                    SendHeartbeatPacket();
                    hbstarted = true;
                }
                if (hbstarted && !IsConnected)
                    break;
            }
        }

        private void PacketRecvThreadStart()
        {
            byte[] buf = new byte[84];
            uint[] args = new uint[16];
            int ret;
            var stream = _netStream;

            while (true)
            {
                try
                {
                    ret = ReadNetworkStream(stream, buf, buf.Length);
                    if (ret == 0)
                        break;
                    
                    var magic = BitConverter.ToUInt32(buf, 0);
                    var seq = BitConverter.ToUInt32(buf, 4);
                    var type = BitConverter.ToUInt32(buf, 8);
                    var cmd = BitConverter.ToUInt32(buf, 12);
                    var t = 12;
                    for (var i = 0; i < args.Length; i++)
                    {
                        t += 4;
                        args[i] = BitConverter.ToUInt32(buf, t);
                    }
                    var dataLen = BitConverter.ToUInt32(buf, t + 4);
                    if (cmd != 0)
                        Log($"packet: cmd = {cmd}, dataLen = {dataLen}");

                    if (magic != 0x12345678)
                    {
                        Log($"broken protocol: magic = {magic}, seq = {seq}");
                        break;
                    }

                    if (cmd == 0)
                    {
                        if (dataLen != 0)
                        {
                            var dataBuf = new byte[dataLen];
                            ReadNetworkStream(stream, dataBuf, dataBuf.Length);
                            var logMsg = Encoding.UTF8.GetString(dataBuf);
                            OnInfoReady(new InfoReadyEventArgs(logMsg));
                            Log(logMsg);
                        }
                        lock (_syncLock)
                        {
                            _heartbeatSendable = 1;
                        }
                        continue;
                    }
                    if (dataLen != 0)
                    {
                        var dataBuf = new byte[dataLen];
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

        private static string ByteToHex(byte[] datBuf, int type) => datBuf.Aggregate("", (current, b) => current + (b.ToString("X2") + " "));

        private void HandleReadMem(uint seq, byte[] dataBuf)
        {
            if (!pendingReadMem.TryGetValue(seq, out var requestDetails))
            {
                Log("seq not in pending readmems, ignored");
                return;
            }
            pendingReadMem.Remove(seq);

            if (requestDetails.FileName != null)
            {
                string fileName = requestDetails.FileName;
                FileStream fs = new FileStream(fileName, FileMode.Create);
                fs.Write(dataBuf, 0, dataBuf.Length);
                fs.Close();
                Log("dump saved into " + fileName + " successfully");
                return;
            }
            else if (requestDetails.IsCallback)
            {
                //Copies the data, truncates if necessary
                var dataBufCopy = new byte[dataBuf.Length];
                dataBuf.CopyTo(dataBufCopy, 0);
                var e = new DataReadyEventArgs(seq, dataBufCopy);
                OnDataReady(e);
            }
            else
            {
                Log(ByteToHex(dataBuf, 0));
            }

        }

        private void HandlePacket(uint cmd, uint seq, byte[] dataBuf)
        {
            if (cmd == 9) HandleReadMem(seq, dataBuf);
        }

        private void SetServer(string serverHost, int serverPort)
        {
            _host = serverHost;
            _port = serverPort;
        }

        private void ConnectToServer()
        {
            if (_tcp != null)
            {
                Disconnect();
            }
            try
            {
                _tcp = new TcpClient();
                _tcp.NoDelay = true;
                _tcp.Connect(_host, _port);
                _currentSeq = 0;
                _netStream = _tcp.GetStream();
                _heartbeatSendable = 1;
                _packetRecvThread = new Thread(PacketRecvThreadStart);
                _packetRecvThread.Start();
                _heartbeatThread = new Thread(SendHeartBeat);
                _heartbeatThread.Start();
                Log("Server connected.");
                OnConnected(null);
                IsConnected = true;
            }
            catch
            {
                Console.WriteLine("Could not connect, make sure the IP is correct, you're running NTR and you're online in-game!");
            }

        }

        public void Disconnect(bool waitPacketThread = true)
        {
            try
            {
                _tcp?.Close();
                if (waitPacketThread)
                {
                    if (_packetRecvThread != null)
                    {
                        _packetRecvThread.Join();
                        _heartbeatThread.Join();
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
            _tcp = null;
            IsConnected = false;
        }

        private void SendPacket(uint type, uint cmd, IReadOnlyList<uint> args, uint dataLen)
        {
            _currentSeq += 1000;
            var buf = new byte[84];
            var t = 12;
            BitConverter.GetBytes(0x12345678).CopyTo(buf, 0);
            BitConverter.GetBytes(_currentSeq).CopyTo(buf, 4);
            BitConverter.GetBytes(type).CopyTo(buf, 8);
            BitConverter.GetBytes(cmd).CopyTo(buf, 12);
            for (var i = 0; i < 16; i++)
            {
                t += 4;
                uint arg = 0;
                if (args != null)
                    arg = args[i];
                BitConverter.GetBytes(arg).CopyTo(buf, t);
            }
            BitConverter.GetBytes(dataLen).CopyTo(buf, t+4);
            _netStream.Write(buf, 0, buf.Length);
        }

        private uint SendReadMemPacket(uint addr, uint size, uint pid)
        {
            SendEmptyPacket(9, pid, addr, size);
            pendingReadMem.Add(_currentSeq, new ReadMemRequest());
            return _currentSeq;
        }

        private void SendWriteMemPacket(uint addr, uint pid, byte[] buf)
        {
            uint[] args = new uint[16];
            args[0] = pid;
            args[1] = addr;
            args[2] = (uint)buf.Length;
            SendPacket(1, 10, args, args[2]);
            _netStream.Write(buf, 0, buf.Length);
        }

        private void SendHeartbeatPacket()
        {
            if (_tcp != null)
            {
                lock (_syncLock)
                {
                    if (_heartbeatSendable == 1)
                    {
                        _heartbeatSendable = 0;
                        SendPacket(0, 0, null, 0);
                    }
                }
            }

        }

        private void SendEmptyPacket(uint cmd, uint arg0 = 0, uint arg1 = 0, uint arg2 = 0)
        {
            var args = new uint[16];

            args[0] = arg0;
            args[1] = arg1;
            args[2] = arg2;
            SendPacket(0, cmd, args, 0);
        }

        private void Log(string msg)
        {
            OnLogArrival?.Invoke(msg);
            try
            {
                DelLastLog(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
