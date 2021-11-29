namespace PKHeX.Core.Injection
{
    public interface ICustomBlock
    {
        // static byte[]? Getter(PokeSysBotMini psb);
        void Setter(PokeSysBotMini psb, byte[] data);
    }
}
