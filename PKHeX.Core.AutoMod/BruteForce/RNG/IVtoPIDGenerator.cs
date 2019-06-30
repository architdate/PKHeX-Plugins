using PKHeX.Core;
using static RNGReporter.CompareType;

namespace RNGReporter
{
    public static class IVtoPIDGenerator
    {
        public static uint[] Generate(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid, FrameType type)
        {
            var seeds = IVtoSeed.GetSeeds(hp, atk, def, spa, spd, spe, nature, tid, type);
            if (seeds.Count == 0)
                return new[] {0u,0u};

            return new[]
            {
                seeds[0].Pid,
                seeds[0].Sid,
            };
        }

        public static IVPID GenerateWishmkr(uint targetNature)
        {
            var ivp = new IVPID();
            for (uint x = 0; x <= 0xFFFF; x++)
            {
                uint pid1 = Forward(x);
                uint pid2 = Forward(pid1);
                uint pid = (pid1 & 0xFFFF0000) | (pid2 >> 16);
                uint nature = pid % 25;

                if (nature != targetNature)
                    continue;

                uint ivs1 = Forward(pid2);
                uint ivs2 = Forward(ivs1);
                ivs1 >>= 16;
                ivs2 >>= 16;
                uint[] ivs = CreateIVs(ivs1, ivs2);

                ivp.PID = pid;
                ivp.HP = ivs[0];
                ivp.ATK = ivs[1];
                ivp.DEF = ivs[2];
                ivp.SPA = ivs[3];
                ivp.SPD = ivs[4];
                ivp.SPE = ivs[5];
                return ivp;
            }
            return ivp;
        }

        private static uint Forward(uint seed)
        {
            return (seed * 0x41c64e6d) + 0x6073;
        }

        private static uint[] CreateIVs(uint iv1, uint ivs2)
        {
            uint[] ivs = new uint[6];

            for (int x = 0; x < 3; x++)
            {
                ivs[x] = (iv1 >> (x * 5)) & 31;
            }

            ivs[3] = (ivs2 >> 5) & 31;
            ivs[4] = (ivs2 >> 10) & 31;
            ivs[5] = ivs2 & 31;

            return ivs;
        }

        private static IVFilter Hptofilter(int hiddenpower)
        {
            switch (hiddenpower)
            {
                case 0: // Fighting
                    return new IVFilter(0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenEven, 0, HiddenEven);
                case 1: // Flying
                    return new IVFilter(0, HiddenEven, 0, HiddenEven, 0, HiddenEven, 0, HiddenEven, 0, HiddenEven, 0, HiddenOdd);
                case 2: // Poison
                    return new IVFilter(0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenEven, 0, HiddenOdd);
                case 3: // Ground
                    return new IVFilter(0, HiddenEven, 0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenEven);
                case 4: // Rock
                    return new IVFilter(0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenEven, 0, HiddenEven);
                case 5: // Bug
                    return new IVFilter(0, HiddenOdd,  0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd);
                case 6: // Ghost
                    return new IVFilter(0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd);
                case 7: // Steel
                    return new IVFilter(0, HiddenOdd,  0, HiddenEven, 0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenEven);
                case 8: // Fire
                    return new IVFilter(0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd,  0, HiddenEven);
                case 9: // Water
                    return new IVFilter(0, HiddenOdd,  0, HiddenEven, 0, HiddenEven, 0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd);
                case 10: // Grass
                    return new IVFilter(0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd);
                case 11: // Electric
                    return new IVFilter(0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenEven);
                case 12: // Psychic
                    return new IVFilter(0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenEven);
                case 13: // Ice
                    return new IVFilter(0, HiddenEven, 0, HiddenOdd,  0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd);
                case 14: // Dragon
                    return new IVFilter(0, HiddenEven, 0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd);
                case 15: // Dark
                    return new IVFilter(0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd,  0, HiddenOdd);
                default:
                    return new IVFilter();
            }
        }

        public static IVPID GetIVPID(uint nature, int hiddenpower, bool XD = false, PIDType method = PIDType.None)
        {
            if (method == PIDType.BACD_R)
                return GenerateWishmkr(nature);

            var generator = new FrameGenerator();
            if (XD || method == PIDType.CXD)
                generator.FrameType = FrameType.ColoXD;
            else if (method == PIDType.Method_2)
                generator.FrameType = FrameType.Method2;

            var frameCompare = new FrameCompare(Hptofilter(hiddenpower), nature);
            var frames = generator.Generate(frameCompare, 0, 0);
            return new IVPID(frames[0]);
        }
    }
}