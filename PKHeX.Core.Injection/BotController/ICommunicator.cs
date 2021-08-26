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
        void WriteBytes(byte[] data, uint offset);
        byte[] ReadBytes(uint offset, int length);
        bool Connected { get; set; }
        int Port { get; set; }
        string IP { get; set; }
    }

    public interface ICommunicatorNX : ICommunicator
    {
        byte[] ReadBytesMain(ulong offset, int length);
        byte[] ReadBytesAbsolute(ulong offset, int length);
        void WriteBytesMain(byte[] data, uint offset);
        void WriteBytesAbsolute(byte[] data, ulong offset);
    }

    public interface IPokeBlocks : ICommunicator
    {
    }
}
