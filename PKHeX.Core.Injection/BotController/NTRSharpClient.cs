using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NtrSharp;
using NtrSharp.Events;

namespace PKHeX.Core.Injection
{
    public class NTRSharpClient : ICommunicator
    {
        public string IP = "192.168.1.106";
        public int Port = 8000;

        private int timeout = 10;

        private NTR clientNTR = new NTR();

        public bool Connected;
        private bool NTRConnected;

        private readonly object _sync = new object();
        private int PID = -1;
        private byte[]? lastMemoryRead;

        public delegate void waitingForData(uint newkey, DataReadyWaiting newvalue);
        public delegate void writeLastLog(string str);

        public event writeLastLog onWriteLastLog;
        public event waitingForData onWaitingForData;

        public void Connect()
        {
            lock (_sync)
            {
                clientNTR.setServer(IP, Port);
                clientNTR.connectToServer();
                if (clientNTR.isConnected)
                {
                    Connected = true;
                    NTRConnected = true;
                }
            }
        }

        bool ICommunicator.Connected { get => Connected; set => Connected = value; }
        int ICommunicator.Port { get => Port; set => Port = value; }
        string ICommunicator.IP { get => IP; set => IP = value; }

        public void Disconnect()
        {
            lock (_sync)
            {
                clientNTR.disconnect();
            }
        }

        private void WriteLastLog(string str)
        {
            clientNTR.lastlog = str;
        }

        private bool CompareLastLog(string str)
        {
            return clientNTR.lastlog.Contains(str);
        }

        private void handleMemoryRead(object args_obj)
        {
            DataReadyWaiting args = (DataReadyWaiting)args_obj;
            lastMemoryRead = args.data;
        }

        public byte[] ReadBytes(uint offset, int length)
        {
            lock (_sync)
            {
                if (!NTRConnected) Connect();

                clientNTR.sendReadMemPacket(offset, (uint)length, (uint)PID);
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[length], handleMemoryRead, null);
                clientNTR.addwaitingForData(clientNTR.sh.data(offset, (uint)length, PID), myArgs);

                byte[] result = lastMemoryRead!;
                lastMemoryRead = null;
                return result;
            }
        }

        public byte[] RetryByteRead(uint offset, int length)
        {
            Disconnect();
            return ReadBytes(offset, length);
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            lock (_sync)
            {
                if (!NTRConnected) Connect();
                clientNTR.sendWriteMemPacket(offset, (uint)PID, data);

                // give it time to push data back
                Thread.Sleep((data.Length / 256) + 100);
                Disconnect();
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            Debug.WriteLine("Connected");
            Connected = true;
            NTRConnected = true;
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            Debug.WriteLine("Disconnected");
            NTRConnected = false;
            lastMemoryRead = null;
        }


        private static readonly string[] titleidstr_um = { "175e00", "1b5100" };

        private const string pname_uu = "momiji";
        private const string pname_sm = "niji_loc";
        private const string pname_or = "sango-1";
        private const string pname_as = "sango-2";
        private const string pname_x = "kujira-1";
        private const string pname_y = "kujira-2";

        private void OnProcessList(object sender, MessageReceivedEventArgs e)
        {
            string log = e.Message;
            var currver = LiveHeXVersion.SWSH_Rigel2;
            Debug.WriteLine(log);

            int GetPID(string pname)
            {
                var index = log.IndexOf($", pname: {pname,-8}", StringComparison.Ordinal);
                if (index < 0)
                    return 0;

                // PID u32 precedes the above substring; slice it out.
                var str = log.Substring(index - 8, 8);
                return (int)Util.GetHexValue(str);
            }

            if (log.Contains(pname_uu)) // Ultra Sun and Ultra Moon
            {
                PID = GetPID(pname_uu);
                currver = titleidstr_um.Any(log.Contains) ? LiveHeXVersion.UM_v12 : LiveHeXVersion.US_v12;
            }
            else if (log.Contains(pname_sm)) // Sun and Moon
            {
                PID = GetPID(pname_sm);
                currver = LiveHeXVersion.SM_v12;
            }
            else if (log.Contains(pname_or)) // Omega Ruby
            {
                PID = GetPID(pname_or);
                currver = LiveHeXVersion.ORAS;
            }
            else if (log.Contains(pname_as)) // Alpha Sapphire
            {
                PID = GetPID(pname_as);
                currver = LiveHeXVersion.ORAS;
            }
            else if (log.Contains(pname_x)) // X
            {
                PID = GetPID(pname_x);
                currver = LiveHeXVersion.XY;
            }
            else if (log.Contains(pname_y)) // X
            {
                PID = GetPID(pname_y);
                currver = LiveHeXVersion.XY;
            }
            else
            {
                PID = 0;
            }

            // Patch NFC if needed
            if (RamOffsets.NFCOffset(currver) != 0)
                WriteBytes(BitConverter.GetBytes(RamOffsets.NFCValue), RamOffsets.NFCOffset(currver));
        }
    }
}
