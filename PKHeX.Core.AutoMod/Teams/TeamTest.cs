using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static PKHeX.Core.GameVersion;

namespace PKHeX.Core.AutoMod
{
    /*
     * Class to test multiple large pastebins with a mix of legal/illegal mons
     */
    public static class TeamTest
    {
        public static string TestPath => Path.Combine(Directory.GetCurrentDirectory(), "tests");

        public static Dictionary<string, int> GetFileStructures()
        {
            var files = Directory.GetFiles(TestPath, "*", SearchOption.AllDirectories);
            Dictionary<string, int> result = new Dictionary<string, int>();
            foreach (var f in files)
            {
                if (int.TryParse(f.Split('k')[f.Split('k').Length - 1].Split(' ')[0], out var gen))
                    result.Add(f, gen);
                else Console.WriteLine($"Invalid file: {f}. Name does not start with 'pkX '");
            }
            return result;
        }

        public static GameVersion[] GetGameVersionsToTest(int gen)
        {
            return gen switch
            {
                3 => new[] { SW, US, S, OR, X, B2, B, Pt, E },
                4 => new[] { SW, US, S, OR, X, B2, B, Pt },
                5 => new[] { SW, US, S, OR, X, B2 },
                6 => new[] { SW, US, S, OR },
                7 => new[] { SW, US },
                8 => new[] { SW },
                _ => new[] { SW }
            };
        }

        public static Dictionary<string, ShowdownSet[]> VerifyFile(string file, GameVersion[] saves)
        {
            var lines = File.ReadAllLines(file);
            var sets = ShowdownSet.GetShowdownSets(lines);
            var legalsets = new List<ShowdownSet>();
            var illegalsets = new List<ShowdownSet>();
            foreach (var s in saves)
            {
                var sav = SaveUtil.GetBlankSAV(s, "ALM");
                if (sav == null)
                {
                    Console.WriteLine("Null Save!");
                    return new Dictionary<string, ShowdownSet[]> { {"illegal", sets.ToArray() }, {"legal", new ShowdownSet[] { } } };
                }
                var trainer = TrainerSettings.DefaultFallback(sav.Generation);
                PKMConverter.SetPrimaryTrainer(trainer);
                var species = Enumerable.Range(1, sav.MaxSpeciesID);
                if (sav is SAV7b)
                    species = species.Where(z => z <= 151 || (z == 808 || z == 809)); // only include Kanto and M&M
                if (sav is SAV8)
                    species = species.Where(z => ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormeEntry(z, 0)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(z));

                foreach (var set in sets)
                {
                    if (!species.Contains(set.Species))
                        continue;
                    var pk = sav.GetLegalFromSet(set, out _);
                    var la = new LegalityAnalysis(pk);
                    if (la.Valid)
                        legalsets.Add(set);
                    else
                    {
                        illegalsets.Add(set);
                        Console.WriteLine($"Invalid Set for {(Species)set.Species} in file {file}");
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
    }
}
