using System.Collections.Generic;

namespace RNGReporter
{
    public class IVtoPIDGenerator
    {
        public static string[] M1PID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Method1);

            if (seeds.Count == 0)
            {
                return new[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[0].Sid.ToString();
            return ans;
        }

        public static string[] M2PID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Method2);

            if (seeds.Count == 0)
            {
                return new[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[1].Sid.ToString();
            return ans;
        }

        public static string[] M4PID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Method4);

            if (seeds.Count == 0)
            {
                return new[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[1].Sid.ToString();
            return ans;
        }

        public static string[] XDPID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.ColoXD);

            if (seeds.Count == 0)
            {
                return new[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[0].Sid.ToString();
            return ans;
        }

        public static string[] ChannelPID(uint hp, uint atk, uint def, uint spa, uint spd, uint spe, uint nature, uint tid)
        {
            List<Seed> seeds =
                IVtoSeed.GetSeeds(
                    hp,
                    atk,
                    def,
                    spa,
                    spd,
                    spe,
                    nature,
                    tid,
                    FrameType.Channel);

            if (seeds.Count == 0)
            {
                return new[] { "0", "0" };
            }

            string[] ans = new string[2];
            ans[0] = seeds[0].Pid.ToString("X");
            ans[1] = seeds[0].Sid.ToString();
            return ans;
        }

        public string[] GenerateWishmkr(uint targetNature)
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

        private uint Forward(uint seed)
        {
            return (seed * 0x41c64e6d) + 0x6073;
        }

        private uint[] CreateIVs(uint iv1, uint ivs2)
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

        private static IVFilter Hptofilter(string hiddenpower)
        {
            if (hiddenpower == "dark")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "dragon")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "ice")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "psychic")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "electric")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "grass")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "water")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "fire")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "steel")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "ghost")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "bug")
            {
                return new IVFilter(0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "rock")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "ground")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven);
            }
            else if (hiddenpower == "poison")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "flying")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd);
            }
            else if (hiddenpower == "fighting")
            {
                return new IVFilter(0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenOdd, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven, 0, CompareType.HiddenEven);
            }
            else
            {
                return new IVFilter();
            }
        }

        public static string[] GetIVPID(uint nature, string hiddenpower, bool XD = false, string method = "")
        {
            var generator = new FrameGenerator();
            if (XD || method == "XD")
                generator = new FrameGenerator{FrameType = FrameType.ColoXD};
            if (method == "M2")
                generator = new FrameGenerator{FrameType = FrameType.Method2};
            if (method == "BACD_R")
            {
                IVtoPIDGenerator bacdr = new IVtoPIDGenerator();
                return bacdr.GenerateWishmkr(nature);
            }
            FrameCompare frameCompare = new FrameCompare(Hptofilter(hiddenpower), nature);
            List<Frame> frames = generator.Generate(frameCompare, 0, 0);
            //Console.WriteLine("Num frames: " + frames.Count);
            return new[] { frames[0].Pid.ToString("X"), frames[0].Hp.ToString(), frames[0].Atk.ToString(), frames[0].Def.ToString(), frames[0].Spa.ToString(), frames[0].Spd.ToString(), frames[0].Spe.ToString() };
        }
    }
}