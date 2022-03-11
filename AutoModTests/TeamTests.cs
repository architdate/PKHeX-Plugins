using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        static TeamTests() => TestUtil.InitializePKHeXEnvironment();
        private static string TestPath => TestUtil.GetTestFolder("ShowdownSets");
        public static bool TargettedTesting { get; set; }

        private static Dictionary<string, int> GetFileStructures()
        {
            var depth = TargettedTesting ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            var files = Directory.GetFiles(TestPath, "*", depth);
            var result = new Dictionary<string, int>();
            foreach (var f in files)
            {
                var ext = f.EndsWith(".log") ? f.Split('_')[0][^3..] : f[^7..].Split('.')[0];
                if (ext.StartsWith("pb7"))
                    result.Add(f, -1);
                if (ext.StartsWith("pb8"))
                    result.Add(f, -2);
                if (ext.StartsWith("pa8"))
                    result.Add(f, -3);
                else if (ext.StartsWith("pk") && int.TryParse(ext.Split('k')[1], out var gen))
                    result.Add(f, gen);
                else Console.WriteLine($"Invalid file: {f}. Name does not start with 'pkX '");
            }
            return result;
        }

        private static GameVersion[] GetGameVersionsToTest(KeyValuePair<string, int> entry)
        {
            var (key, value) = entry;
            var transfer = !key.Contains("notransfer");
            return value switch
            {
                -3 => new[] { PLA },
                -2 => new[] { BD },
                -1 => transfer ? new[] { SW, GP } : new[] { GE },
                1  => transfer ? new[] { RD, C } : new[] { RD },
                2  => new[] { C },
                3  => transfer ? new[] { SW, US, SN, OR, X, B2, B, Pt, E } : new[] { E },
                4  => transfer ? new[] { SW, US, SN, OR, X, B2, B, Pt } : new[] { Pt },
                5  => transfer ? new[] { SW, US, SN, OR, X, B2 } : new[] { B2 },
                6  => transfer ? new[] { SW, US, SN, OR } : new[] { OR },
                7  => transfer ? new[] { SW, US } : new[] { US },
                8  => new[] { SW },
                _  => new[] { SW },
            };
        }

        private static Dictionary<GameVersion, Dictionary<string, RegenTemplate[]>> VerifyFile(string file, GameVersion[] saves)
        {
            var results = new Dictionary<GameVersion, Dictionary<string, RegenTemplate[]>>();
            foreach (var s in saves)
            {
                var legalsets = new List<RegenTemplate>();
                var illegalsets = new List<RegenTemplate>();
                var sav = SaveUtil.GetBlankSAV(s, "ALMUT");
                PKMConverter.SetPrimaryTrainer(sav);

                var lines = File.ReadAllLines(file).Where(z => !z.StartsWith("====="));
                var sets = ShowdownParsing.GetShowdownSets(lines).Distinct(new ShowdownSetComparator()).ToList();

                var species = Enumerable.Range(1, sav.MaxSpeciesID);
                species = sav switch
                {
                    SAV7b => species.Where(z => z is <= 151 or 808 or 809), // only include Kanto and M&M
                    SAV8BS => species.Where(z => z <= 493), // only include the sinnoh national dex
                    SAV8SWSH => species.Where(z => ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormEntry(z, 0)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(z)),
                    _ => species,
                };

                var spec = species.ToList();
                for (int i = 0; i < sets.Count; i++)
                {
                    var set = sets[i];
                    if (!spec.Contains(sets[i].Species))
                        continue;
                    try
                    {
                        Debug.Write($"Checking Set {i:000} [Species: {(Species)set.Species}] from File {file} using Save {s}: ");
                        var regen = new RegenTemplate(set, sav.Generation);
                        var pk = sav.GetLegalFromSet(regen, out _);
                        var la = new LegalityAnalysis(pk);
                        if (la.Valid)
                        {
                            Debug.WriteLine("Valid");
                            legalsets.Add(regen);
                        }
                        else
                        {
                            illegalsets.Add(regen);
                            Debug.WriteLine($"Invalid Set for {(Species)set.Species} in file {file} with set: {set.Text}");
                        }
                    }
                    catch
                    {
                        Debug.WriteLine($"Exception for {(Species)set.Species} in file {file} with set: {set.Text}");
                    }
                }
                results[s] = new Dictionary<string, RegenTemplate[]> { { "legal", legalsets.ToArray() }, { "illegal", illegalsets.ToArray() } };
            }
            return results;
        }

        public static Dictionary<string, Dictionary<GameVersion, Dictionary<string, RegenTemplate[]>>> VerifyFiles()
        {
            var result = new Dictionary<string, Dictionary<GameVersion, Dictionary<string, RegenTemplate[]>>>();
            var structure = GetFileStructures();
            foreach (var entry in structure)
            {
                var gens = GetGameVersionsToTest(entry);
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
            var testfailed = false;

            foreach (var (key, value) in results)
            {
                var fileName = $"{Path.GetFileName(key).Replace('.', '_')}{DateTime.Now:_yyyy-MM-dd-HH-mm-ss}.log";
                var path = Path.Combine(dir, "logs", fileName);
                var msg = string.Empty;
                foreach (var (gv, sets) in value)
                {
                    if (sets["illegal"].Length == 0)
                        continue;
                    msg += $"\n\n=============== GameVersion: {gv} ===============\n\n";
                    testfailed = true;
                    msg += string.Join("\n\n", sets["illegal"].Select(x => x.Text));
                }
                if (msg.Trim().Length > 0)
                    File.WriteAllText(path, msg);
            }

            var res = Status(results);
            File.WriteAllText(Path.Combine(dir, "logs", "output.txt"), res);
            testfailed.Should().BeFalse($"there were sets that could not be legally genned (Output: \n\n{res}\n\n)");
        }

        /// <summary>
        /// For Partial Debugging in Immediate Window
        /// </summary>
        /// <param name="results">partial results</param>
        /// <returns></returns>
        public static string Status(Dictionary<string, Dictionary<GameVersion, Dictionary<string, RegenTemplate[]>>> results)
        {
            var sb = new StringBuilder();
            foreach (var (key, value) in results)
            {
                var legal = 0;
                var illegal = 0;
                foreach (var (_, sets) in value)
                {
                    legal += sets["legal"].Length;
                    illegal += sets["illegal"].Length;
                }
                sb.Append(Path.GetFileName(key)).Append(" : Legal - ").Append(legal).Append(" | Illegal - ").Append(illegal).AppendLine();
            }
            return sb.ToString();
        }
    }

    internal class ShowdownSetComparator : IEqualityComparer<ShowdownSet>
    {
        public bool Equals([DisallowNull] ShowdownSet x, [DisallowNull] ShowdownSet y) => x!.Text.Trim() == y!.Text.Trim();
        public int GetHashCode([DisallowNull] ShowdownSet obj) => obj.Text.GetHashCode();
    }
}
