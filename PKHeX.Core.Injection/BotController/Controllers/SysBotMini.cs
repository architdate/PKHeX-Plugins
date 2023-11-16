using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace PKHeX.Core.Injection
{
    public enum RWMethod
    {
        Heap,
        Main,
        Absolute,
    }

    public class SysBotMini : ICommunicatorNX, IPokeBlocks
    {
        public string IP = "192.168.1.65";
        public int Port = 6000;
        public InjectorCommunicationType Protocol = InjectorCommunicationType.SocketNetwork;

        public Socket Connection = new(SocketType.Stream, ProtocolType.Tcp);

        public bool Connected;

        private readonly object _sync = new();

        InjectorCommunicationType ICommunicatorNX.Protocol
        {
            get => Protocol;
            set => Protocol = value;
        }
        bool ICommunicator.Connected
        {
            get => Connected;
            set => Connected = value;
        }
        int ICommunicator.Port
        {
            get => Port;
            set => Port = value;
        }
        string ICommunicator.IP
        {
            get => IP;
            set => IP = value;
        }

        public void Connect()
        {
            lock (_sync)
            {
                Connection = new Socket(SocketType.Stream, ProtocolType.Tcp);
                Connection.Connect(IP, Port);
                Connected = true;
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                Connection.Disconnect(false);
                Connected = false;
            }
        }

        private int ReadInternal(byte[] buffer)
        {
            int br = Connection.Receive(buffer, 0, 1, SocketFlags.None);
            while (buffer[br - 1] != (byte)'\n')
                br += Connection.Receive(buffer, br, 1, SocketFlags.None);
            return br;
        }

        private int SendInternal(byte[] buffer) => Connection.Send(buffer);

        public int Read(byte[] buffer)
        {
            lock (_sync)
                return ReadInternal(buffer);
        }

        public byte[] ReadBytes(ulong offset, int length, RWMethod method)
        {
            lock (_sync)
            {
                var cmd = method switch
                {
                    RWMethod.Heap => SwitchCommand.Peek(offset, length),
                    RWMethod.Main => SwitchCommand.PeekMain(offset, length),
                    RWMethod.Absolute => SwitchCommand.PeekAbsolute(offset, length),
                    _ => SwitchCommand.Peek(offset, length),
                };

                SendInternal(cmd);

                // give it time to push data back
                Thread.Sleep((length / 256) + 100);
                var buffer = new byte[(length * 2) + 1];
                var _ = ReadInternal(buffer);
                return Decoder.ConvertHexByteStringToBytes(buffer);
            }
        }

        public byte[] ReadAbsoluteMulti(Dictionary<ulong, int> offsets)
        {
            lock (_sync)
            {
                var cmd = SwitchCommand.PeekAbsoluteMulti(offsets);
                SendInternal(cmd);

                // give it time to push data back
                var length = offsets.Values.ToArray().Sum();
                Thread.Sleep((length / 256) + 100);
                var buffer = new byte[(length * 2) + 1];
                var _ = ReadInternal(buffer);
                return Decoder.ConvertHexByteStringToBytes(buffer);
            }
        }

        public void WriteBytes(byte[] data, ulong offset, RWMethod method)
        {
            lock (_sync)
            {
                var cmd = method switch
                {
                    RWMethod.Heap => SwitchCommand.Poke(offset, data),
                    RWMethod.Main => SwitchCommand.PokeMain(offset, data),
                    RWMethod.Absolute => SwitchCommand.PokeAbsolute(offset, data),
                    _ => SwitchCommand.Poke(offset, data),
                };

                SendInternal(cmd);

                // give it time to push data back
                Thread.Sleep((data.Length / 256) + 100);
            }
        }

        public byte[] ReadLargeBytes(ulong offset, int length, RWMethod method)
        {
            const int maxlength = 344 * 30;
            var concatlist = new List<byte[]>();
            while (length > 0)
            {
                var readlength = Math.Min(maxlength, length);
                length -= readlength;
                concatlist.Add(ReadBytes(offset, readlength, method));
                offset += (ulong)readlength;
            }
            return ArrayUtil.ConcatAll(concatlist.ToArray());
        }

        public void WriteLargeBytes(byte[] data, ulong offset, RWMethod method)
        {
            const int maxlength = 344 * 30;
            if (data.Length <= maxlength)
            {
                WriteBytes(data, offset, method);
                return;
            }
            int i = 0;
            var split = data.GroupBy(_ => i++ / maxlength).Select(g => g.ToArray()).ToArray();
            foreach (var ba in split)
            {
                WriteBytes(ba, offset, method);
                offset += maxlength;
            }
        }

        public ulong GetHeapBase()
        {
            var cmd = SwitchCommand.GetHeapBase();
            SendInternal(cmd);

            var buffer = new byte[17];
            var _ = ReadInternal(buffer);
            return Convert.ToUInt64(string.Concat(buffer.Select(z => (char)z)).Trim(), 16);
        }

        public string GetTitleID()
        {
            var cmd = SwitchCommand.GetTitleID();
            SendInternal(cmd);

            var buffer = new byte[17];
            var _ = ReadInternal(buffer);
            return Encoding.ASCII.GetString(buffer).Trim();
        }

        public string GetBotbaseVersion()
        {
            var cmd = SwitchCommand.GetBotbaseVersion();
            SendInternal(cmd);

            var data = FlexRead();
            return Encoding.ASCII.GetString(data).Trim('\0');
        }

        public string GetGameInfo(string info)
        {
            var cmd = SwitchCommand.GetGameInfo(info);
            SendInternal(cmd);

            var data = FlexRead();
            return Encoding.UTF8.GetString(data).Trim(['\0', '\n']);
        }

        public bool IsProgramRunning(ulong pid)
        {
            var cmd = SwitchCommand.IsProgramRunning(pid);
            SendInternal(cmd);

            var buffer = new byte[17];
            var _ = ReadInternal(buffer);
            return ulong.TryParse(Encoding.ASCII.GetString(buffer).Trim(), out var value)
                && value == 1;
        }

        public byte[] ReadBytes(ulong offset, int length) =>
            ReadLargeBytes(offset, length, RWMethod.Heap);

        public void WriteBytes(byte[] data, ulong offset) =>
            WriteLargeBytes(data, offset, RWMethod.Heap);

        public byte[] ReadBytesMain(ulong offset, int length) =>
            ReadLargeBytes(offset, length, RWMethod.Main);

        public void WriteBytesMain(byte[] data, ulong offset) =>
            WriteLargeBytes(data, offset, RWMethod.Main);

        public byte[] ReadBytesAbsolute(ulong offset, int length) =>
            ReadLargeBytes(offset, length, RWMethod.Absolute);

        public void WriteBytesAbsolute(byte[] data, ulong offset) =>
            WriteLargeBytes(data, offset, RWMethod.Absolute);

        public byte[] ReadBytesAbsoluteMulti(Dictionary<ulong, int> offsets) =>
            ReadAbsoluteMulti(offsets);

        private byte[] FlexRead()
        {
            lock (_sync)
            {
                List<byte> flexBuffer = [];
                int available = Connection.Available;
                Connection.ReceiveTimeout = 1_000;

                do
                {
                    byte[] buffer = new byte[available];
                    Connection.Receive(buffer, available, SocketFlags.None);
                    flexBuffer.AddRange(buffer);

                    Thread.Sleep((0x1C0 / 256) + 64);
                    available = Connection.Available;
                } while (flexBuffer.Count == 0 || flexBuffer.Last() != (byte)'\n');

                Connection.ReceiveTimeout = 0;
                return [.. flexBuffer];
            }
        }
    }
}
