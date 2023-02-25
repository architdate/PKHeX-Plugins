﻿using FluentAssertions;
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
        static LivingDexTests() => TestUtil.InitializePKHeXEnvironment();
        private static readonly GameVersion[] GetGameVersionsToTest = { SL, BD, SW, US, SN, OR, X, B2, B, Pt, E, C, RD };

        private static Dictionary<GameVersion, GenerateResult> TestLivingDex(bool includeforms, bool shiny, out bool passed)
        {
            passed = true;
            var results = new Dictionary<GameVersion, GenerateResult>();
            foreach (var s in GetGameVersionsToTest)
                results[s] = SingleSaveTest(s, includeforms, shiny, ref passed);
            return results;
        }

        private static GenerateResult SingleSaveTest(this GameVersion s, bool includeforms, bool shiny, ref bool passed)
        {
            ModLogic.IncludeForms = includeforms;
            ModLogic.SetShiny = shiny;
            var sav = SaveUtil.GetBlankSAV(s, "ALMUT");
            RecentTrainerCache.SetRecentTrainer(sav);
            var pkms = sav.GenerateLivingDex(out int attempts);
            var genned = pkms.ToList().Count;
            var val = new GenerateResult(genned == attempts, attempts, genned);
            passed = genned == attempts;
            return val;
        }

        //[Theory]
        //[InlineData(B2, true, true)]
        //[InlineData(B, true, true)]
        //[InlineData(Pt, true, true)]
#pragma warning disable xUnit1013 // Only for internal debugging
        public static void VerifyManually(GameVersion s, bool includeforms, bool shiny)
#pragma warning restore xUnit1013 // Only for internal debugging
        {
            APILegality.Timeout = 99999;
            var passed = true;
            _ = s.SingleSaveTest(includeforms, shiny, ref passed);
            passed.Should().BeTrue();
        }

        [Fact]
        public static void RunLivingDexTests()
        {
            var dir = Directory.GetCurrentDirectory();
            bool legalizer_settings = Legalizer.EnableEasterEggs;
            bool ribbon_settings = APILegality.SetAllLegalRibbons;
            int set_timeout = APILegality.Timeout;
            bool inc_forms = ModLogic.IncludeForms;
            bool set_shiny = ModLogic.SetShiny;
            Legalizer.EnableEasterEggs = false;
            APILegality.SetAllLegalRibbons = false;
            APILegality.Timeout = 99999;
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
        /// <param name="includeforms">Check if including forms</param>
        /// <param name="shiny">Check if forcing shiny</param>
        private static string Status(Dictionary<GameVersion, GenerateResult> results, bool includeforms, bool shiny)
        {
            var sb = new StringBuilder();
            sb.Append("IncludeForms: ").Append(includeforms).Append(", SetShiny: ").Append(shiny).AppendLine();
            foreach (var (key, (success, attempts, generated)) in results)
            {
                sb.Append(key).Append(" : Complete - ").Append(success).Append(" | Attempts - ").Append(attempts).Append(" | Generated - ").Append(generated).AppendLine();
            }
            return sb.ToString();
        }

        private readonly record struct GenerateResult(bool Success, int Attempts, int Generated);
    }
}
