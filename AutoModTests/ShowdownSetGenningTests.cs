using System;
using System.IO;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class ShowdownSetGenningTests
    {
        private static readonly string ShowdownSetsFolder = TestUtil.GetTestFolder("ShowdownSets");

        static ShowdownSetGenningTests()
        {
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB();
        }

        private static GameVersion GetGameFromFile(string path)
        {
            var filename = Path.GetFileNameWithoutExtension(path);
            var split = filename.Split('_');
            var version = split[1].Trim();
            Enum.TryParse<GameVersion>(version, true, out var game).Should().BeTrue();
            return game;
        }

        [Fact]
        private static void ShowdownTextGenerate()
        {
            var path = ShowdownSetsFolder;
            var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            foreach (var f in files)
                VerifyFile(f);
        }

        private static void VerifyFile(string path)
        {
            var lines = File.ReadAllLines(path);
            var sets = ShowdownSet.GetShowdownSets(lines);
            var game = GetGameFromFile(path);
            var sav = SaveUtil.GetBlankSAV(game, "ALM");
            sav.Should().NotBeNull();
            foreach (var s in sets)
            {
                var blank = sav.BlankPKM;
                var pk = sav.GetLegalFromSet(s, out _, true);
                var la = new LegalityAnalysis(pk);
                la.Valid.Should().BeTrue($"{path}'s set for {GameInfo.Strings.Species[s.Species]} should generate a legal mon");
            }
        }
    }
}
