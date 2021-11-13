using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public enum InjectorCommunicationType
    {
        SocketNetwork = 0,
        USB = 1,
    }

    public interface ICommunicator
    {
        void Connect();
        void Disconnect();
        void WriteBytes(byte[] data, ulong offset);
        byte[] ReadBytes(ulong offset, int length);
        bool Connected { get; set; }
        int Port { get; set; }
        string IP { get; set; }
    }

    public interface ICommunicatorNX : ICommunicator
    {
        byte[] ReadBytesMain(ulong offset, int length);
        byte[] ReadBytesAbsolute(ulong offset, int length);
        ulong GetHeapBase();
        void WriteBytesMain(byte[] data, ulong offset);
        void WriteBytesAbsolute(byte[] data, ulong offset);
        byte[] ReadBytesAbsoluteMulti(Dictionary<ulong, int> offsets);
    }

    public interface IPokeBlocks : ICommunicator
    {
    }
}
