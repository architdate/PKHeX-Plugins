using System;
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
        public readonly string? FileName;
        public readonly bool IsCallback;

        public ReadMemRequest(bool callback = true, string? fn = null)
        {
            FileName = fn;
            IsCallback = callback;
        }
    }

    public class DataReadyWaiting
    {
        public readonly byte[] Data;
        public object? Arguments;
        public delegate void DataHandler(object dataArguments);
        public readonly DataHandler Handler;

        public DataReadyWaiting(byte[] data, DataHandler handler, object? arguments)
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
        private string _host = "192.168.1.106";
        private int _port = 8000;

        public bool IsConnected;
        private TcpClient? _tcp;

        // Initialized on Connect
        private NetworkStream? _netStream;
        private Thread? _packetRecvThread;
        private Thread? _heartbeatThread;

        private int _heartbeatSendable;
        private readonly object _syncLock = new object();

        public event EventHandler<DataReadyEventArgs> DataReady;
        public event EventHandler Connected;
        public event EventHandler<InfoReadyEventArgs> InfoReady;

        private readonly Dictionary<uint, DataReadyWaiting> _waitingForData = new Dictionary<uint, DataReadyWaiting>();
        private readonly Dictionary<uint, ReadMemRequest> _pendingReadMem = new Dictionary<uint, ReadMemRequest>();

        private delegate void LogDelegate(string l);
        private readonly LogDelegate _delLastLog;
        public string Lastlog = "";

        public int PID = -1;
        private uint _currentSeq;

        public NTR()
        {
            DataReady += HandleDataReady;
            Connected += ConnectCheck;
            InfoReady += GetGame;
            _delLastLog = LastLog;
        }

        private void LastLog(string l) => Lastlog = l;
        private void OnDataReady(DataReadyEventArgs e) => DataReady.Invoke(this, e);
        private void OnConnected(EventArgs e) => Connected.Invoke(this, e);
        private void OnInfoReady(InfoReadyEventArgs e) => InfoReady.Invoke(this, e);
        private static string ByteToHex(byte[] datBuf) => datBuf.Aggregate("", (current, b) => current + (b.ToString("X2") + " "));
        public void AddWaitingForData(uint newkey, DataReadyWaiting newvalue) => _waitingForData.Add(newkey, newvalue);
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
            do
            {
                var len = stream.Read(buf, index, length - index);
                if (len == 0)
                {
                    return 0;
                }
                index += len;
            }
            while (index < length);
            return length;
        }

        private void SendHeartBeat()
        {
            var hbstarted = false;
            do
            {
                Thread.Sleep(1000);
                if (!IsConnected)
                    continue;
                SendHeartbeatPacket();
                hbstarted = true;
            }
            while (!hbstarted || IsConnected);
        }

        private void PacketRecvThreadStart()
        {
            var buf = new byte[84];
            var args = new uint[16];
            var stream = _netStream ?? throw new ArgumentNullException(nameof(_netStream));

            while (true)
            {
                try
                {
                    var ret = ReadNetworkStream(stream, buf, buf.Length);
                    if (ret == 0)
                        break;

                    var magic = BitConverter.ToUInt32(buf, 0);
                    var seq = BitConverter.ToUInt32(buf, 4);
                    //var type = BitConverter.ToUInt32(buf, 8);
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

        private void HandleReadMem(uint seq, byte[] dataBuf)
        {
            if (!_pendingReadMem.TryGetValue(seq, out var requestDetails))
            {
                Log("seq not in pending readmems, ignored");
                return;
            }
            _pendingReadMem.Remove(seq);

            if (requestDetails.FileName != null)
            {
                string fileName = requestDetails.FileName;
                FileStream fs = new FileStream(fileName, FileMode.Create);
                fs.Write(dataBuf, 0, dataBuf.Length);
                fs.Close();
                Log("dump saved into " + fileName + " successfully");
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
                Log(ByteToHex(dataBuf));
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
                Disconnect();
            try
            {
                _tcp = new TcpClient {NoDelay = true};
                _tcp.Connect(_host, _port);
                _currentSeq = 0;
                _netStream = _tcp.GetStream();
                _heartbeatSendable = 1;
                _packetRecvThread = new Thread(PacketRecvThreadStart);
                _packetRecvThread.Start();
                _heartbeatThread = new Thread(SendHeartBeat);
                _heartbeatThread.Start();
                Log("Server connected.");
                OnConnected(EventArgs.Empty);
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
                    _packetRecvThread?.Join();
                    _heartbeatThread?.Join();
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
            BitConverter.GetBytes(0x12345678).CopyTo(buf, 0);
            BitConverter.GetBytes(_currentSeq).CopyTo(buf, 4);
            BitConverter.GetBytes(type).CopyTo(buf, 8);
            BitConverter.GetBytes(cmd).CopyTo(buf, 12);
            var t = 16;
            for (var i = 0; i < 16; i++)
            {
                var val = args.Count > i ? args[i] : 0;
                BitConverter.GetBytes(val).CopyTo(buf, t);
                t += 4;
            }
            BitConverter.GetBytes(dataLen).CopyTo(buf, t + 4);
            var stream = _netStream ?? throw new ArgumentNullException(nameof(_netStream));
            stream.Write(buf, 0, buf.Length);
        }

        private uint SendReadMemPacket(uint addr, uint size, uint pid)
        {
            SendEmptyPacket(9, pid, addr, size);
            _pendingReadMem.Add(_currentSeq, new ReadMemRequest());
            return _currentSeq;
        }

        private void SendWriteMemPacket(uint addr, uint pid, byte[] buf)
        {
            uint[] args = new uint[16];
            args[0] = pid;
            args[1] = addr;
            args[2] = (uint)buf.Length;
            SendPacket(1, 10, args, args[2]);
            var stream = _netStream ?? throw new ArgumentNullException(nameof(_netStream));
            stream.Write(buf, 0, buf.Length);
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
                        SendPacket(0, 0, Array.Empty<uint>(), 0);
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
            try
            {
                _delLastLog(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void GetGame(object sender, InfoReadyEventArgs e)
        {
            var pnamestr = new[] { "kujira-1", "kujira-2", "sango-1", "sango-2", "salmon", "niji_loc", "niji_loc", "momiji", "momiji" };
            string log = e.Info;
            if (!GetPID(pnamestr, log, out PID))
                return;

            if (log.Contains("niji_loc"))
            {
                Write(0x3E14C0, BitConverter.GetBytes(0xE3A01000), PID);
            }
            else if (log.Contains("momiji"))
            {
                Write(0x3F3424, BitConverter.GetBytes(0xE3A01000), PID); // Ultra Sun  // NFC ON: E3A01001 NFC OFF: E3A01000
                Write(0x3F3428, BitConverter.GetBytes(0xE3A01000), PID); // Ultra Moon // NFC ON: E3A01001 NFC OFF: E3A01000
            }
        }

        private static bool GetPID(string[] pnamestr, string log, out int pid)
        {
            pid = 0;
            string pname;
            if ((pname = Array.Find(pnamestr, log.Contains)) == null)
                return false;
            pname = ", pname:" + pname.PadLeft(9);
            string pidaddr = log.Substring(log.IndexOf(pname, StringComparison.Ordinal) - 10, 10);
            pid = Convert.ToInt32(pidaddr, 16);
            return true;
        }

        private void HandleDataReady(object sender, DataReadyEventArgs e)
        {
            // We move data processing to a separate thread. This way even if processing takes a long time, the netcode doesn't hang.
            if (_waitingForData.TryGetValue(e.Seq, out DataReadyWaiting args))
            {
                Array.Copy(e.Data, args.Data, Math.Min(e.Data.Length, args.Data.Length));
                Thread t = new Thread(new ParameterizedThreadStart(args.Handler));
                t.Start(args);
                _waitingForData.Remove(e.Seq);
            }
        }

        private void ConnectCheck(object sender, EventArgs e)
        {
            ListProcess();
            IsConnected = true;
        }
    }
}
