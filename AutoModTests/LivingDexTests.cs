using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using static PKHeX.Core.GameVersion;

namespace AutoModTests
{
    public static class LivingDexTests
    {
        static LivingDexTests() => TestUtil.InitializePKHeXEnvironment();
        private static readonly GameVersion[] GetGameVersionsToTest = { SL, BD, PLA, SW, US, SN, OR, X, B2, B, Pt, E, C, RD };

        private static Dictionary<GameVersion, GenerateResult> TestLivingDex(bool includeforms, bool shiny, bool alpha, bool native)
        {
            var results = new Dictionary<GameVersion, GenerateResult>();
            foreach (var s in GetGameVersionsToTest)
                results[s] = SingleSaveTest(s, includeforms, shiny, alpha, native);
            return results;
        }

        private static GenerateResult SingleSaveTest(this GameVersion s, bool includeforms, bool shiny, bool alpha, bool native)
        {
            ModLogic.IncludeForms = includeforms;
            ModLogic.SetShiny = shiny;
            ModLogic.SetAlpha = alpha;
            ModLogic.NativeOnly = native;

            var sav = SaveUtil.GetBlankSAV(s, "ALMUT");
            RecentTrainerCache.SetRecentTrainer(sav);

            var expected = sav.GetExpectedDexCount(includeforms, shiny, alpha, native);
            expected.Should().NotBe(0);

            var pkms = sav.GenerateLivingDex().ToArray();
            var genned = pkms.Length;
            var val = new GenerateResult(genned == expected, expected, genned);
            return val;
        }

        //[Theory]
        //[InlineData(B2, true, true)]
        //[InlineData(B, true, true)]
        //[InlineData(Pt, true, true)]
#pragma warning disable xUnit1013 // Only for internal debugging
        public static void VerifyManually(GameVersion s, bool includeforms, bool shiny, bool alpha, bool native)
#pragma warning restore xUnit1013 // Only for internal debugging
        {
            APILegality.Timeout = 99999;
            var res = s.SingleSaveTest(includeforms, shiny, alpha, native);
            res.Success.Should().BeTrue();
        }

        [Fact]
        public static void RunLivingDexTests()
        {
            var dir = Directory.GetCurrentDirectory();
            bool legalizer_settings = Legalizer.EnableEasterEggs;
            bool ribbon_settings = APILegality.SetAllLegalRibbons;
            int set_timeout = APILegality.Timeout;
            bool mismatch = APILegality.AllowMismatch;
            bool inc_forms = ModLogic.IncludeForms;
            bool set_shiny = ModLogic.SetShiny;
            bool set_alpha = ModLogic.SetAlpha;
            bool set_native = ModLogic.NativeOnly;

            Legalizer.EnableEasterEggs = false;
            APILegality.SetAllLegalRibbons = false;
            APILegality.Timeout = 99999;
            APILegality.AllowMismatch = true;

            // SetShiny and SetAlpha should not exclude entries from the living dex.
            // new[] { includeForms, shiny, alpha, nativeOnly }
            var matrix = new bool[][]
            {
                new[] { true, false, false, false },
                new[] { true, true, false, false },
                new[] { true, true, true, false },
                new[] { true, true, true, true },

                new[] { true, false, true, false },
                new[] { true, false, false, true },
                new[] { true, false, true, true },
                new[] { true, true, false, true },

                new[] { false, true, true, true },
                new[] { false, false, true, true },
                new[] { false, false, false, true },
                new[] { false, false, false, false },

                new[] { false, true, false, true },
                new[] { false, true, true, false },
                new[] { false, true, false, false },
                new[] { false, false, true, false },
            };

            string status = string.Empty;
            var res = new List<Dictionary<GameVersion, GenerateResult>>();
            for (int row = 0; row < matrix.Length; row++)
            {
                var result = TestLivingDex(matrix[row][0], matrix[row][1], matrix[row][2], matrix[row][3]);
                status += Status(result, matrix[row][0], matrix[row][1], matrix[row][2], matrix[row][3]) + Environment.NewLine;
                res.Add(result);
            }

            Legalizer.EnableEasterEggs = legalizer_settings;
            APILegality.SetAllLegalRibbons = ribbon_settings;
            APILegality.Timeout = set_timeout;
            APILegality.AllowMismatch = mismatch;
            ModLogic.IncludeForms = inc_forms;
            ModLogic.SetShiny = set_shiny;
            ModLogic.SetAlpha = set_alpha;
            ModLogic.NativeOnly = set_native;

            Directory.CreateDirectory(Path.Combine(dir, "logs"));
            File.WriteAllText(Path.Combine(dir, "logs", "output_livingdex.txt"), status);

            var succeeded = res.All(x => x.Values.All(z => z.Success));
            succeeded.Should().BeTrue($"Living Dex Successfully Genned (Output: \n\n{status}\n\n)");
        }

        /// <summary>
        /// For Partial Debugging in Immediate Window
        /// </summary>
        /// <param name="results">partial results</param>
        /// <param name="includeforms">Check if including forms</param>
        /// <param name="shiny">Check if forcing shiny</param>
        private static string Status(Dictionary<GameVersion, GenerateResult> results, bool includeforms, bool shiny, bool alpha, bool native)
        {
            var result = $"IncludeForms: {includeforms}, Shiny: {shiny}, Alpha: {alpha}, NativeOnly: {native}\n\n";
            foreach (var (key, (success, expected, generated)) in results)
                result += $"{key} : Complete - {success} | Expected - {expected} | Generated - {generated}\n\n";
            return result;
        }

        private readonly record struct GenerateResult(bool Success, int Expected, int Generated);

        // Ideally should use purely PKHeX's methods or known total counts so that we're not verifying against ourselves.
        private static int GetExpectedDexCount(this SaveFile sav, bool includeForms, bool shiny, bool alpha, bool native)
        {
            Dictionary<ushort, List<byte>> speciesDict = new();
            var personal = sav.Personal;
            var species = Enumerable.Range(1, sav.MaxSpeciesID).Select(x => (ushort)x);
            foreach (ushort s in species)
            {
                if (!personal.IsSpeciesInGame(s))
                    continue;

                List<byte> forms = new();
                var formCount = personal[s].FormCount;
                for (byte f = 0; f < formCount; f++)
                {
                    if (!personal.IsPresentInGame(s, f) || FormInfo.IsFusedForm(s, f, sav.Generation) || FormInfo.IsBattleOnlyForm(s, f, sav.Generation)
                        || (FormInfo.IsTotemForm(s, f) && sav.Context is not EntityContext.Gen7) || FormInfo.IsLordForm(s, f, sav.Context))
                        continue;

                    var valid = sav.GetRandomEncounter(s, f, shiny, alpha, native, out PKM? pk);
                    if (pk is not null && valid && pk.Form == f && !forms.Contains(f))
                    {
                        forms.Add(f);
                        if (!includeForms)
                            break;
                    }
                }

                if (forms.Count > 0)
                    speciesDict.TryAdd(s, forms);
            }
            return includeForms ? speciesDict.Values.Sum(x => x.Count) : speciesDict.Count;
        }
    }
}
