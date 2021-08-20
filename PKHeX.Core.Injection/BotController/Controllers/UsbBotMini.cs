using System;
using System.Collections.Generic;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace PKHeX.Core.Injection
{
    public class UsbBotMini : ICommunicator, ICommunicatorNX
    {
        private const int MaximumTransferSize = 468; // byte limitation of USB-Botbase over Android for ACNHMS, assumed same here.

        public string IP = string.Empty;
        public int Port;

        private UsbDevice? SwDevice;
        private UsbEndpointReader? reader;
        private UsbEndpointWriter? writer;

        public bool Connected;

        private readonly object _sync = new();

        bool ICommunicator.Connected { get => Connected; set => Connected = value; }
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
                foreach (UsbRegistry ur in UsbDevice.AllDevices)
                {
                    ur.DeviceProperties.TryGetValue("Address", out object port);
                    if (ur.Vid == 1406 && ur.Pid == 12288 && Port == (int)port)
                        SwDevice = ur.Device;
                }
                //SwDevice = UsbDevice.OpenUsbDevice(MyUsbFinder);

                // If the device is open and ready
                if (SwDevice == null)
                {
                    throw new Exception("Device Not Found.");
                }

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
                Connected = false;
            }
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

        public byte[] ReadBytes(uint offset, int length)
        {
            if (length > MaximumTransferSize)
                return ReadBytesLarge(offset, length);
            lock (_sync)
            {
                var cmd = SwitchCommand.PeekRaw(offset, length);
                SendInternal(cmd);

                // give it time to push data back
                Thread.Sleep(1);

                var buffer = new byte[length];
                var _ = ReadInternal(buffer);
                //return Decoder.ConvertHexByteStringToBytes(buffer);
                return buffer;
            }
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            if (data.Length > MaximumTransferSize)
                WriteBytesLarge(data, offset);
            lock (_sync)
            {
                SendInternal(SwitchCommand.PokeRaw(offset, data));

                // give it time to push data back
                Thread.Sleep(1);
            }
        }

        private void WriteBytesLarge(byte[] data, uint offset)
        {
            int byteCount = data.Length;
            for (int i = 0; i < byteCount; i += MaximumTransferSize)
                WriteBytes(SubArray(data, i, MaximumTransferSize), offset + (uint)i);
        }

        private byte[] ReadBytesLarge(uint offset, int length)
        {
            List<byte> read = new();
            for (int i = 0; i < length; i += MaximumTransferSize)
                read.AddRange(ReadBytes(offset + (uint)i, Math.Min(MaximumTransferSize, length - i)));
            return read.ToArray();
        }

        private static T[] SubArray<T>(T[] data, int index, int length)
        {
            if (index + length > data.Length)
                length = data.Length - index;
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
