using System;
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
        void WriteBytes(ReadOnlySpan<byte> data, ulong offset);
        byte[] ReadBytes(ulong offset, int length);
        bool Connected { get; set; }
        int Port { get; set; }
        string IP { get; set; }
    }

    public interface ICommunicatorNX : ICommunicator
    {
        InjectorCommunicationType Protocol { get; set; }
        byte[] ReadBytesMain(ulong offset, int length);
        byte[] ReadBytesAbsolute(ulong offset, int length);
        ulong GetHeapBase();
        string GetBotbaseVersion();
        string GetTitleID();
        string GetGameInfo(string info);
        bool IsProgramRunning(ulong pid);
        void WriteBytesMain(ReadOnlySpan<byte> data, ulong offset);
        void WriteBytesAbsolute(ReadOnlySpan<byte> data, ulong offset);
        byte[] ReadBytesAbsoluteMulti(Dictionary<ulong, int> offsets);
    }

    public interface IPokeBlocks : ICommunicator;
}
