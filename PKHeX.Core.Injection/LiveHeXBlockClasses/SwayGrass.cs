using System;
using System.Runtime.InteropServices;

namespace PKHeX.Core.Injection
{
    public class SwayGrass : ICustomBlock
    {
        private const int SWAYGRASS_BLOCK_SIZE = 0xC0;

        public readonly byte[] Data;
        public SwayGrass(byte[] data) => Data = data;

        public bool IsSwayGrassFlag { get => Data[0x00] != 0; set => Data[0x00] = (byte)(value ? 1 : 0); }
        public uint SwayZone { get => BitConverter.ToUInt32(Data, 0x04); set => BitConverter.GetBytes(value).CopyTo(Data, 0x04); }
        // 0x10 - AudioInstance[] _grassAudio
        // 0x18 - SwayGrass.GrassData work_data
        public uint ChainCount { get => BitConverter.ToUInt32(Data, 0x08); set => BitConverter.GetBytes(value).CopyTo(Data, 0x08); }
        public uint ChainSpeciesId { get => BitConverter.ToUInt32(Data, 0x0C); set => BitConverter.GetBytes(value).CopyTo(Data, 0x0C); }
        public uint ChainLevel { get => BitConverter.ToUInt32(Data, 0x10); set => BitConverter.GetBytes(value).CopyTo(Data, 0x10); }
        public bool BattleEndChainStart { get => Data[0x14] != 0; set => Data[0x14] = (byte)(value ? 1 : 0); }
        // 0x30 - GameObject RootGrass
        public bool CallSwayBGM { get => Data[0x15] != 0; set => Data[0x15] = (byte)(value ? 1 : 0); }
        public bool CallStopSwayBGM { get => Data[0x16] != 0; set => Data[0x16] = (byte)(value ? 1 : 0); }
        public Grass[] GrassData
        {
            get
            {
                var result = new Grass[4];
                for (int i = 0; i < result.Length; i++)
                {
                    var slice = Data.Slice(0x20 + i * Grass.SIZE, Grass.SIZE);
                    result[i] = slice.ToClass<Grass>();
                }
                return result;
            }

            set
            {
                for (int i = 0; i < value.Length; i++)
                    value[i].ToBytesClass().CopyTo(Data, 0x20 + i * Grass.SIZE);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = SIZE)]
        public class Grass
        {
            public const int SIZE = 0x28;

            public bool IsEnabledFlag { get; set; }
            public float EffectTime { get; set; }
            public bool IsChainActiveFlag { get; set; }
            public uint Rank { get; set; }
            public uint RandomShiny { get; set; }
            public uint RandomAbility { get; set; }
            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }
            public uint Attricode { get; set; }
        }

        private static string? GetSwayGrassPointers(LiveHeXVersion lv)
        {
            return lv switch
            {
                LiveHeXVersion.BD_v111 => "[main+4C13C60]+B8",
                LiveHeXVersion.SP_v111 => "[main+4E2AD38]+B8",
                _ => null
            };
        }

        public static byte[]? Getter(PokeSysBotMini psb)
        {
            var ptr = GetSwayGrassPointers(psb.Version);
            if (ptr == null)
                return null;
            var nx = (ICommunicatorNX)psb.com;
            var sway_grass = psb.com.ReadBytes(InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x0)), 0x40);
            var grass_one = psb.com.ReadBytes(InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x20, 0x10)), 0x28);
            var grass_two = psb.com.ReadBytes(InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x28, 0x10)), 0x28);
            var grass_three = psb.com.ReadBytes(InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x30, 0x10)), 0x28);
            var grass_four = psb.com.ReadBytes(InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x38, 0x10)), 0x28);
            var block = new byte[SWAYGRASS_BLOCK_SIZE];
            sway_grass.Slice(0x0, 0x8).CopyTo(block, 0x0);
            sway_grass.Slice(0x20, 0xD).CopyTo(block, 0x8);
            sway_grass.Slice(0x38, 0x2).CopyTo(block, 0x15);
            grass_one.CopyTo(block, 0x20 + 0 * 0x28);
            grass_two.CopyTo(block, 0x20 + 1 * 0x28);
            grass_three.CopyTo(block, 0x20 + 2 * 0x28);
            grass_four.CopyTo(block, 0x20 + 3 * 0x28);
            return block;
        }

        public void Setter(PokeSysBotMini psb, byte[] data)
        {
            var ptr = GetSwayGrassPointers(psb.Version);
            if (ptr == null)
                return;
            var nx = (ICommunicatorNX)psb.com;
            var sway_grass_addr = InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x0));
            var grass_one_addr = InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x20, 0x10));
            var grass_two_addr = InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x28, 0x10));
            var grass_three_addr = InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x30, 0x10));
            var grass_four_addr = InjectionUtil.GetPointerAddress(nx, ptr.ExtendPointer(0x8, 0x38, 0x10));
            psb.com.WriteBytes(data.Slice(0x0, 0x8), sway_grass_addr);
            psb.com.WriteBytes(data.Slice(0x8, 0xD), sway_grass_addr + 0x20);
            psb.com.WriteBytes(data.Slice(0x15, 0x2), sway_grass_addr + 0x38);
            psb.com.WriteBytes(data.Slice(0x20 + 0x28 * 0, 0x28), grass_one_addr);
            psb.com.WriteBytes(data.Slice(0x20 + 0x28 * 1, 0x28), grass_two_addr);
            psb.com.WriteBytes(data.Slice(0x20 + 0x28 * 2, 0x28), grass_three_addr);
            psb.com.WriteBytes(data.Slice(0x20 + 0x28 * 3, 0x28), grass_four_addr);
            return;
        }
    }
}
