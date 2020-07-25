using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NtrSharp;
using NtrSharp.Events;

namespace PKHeX.Core.AutoMod
{
    public class NTRMini : ICommunicator
    {
        public string IP = "192.168.1.106";
        public int Port = 8000;

        private NtrClient client;

        public bool Connected;
        private bool NTRConnected;

        private readonly object _sync = new object();
        private int PID = -1;
        private byte[] lastMemoryRead = null;

        public void Connect()
        {
            lock (_sync)
            {
                client = new NtrClient();
                client.EvtNtrStringReceived += OnProcessList;
                client.EvtReadMemoryReceived += OnReadMemory;
                client.EvtConnect += OnConnected;
                client.SetServer(IP, 8000);
                client.ConnectToServer();

                while (!Connected)
                    Thread.Sleep(100);

                GetProcessList();
            }
        }

        bool ICommunicator.Connected { get => Connected; set => Connected = value; }
        int ICommunicator.Port { get => Port; set => Port = value; }
        string ICommunicator.IP { get => IP; set => IP = value; }

        public void Disconnect()
        {
            lock (_sync)
            {
                client.Disconnect();
                NTRConnected = false;
                lastMemoryRead = null;
            }
        }

        private void GetProcessList()
        {
            client.SendEmptyPacket(5);
            while (PID == -1)
                Thread.Sleep(100);
        }

        public byte[] ReadBytes(uint offset, int length)
        {
            lock (_sync)
            {
                if (!NTRConnected) Connect();
                client.SendReadMemPacket(offset, (uint)length, (uint)PID);

                while (lastMemoryRead == null)
                    Thread.Sleep(100);

                byte[] result = lastMemoryRead;
                lastMemoryRead = null;

                Disconnect();
                return result;
            }
        }

        public void WriteBytes(byte[] data, uint offset)
        {
            lock (_sync)
            {
                if (!NTRConnected) Connect();
                client.SendWriteMemPacket(offset, (uint)PID, data);

                // give it time to push data back
                Thread.Sleep((data.Length / 256) + 100);
                Disconnect();
            }
        }

        private void OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("Connected");
            Connected = true;
            NTRConnected = true;
        }

        private void OnReadMemory(object sender, ReadMemoryReceivedEventArgs e)
        {
            Console.WriteLine("Requested Memory Read Size:" + e.Buffer.Length);
            lastMemoryRead = client.ReadMemory;
        }

        private void OnProcessList(object sender, MessageReceivedEventArgs e)
        {
            string log = e.Message;
            if (log.Contains("momiji")) // Ultra Sun and Moon
            {
                string splitlog = log.Substring(log.IndexOf(", pname:   momiji") - 8, log.Length - log.IndexOf(", pname:   momiji"));
                PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
            }

            else if (log.Contains("sango-1")) // Omega Ruby
            {
                string splitlog = log.Substring(log.IndexOf(", pname:  sango-1") - 8, log.Length - log.IndexOf(", pname:  sango-1"));
                PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
            }

            else if (log.Contains("sango-2")) // Alpha Sapphire
            {
                string splitlog = log.Substring(log.IndexOf(", pname:  sango-2") - 8, log.Length - log.IndexOf(", pname:  sango-2"));
                PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
            }

            else if (log.Contains("kujira-1")) // X
            {
                string splitlog = log.Substring(log.IndexOf(", pname: kujira-1") - 8, log.Length - log.IndexOf(", pname: kujira-1"));
                PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
            }

            else if (log.Contains("kujira-2")) // Y
            {
                string splitlog = log.Substring(log.IndexOf(", pname: kujira-2") - 8, log.Length - log.IndexOf(", pname: kujira-2"));
                PID = Convert.ToInt32("0x" + splitlog.Substring(0, 8), 16);
            }

            else
                PID = 0;

        }
    }
}
