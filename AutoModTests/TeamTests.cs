using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;
using static PKHeX.Core.GameVersion;

namespace AutoModTests
{
    /// <summary>
    /// Class to test multiple large pastebins with a mix of legal/illegal mons
    /// </summary>
    public static class TeamTests
    {
        private static string TestPath => Path.Combine(Directory.GetCurrentDirectory(), "tests");

        private static Dictionary<string, int> GetFileStructures()
        {
            var files = Directory.GetFiles(TestPath, "*", SearchOption.AllDirectories);
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (var f in files)
            {
                var ext = f[^7..].Split('.')[0];
                if (ext.StartsWith("pb"))
                    result.Add(f, -1);
                else if (ext.StartsWith("pk") && int.TryParse(ext.Split('k')[1], out var gen))
                    result.Add(f, gen);
                else Console.WriteLine($"Invalid file: {f}. Name does not start with 'pkX '");
            }
            return result;
        }

        private static GameVersion[] GetGameVersionsToTest(int gen)
        {
            return gen switch
            {
                -1 => new[] { GP },
                3 => new[] { SW, US, S, OR, X, B2, B, Pt, E },
                4 => new[] { SW, US, S, OR, X, B2, B, Pt },
                5 => new[] { SW, US, S, OR, X, B2 },
                6 => new[] { SW, US, S, OR },
                7 => new[] { SW, US },
                8 => new[] { SW },
                _ => new[] { SW }
            };
        }

        private static Dictionary<string, ShowdownSet[]> VerifyFile(string file, GameVersion[] saves)
        {
            var lines = File.ReadAllLines(file);
            var sets = ShowdownSet.GetShowdownSets(lines).ToList();
            var legalsets = new List<ShowdownSet>();
            var illegalsets = new List<ShowdownSet>();
            foreach (var s in saves)
            {
                var sav = SaveUtil.GetBlankSAV(s, "ALM");
                var trainer = TrainerSettings.DefaultFallback(sav.Generation);
                PKMConverter.SetPrimaryTrainer(trainer);
                var species = Enumerable.Range(1, sav.MaxSpeciesID);
                species = sav switch
                {
                    SAV7b _ => species.Where(z => z <= 151 || (z == 808 || z == 809)), // only include Kanto and M&M
                    SAV8 _ => species.Where(z => ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormEntry(z, 0)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(z)),
                    _ => species
                };

                var spec = species.ToList();
                foreach (var set in sets)
                {
                    if (!spec.Contains(set.Species))
                        continue;
                    try
                    {
                        var pk = sav.GetLegalFromSet(set, out _);
                        var la = new LegalityAnalysis(pk);
                        if (la.Valid)
                        {
                            legalsets.Add(set);
                        }
                        else
                        {
                            illegalsets.Add(set);
                            Console.WriteLine($"Invalid Set for {(Species)set.Species} in file {file} with set: {set.Text}");
                        }
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        Console.WriteLine($"Exception for {(Species)set.Species} in file {file} with set: {set.Text}");
                    }
                }
            }
            return new Dictionary<string, ShowdownSet[]> { { "legal", legalsets.ToArray() }, { "illegal", illegalsets.ToArray() } };
        }

        public static Dictionary<string, Dictionary<string, ShowdownSet[]>> VerifyFiles()
        {
            var result = new Dictionary<string, Dictionary<string, ShowdownSet[]>>();
            var structure = GetFileStructures();
            foreach (var entry in structure)
            {
                var gens = GetGameVersionsToTest(entry.Value);
                var file = entry.Key;
                var res = VerifyFile(file, gens);
                result.Add(file, res);
            }
            return result;
        }

        [Fact]
        public static void RunTeamTests()
        {
            Directory.Exists(TestPath).Should().BeTrue();
            var dir = Directory.GetCurrentDirectory();

            var results = VerifyFiles();
            Directory.CreateDirectory(Path.Combine(dir, "logs"));

            foreach (var (key, value) in results)
            {
                var fileName = $"{Path.GetFileName(key).Replace('.', '_')}{DateTime.Now:_yyyy-MM-dd-HH-mm-ss}.log";
                var path = Path.Combine(dir, "logs", fileName);
                var msg = string.Join("\n\n", value["illegal"].Select(x => x.Text));
                File.WriteAllText(path, msg);
            }

            var sb = new StringBuilder();
            foreach (var (key, value) in results)
                sb.Append(Path.GetFileName(key)).Append(" : Legal - ").Append(value["legal"].Length).Append(" | Illegal - ").Append(value["illegal"].Length).AppendLine();

            File.WriteAllText(Path.Combine(dir, "logs", "output.txt"), sb.ToString());
        }
    }
}
