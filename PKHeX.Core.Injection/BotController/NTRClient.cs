using System;
using System.Threading;

namespace PKHeX.Core.Injection
{
    public class NTRClient : ICommunicator
    {
        private string IP = "192.168.1.106";
        private int Port = 8000;

        private int timeout = 10;

        private static readonly NTR clientNTR = new NTR();

        private bool Connected;

        private readonly object _sync = new object();
        private byte[]? _lastMemoryRead;

        public void Connect()
        {
            clientNTR.DataReady += handleDataReady;
            clientNTR.Connected += ConnectCheck;
            clientNTR.InfoReady += getGame;
            clientNTR.DelLastLog = clientNTR.lastLog;
            clientNTR.Connect(IP, Port);
            if (clientNTR.IsConnected)
                Connected = true;
        }

        bool ICommunicator.Connected { get => Connected; set => Connected = value; }
        int ICommunicator.Port { get => Port; set => Port = value; }
        string ICommunicator.IP { get => IP; set => IP = value; }

        public void Disconnect()
        {
            lock (_sync)
            {
                clientNTR.Disconnect();
                Connected = false;
            }
        }

        private void HandleMemoryRead(object argsObj)
        {
            DataReadyWaiting args = (DataReadyWaiting)argsObj;
            _lastMemoryRead = args.Data;
        }

        public byte[] ReadBytes(uint offset, int length)
        {
            lock (_sync)
            {
                if (!Connected) Connect();

                WriteLastLog("");
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[length], HandleMemoryRead, null);
                while (clientNTR.PID == -1)
                {
                    Thread.Sleep(10);
                }
                clientNTR.AddWaitingForData(clientNTR.Data(offset, (uint)length, clientNTR.PID), myArgs);

                int readcount = 0;
                for (readcount = 0; readcount < timeout * 100; readcount++)
                {
                    Thread.Sleep(10);
                    if (CompareLastLog("finished"))
                        break;
                }

                byte[] result = _lastMemoryRead ?? new byte[]{};
                _lastMemoryRead = null;
                return result;
            }
        }

        private static void WriteLastLog(string str) => clientNTR.Lastlog = str;
        private static bool CompareLastLog(string str) => clientNTR.Lastlog.Contains(str);

        public void WriteBytes(byte[] data, uint offset)
        {
            lock (_sync)
            {
                if (!Connected) Connect();
                while (clientNTR.PID == -1)
                {
                    Thread.Sleep(10);
                }
                clientNTR.Write(offset, data, clientNTR.PID);
                int waittimeout;
                for (waittimeout = 0; waittimeout < timeout * 100; waittimeout++)
                {
                    WriteLastLog("");
                    Thread.Sleep(10);
                    if (CompareLastLog("finished"))
                        break;
                }
            }
        }

        private void getGame(object sender, EventArgs e)
        {
            var args = (InfoReadyEventArgs)e;

            string log = args.Info;
            if (log.Contains("niji_loc"))
            {
                string splitlog = log.Substring(log.IndexOf(", pname: niji_loc") - 8, log.Length - log.IndexOf(", pname: niji_loc"));
                clientNTR.PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
                clientNTR.Write(0x3E14C0, BitConverter.GetBytes(0xE3A01000), clientNTR.PID);
            }
            else if (log.Contains("momiji"))
            {
                string splitlog = log.Substring(log.IndexOf(", pname:   momiji") - 8, log.Length - log.IndexOf(", pname:   momiji"));
                clientNTR.PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
                clientNTR.Write(0x3F3424, BitConverter.GetBytes(0xE3A01000), clientNTR.PID); // Ultra Sun  // NFC ON: E3A01001 NFC OFF: E3A01000
                clientNTR.Write(0x3F3428, BitConverter.GetBytes(0xE3A01000), clientNTR.PID); // Ultra Moon // NFC ON: E3A01001 NFC OFF: E3A01000
            }
        }

        static void handleDataReady(object sender, DataReadyEventArgs e)
        { // We move data processing to a separate thread. This way even if processing takes a long time, the netcode doesn't hang.
            DataReadyWaiting args;
            if (clientNTR.WaitingForData.TryGetValue(e.Seq, out args))
            {
                Array.Copy(e.Data, args.Data, Math.Min(e.Data.Length, args.Data.Length));
                Thread t = new Thread(new ParameterizedThreadStart(args.Handler));
                t.Start(args);
                clientNTR.WaitingForData.Remove(e.Seq);
            }
        }

        private static void ConnectCheck(object sender, EventArgs e)
        {
            clientNTR.ListProcess();
            clientNTR.IsConnected = true;
        }
    }
}
