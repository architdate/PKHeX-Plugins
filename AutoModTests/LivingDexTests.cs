using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using static PKHeX.Core.GameVersion;

namespace AutoModTests
{
    public static class LivingDexTests
    {
        private static GameVersion[] GetGameVersionsToTest = new[] { SW, US, SN, OR, X, B2, B, Pt, E, C, RD };

        private static Dictionary<GameVersion, Tuple<bool, int, int>> TestLivingDex(bool includeforms, bool shiny, out bool passed)
        {
            passed = true;
            var results = new Dictionary<GameVersion, Tuple<bool, int, int>>();
            foreach (var s in GetGameVersionsToTest)
                results[s] = SingleSaveTest(s, includeforms, shiny, ref passed);
            return results;
        }

        private static Tuple<bool, int, int> SingleSaveTest(this GameVersion s, bool includeforms, bool shiny, ref bool passed)
        {
            ModLogic.IncludeForms = includeforms;
            ModLogic.SetShiny = shiny;
            var sav = SaveUtil.GetBlankSAV(s, "ALMUT");
            PKMConverter.SetPrimaryTrainer(sav);
            var pkms = sav.GenerateLivingDex(out int attempts);
            var genned = pkms.Count();
            var val = new Tuple<bool, int, int>(genned == attempts, attempts, genned);
            if (genned != attempts)
                passed = false;
            return val;
        }

        //[Theory]
        //[InlineData(B2, true, true)]
        //[InlineData(B, true, true)]
        //[InlineData(Pt, true, true)]
        public static void VerifyManually(GameVersion s, bool includeforms, bool shiny)
        {
            EncounterEvent.RefreshMGDB(Path.Combine(Directory.GetCurrentDirectory(), "mgdb"));
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            APILegality.Timeout = 99999;
            var passed = true;
            var res = s.SingleSaveTest(includeforms, shiny, ref passed);
            passed.Should().BeTrue();
        }

        [Fact]
        public static void RunLivingDexTests()
        {
            EncounterEvent.RefreshMGDB(Path.Combine(Directory.GetCurrentDirectory(), "mgdb"));
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            var dir = Directory.GetCurrentDirectory();
            bool legalizer_settings = Legalizer.EnableEasterEggs;
            bool ribbon_settings = APILegality.SetAllLegalRibbons;
            int set_timeout = APILegality.Timeout;
            bool inc_forms = ModLogic.IncludeForms;
            bool set_shiny = ModLogic.SetShiny;
            Legalizer.EnableEasterEggs = false;
            APILegality.SetAllLegalRibbons = false;
            APILegality.Timeout = 99999;
            var result_cases = new List<Dictionary<GameVersion, Tuple<bool, int, int>>>();
            var result_f_f = TestLivingDex(false, false, out bool p1);
            var result_f_t = TestLivingDex(false, true, out bool p2);
            var result_t_f = TestLivingDex(true, false, out bool p3);
            var result_t_t = TestLivingDex(true, true, out bool p4);
            var passed = p1 && p2 && p3 && p4;
            Legalizer.EnableEasterEggs = legalizer_settings;
            APILegality.SetAllLegalRibbons = ribbon_settings;
            APILegality.Timeout = set_timeout;
            ModLogic.IncludeForms = inc_forms;
            ModLogic.SetShiny = set_shiny;
            Directory.CreateDirectory(Path.Combine(dir, "logs"));

            var res =   Status(result_f_f, false, false) + Environment.NewLine + 
                        Status(result_t_f, true, false) + Environment.NewLine + 
                        Status(result_f_t, false, true) + Environment.NewLine + 
                        Status(result_t_t, true, true);

            File.WriteAllText(Path.Combine(dir, "logs", "output_livingdex.txt"), res);
            passed.Should().BeTrue($"Living Dex Successfully Genned (Output: \n\n{res}\n\n)");
        }

        /// <summary>
        /// For Partial Debugging in Immediate Window
        /// </summary>
        /// <param name="results">partial results</param>
        /// <returns></returns>
        public static string Status(Dictionary<GameVersion, Tuple<bool, int, int>> results, bool includeforms, bool shiny)
        {
            var sb = new StringBuilder();
            sb.Append("IncludeForms: ").Append(includeforms).Append(", SetShiny: ").Append(shiny).AppendLine();
            foreach (var (key, value) in results)
            {
                sb.Append(key).Append(" : Complete - ").Append(value.Item1).Append(" | Attempts - ").Append(value.Item2).Append(" | Generated - ").Append(value.Item3).AppendLine();
            }
            return sb.ToString();
        }
    }
}
