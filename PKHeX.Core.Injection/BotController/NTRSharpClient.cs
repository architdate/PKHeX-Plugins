using System;
using System.Threading;

namespace PKHeX.Core.Injection
{
    public class NTRSharpClient : ICommunicator
    {
        public string IP = "192.168.1.106";
        public int Port = 8000;

        private int timeout = 10;

        private static NTR clientNTR = new NTR();
        private ScriptHelper sh = new ScriptHelper(clientNTR);

        public bool Connected;

        private readonly object _sync = new object();
        private byte[]? lastMemoryRead;

        public void Connect()
        {
            clientNTR.DataReady += handleDataReady;
            clientNTR.Connected += connectCheck;
            clientNTR.InfoReady += getGame;
            clientNTR.delLastLog = clientNTR.lastLog;
            sh.connect(IP, Port);
            if (clientNTR.isConnected)
                Connected = true;
        }

        bool ICommunicator.Connected { get => Connected; set => Connected = value; }
        int ICommunicator.Port { get => Port; set => Port = value; }
        string ICommunicator.IP { get => IP; set => IP = value; }

        public void Disconnect()
        {
            lock (_sync)
            {
                clientNTR.disconnect();
                Connected = false;
            }
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
                if (!Connected) Connect();

                WriteLastLog("");
                DataReadyWaiting myArgs = new DataReadyWaiting(new byte[length], handleMemoryRead, null);
                while (clientNTR.PID == -1)
                {
                    Thread.Sleep(10);
                }
                clientNTR.addwaitingForData(sh.data(offset, (uint)length, clientNTR.PID), myArgs);

                int readcount = 0;
                for (readcount = 0; readcount < timeout * 100; readcount++)
                {
                    Thread.Sleep(10);
                    if (CompareLastLog("finished"))
                        break;
                }

                byte[] result = lastMemoryRead ?? new byte[]{};
                lastMemoryRead = null;
                return result;
            }
        }

        private void WriteLastLog(string str) => clientNTR.lastlog = str;
        private bool CompareLastLog(string str) => clientNTR.lastlog.Contains(str);

        public void WriteBytes(byte[] data, uint offset)
        {
            lock (_sync)
            {
                if (!Connected) Connect();
                while (clientNTR.PID == -1)
                {
                    Thread.Sleep(10);
                }
                sh.write(offset, data, clientNTR.PID);
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

        public void getGame(object sender, EventArgs e)
        {
            InfoReadyEventArgs args = (InfoReadyEventArgs)e;

            string log = args.info;
            if (log.Contains("niji_loc"))
            {
                string splitlog = log.Substring(log.IndexOf(", pname: niji_loc") - 8, log.Length - log.IndexOf(", pname: niji_loc"));
                clientNTR.PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
                sh.write(0x3E14C0, BitConverter.GetBytes(0xE3A01000), clientNTR.PID);
            }
            else if (log.Contains("momiji"))
            {
                string splitlog = log.Substring(log.IndexOf(", pname:   momiji") - 8, log.Length - log.IndexOf(", pname:   momiji"));
                clientNTR.PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
                sh.write(0x3F3424, BitConverter.GetBytes(0xE3A01000), clientNTR.PID); // Ultra Sun  // NFC ON: E3A01001 NFC OFF: E3A01000
                sh.write(0x3F3428, BitConverter.GetBytes(0xE3A01000), clientNTR.PID); // Ultra Moon // NFC ON: E3A01001 NFC OFF: E3A01000
            }
        }

        static void handleDataReady(object sender, DataReadyEventArgs e)
        { // We move data processing to a separate thread. This way even if processing takes a long time, the netcode doesn't hang.
            DataReadyWaiting args;
            if (clientNTR.waitingForData.TryGetValue(e.seq, out args))
            {
                Array.Copy(e.data, args.data, Math.Min(e.data.Length, args.data.Length));
                Thread t = new Thread(new ParameterizedThreadStart(args.handler));
                t.Start(args);
                clientNTR.waitingForData.Remove(e.seq);
            }
        }

        public void connectCheck(object sender, EventArgs e)
        {
            sh.listprocess();
            clientNTR.isConnected = true;
        }
    }
}
