using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace PKHeX.Core.Injection
{
    public class UsbBotMini : ICommunicatorNX, IPokeBlocks
    {
        private const int MaximumTransferSize = 468; // byte limitation of USB-Botbase over Android for ACNHMS, assumed same here.

        public string IP = string.Empty;
        public int Port;
        public InjectorCommunicationType Protocol = InjectorCommunicationType.USB;

        private UsbDevice? SwDevice;
        private UsbEndpointReader? reader;
        private UsbEndpointWriter? writer;

        public bool Connected;

        private readonly object _sync = new();

        InjectorCommunicationType ICommunicatorNX.Protocol { get => Protocol; set => Protocol = value; }
        bool ICommunicator.Connected { get => Connected; set => Connected = SwDevice is not null; }
        int ICommunicator.Port { get => Port; set => Port = value; }
        string ICommunicator.IP { get => IP; set => IP = value; }

        /// <summary>
        /// Soft connect USB reader and writer, no persistent connection will be active due to limitations of USB-Botbase.
        /// </summary>
        public void Connect()
        {
            lock (_sync)
            {
                // Find and open the usb device.
                foreach (UsbRegistry ur in UsbDevice.AllDevices.Cast<UsbRegistry>())
                {
                    if (ur.Vid != 0x057E)
                        continue;
                    if (ur.Pid != 0x3000)
                        continue;

                    ur.DeviceProperties.TryGetValue("Address", out object? addr);
                    if (Port.ToString() != addr?.ToString())
                        continue;

                    SwDevice = ur.Device;
                }

                // If the device is open and ready
                if (SwDevice == null)
                    throw new Exception("USB device not found.");

                if (SwDevice is not IUsbDevice usb)
                    throw new Exception("Device is using a WinUSB driver. Use libusbK and create a filter.");
                if (!usb.UsbRegistryInfo.IsAlive)
                    usb.ResetDevice();

                if (SwDevice.IsOpen)
                    SwDevice.Close();
                SwDevice.Open();

                if (SwDevice is IUsbDevice wholeUsbDevice)
                {
                    // This is a "whole" USB device. Before it can be used, 
                    // the desired configuration and interface must be selected.

                    // Select config #1
                    wholeUsbDevice.SetConfiguration(1);

                    // Claim interface #0.
                    bool resagain = wholeUsbDevice.ClaimInterface(0);
                    if (!resagain)
                    {
                        wholeUsbDevice.ReleaseInterface(0);
                        wholeUsbDevice.ClaimInterface(0);
                    }
                }
                else
                {
                    Disconnect();
                    throw new Exception("Device is using WinUSB driver. Use libusbK and create a filter");
                }

                // open read write endpoints 1.
                reader = SwDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                writer = SwDevice.OpenEndpointWriter(WriteEndpointID.Ep01);

                Connected = true;
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                if (SwDevice is { IsOpen: true })
                {
                    if (SwDevice is IUsbDevice wholeUsbDevice)
                        wholeUsbDevice.ReleaseInterface(0);
                    SwDevice.Close();
                }

                reader?.Dispose();
                writer?.Dispose();
                SwDevice = null;
                Connected = false;
            }
        }

        public byte[] ReadBytes(ulong offset, int length) => ReadBytesUSB(offset, length, RWMethod.Heap);
        public void WriteBytes(byte[] data, ulong offset) => WriteBytesUSB(data, offset, RWMethod.Heap);
        public byte[] ReadBytesMain(ulong offset, int length) => ReadBytesUSB(offset, length, RWMethod.Main);
        public void WriteBytesMain(byte[] data, ulong offset) => WriteBytesUSB(data, offset, RWMethod.Main);
        public byte[] ReadBytesAbsolute(ulong offset, int length) => ReadBytesUSB(offset, length, RWMethod.Absolute);
        public void WriteBytesAbsolute(byte[] data, ulong offset) => WriteBytesUSB(data, offset, RWMethod.Absolute);
        public byte[] ReadBytesAbsoluteMulti(Dictionary<ulong, int> offsets) => ReadAbsoluteMultiUSB(offsets);

        public ulong GetHeapBase()
        {
            var cmd = SwitchCommand.GetHeapBase();
            SendInternal(cmd);
            var buffer = new byte[(8 * 2) + 1];
            var _ = ReadInternal(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

        public string GetTitleID()
        {
            SendInternal(SwitchCommand.GetTitleID(false));
            byte[] baseBytes = ReadBulkUSB();
            return BitConverter.ToUInt64(baseBytes, 0).ToString("X16").Trim();
        }

        public string GetBotbaseVersion()
        {
            SendInternal(SwitchCommand.GetBotbaseVersion(false));
            byte[] baseBytes = ReadBulkUSB();
            return Encoding.UTF8.GetString(baseBytes).Trim('\0');
        }

        public string GetGameInfo(string info)
        {
            SendInternal(SwitchCommand.GetGameInfo(info, false));
            byte[] baseBytes = ReadBulkUSB();
            return Encoding.UTF8.GetString(baseBytes).Trim('\0');
        }

        private int ReadInternal(byte[] buffer)
        {
            byte[] sizeOfReturn = new byte[4];

            //read size, no error checking as of yet, should be the required 368 bytes
            if (reader == null)
                throw new Exception("USB writer is null, you may have disconnected the device during previous function");

            reader.Read(sizeOfReturn, 5000, out _);

            //read stack
            reader.Read(buffer, 5000, out var lenVal);
            return lenVal;
        }

        private int SendInternal(byte[] buffer)
        {
            if (writer == null)
                throw new Exception("USB writer is null, you may have disconnected the device during previous function");

            uint pack = (uint)buffer.Length + 2;
            var ec = writer.Write(BitConverter.GetBytes(pack), 2000, out _);
            if (ec != ErrorCode.None)
            {
                Disconnect();
                throw new Exception(UsbDevice.LastErrorString);
            }
            ec = writer.Write(buffer, 2000, out var l);
            if (ec != ErrorCode.None)
            {
                Disconnect();
                throw new Exception(UsbDevice.LastErrorString);
            }
            return l;
        }

        public int Read(byte[] buffer)
        {
            lock (_sync)
            {
                return ReadInternal(buffer);
            }
        }

        public byte[] ReadBytesUSB(ulong offset, int length, RWMethod method)
        {
            lock (_sync)
            {
                var cmd = method switch
                {
                    RWMethod.Heap => SwitchCommand.Peek(offset, length, false),
                    RWMethod.Main => SwitchCommand.PeekMain(offset, length, false),
                    RWMethod.Absolute => SwitchCommand.PeekAbsolute(offset, length, false),
                    _ => SwitchCommand.Peek(offset, length, false),
                };

                SendInternal(cmd);
                return ReadBulkUSB();
            }
        }

        public byte[] ReadAbsoluteMultiUSB(Dictionary<ulong, int> offsets)
        {
            lock (_sync)
            {
                var cmd = SwitchCommand.PeekAbsoluteMulti(offsets, false);
                SendInternal(cmd);
                return ReadBulkUSB();
            }
        }

        private byte[] ReadBulkUSB()
        {
            // Give it time to push back.
            Thread.Sleep(1);

            if (reader == null)
                throw new Exception("USB device not found or not connected.");

            // Let usb-botbase tell us the response size.
            byte[] sizeOfReturn = new byte[4];
            reader.Read(sizeOfReturn, 5000, out _);

            int size = BitConverter.ToInt32(sizeOfReturn, 0);
            byte[] buffer = new byte[size];

            // Loop until we have read everything.
            int transfSize = 0;
            while (transfSize < size)
            {
                Thread.Sleep(1);
                var ec = reader.Read(buffer, transfSize, Math.Min(reader.ReadBufferSize, size - transfSize), 5000, out int lenVal);
                if (ec != ErrorCode.None)
                {
                    Disconnect();
                    throw new Exception(UsbDevice.LastErrorString);
                }
                transfSize += lenVal;
            }
            return buffer;
        }

        public void WriteBytesUSB(byte[] data, ulong offset, RWMethod method)
        {
            if (data.Length > MaximumTransferSize)
                WriteBytesLarge(data, offset, method);
            else WriteSmall(data, offset, method);
        }

        public void WriteSmall(byte[] data, ulong offset, RWMethod method)
        {
            lock (_sync)
            {
                var cmd = method switch
                {
                    RWMethod.Heap => SwitchCommand.Poke(offset, data, false),
                    RWMethod.Main => SwitchCommand.PokeMain(offset, data, false),
                    RWMethod.Absolute => SwitchCommand.PokeAbsolute(offset, data, false),
                    _ => SwitchCommand.Poke(offset, data, false),
                };

                SendInternal(cmd);
                Thread.Sleep(1);
            }
        }

        private void WriteBytesLarge(byte[] data, ulong offset, RWMethod method)
        {
            int byteCount = data.Length;
            for (int i = 0; i < byteCount; i += MaximumTransferSize)
            {
                var slice = SliceSafe(data, i, MaximumTransferSize);
                WriteBytesUSB(slice, offset + (ulong)i, method);
            }
        }

        // Taken from SysBot.
        private static byte[] SliceSafe(byte[] src, int offset, int length)
        {
            var delta = src.Length - offset;
            if (delta < length)
                length = delta;

            byte[] data = new byte[length];
            Buffer.BlockCopy(src, offset, data, 0, data.Length);
            return data;
        }
    }
}