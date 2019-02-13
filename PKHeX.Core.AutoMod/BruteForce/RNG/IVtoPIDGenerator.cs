using static RNGReporter.CompareType;

namespace RNGReporter
{
    public static class IVtoPIDGenerator
    {
        public static string[] Generate(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid, FrameType type)
        {
            var seeds = IVtoSeed.GetSeeds(hp, atk, def, spa, spd, spe, nature, tid, type);
            if (seeds.Count == 0)
                return new[] { "0", "0" };

            return new[]
            {
                seeds[0].Pid.ToString("X"),
                seeds[0].Sid.ToString(),
            };
        }

        public static string[] GenerateWishmkr(uint targetNature)
        {
            uint finalPID = 0;
            uint finalHP = 0;
            uint finalATK = 0;
            uint finalDEF = 0;
            uint finalSPA = 0;
            uint finalSPD = 0;
            uint finalSPE = 0;
            for (uint x = 0; x <= 0xFFFF; x++)
            {
                uint pid1 = Forward(x);
                uint pid2 = Forward(pid1);
                uint pid = (pid1 & 0xFFFF0000) | (pid2 >> 16);
                uint nature = pid % 25;

                if (nature == targetNature)
                {
                    uint ivs1 = Forward(pid2);
                    uint ivs2 = Forward(ivs1);
                    ivs1 >>= 16;
                    ivs2 >>= 16;
                    uint[] ivs = CreateIVs(ivs1, ivs2);
                    if (ivs != null)
                    {
                        finalPID = pid;
                        finalHP = ivs[0];
                        finalATK = ivs[1];
                        finalDEF = ivs[2];
                        finalSPA = ivs[3];
                        finalSPD = ivs[4];
                        finalSPE = ivs[5];
                        break;
                    }
                }
            }
            return new[] { finalPID.ToString("X"), finalHP.ToString(), finalATK.ToString(), finalDEF.ToString(), finalSPA.ToString(), finalSPD.ToString(), finalSPE.ToString() };
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

        public static string[] GetIVPID(uint nature, int hiddenpower, bool XD = false, string method = "")
        {
            if (method == "BACD_R")
                return GenerateWishmkr(nature);
            var generator = new FrameGenerator();
            if (XD || method == "XD")
                generator = new FrameGenerator{FrameType = FrameType.ColoXD};
            if (method == "M2")
                generator = new FrameGenerator{FrameType = FrameType.Method2};
            var frameCompare = new FrameCompare(Hptofilter(hiddenpower), nature);
            var frames = generator.Generate(frameCompare, 0, 0);
            return new[] { frames[0].Pid.ToString("X"), frames[0].Hp.ToString(), frames[0].Atk.ToString(), frames[0].Def.ToString(), frames[0].Spa.ToString(), frames[0].Spd.ToString(), frames[0].Spe.ToString() };
        }
    }
}