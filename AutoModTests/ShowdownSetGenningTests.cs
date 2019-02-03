using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public class ShowdownSetGenningTests
    {
        public static readonly string ShowdownSetsFolder;

        static ShowdownSetGenningTests()
        {
            // Init here
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB();
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
                folder = Directory.GetParent(folder).FullName;

            ShowdownSetsFolder = Path.Combine(folder, "ShowdownSets");
        }

        private static GameVersion GetGameFromFile(string path)
        {
            string filename = Path.GetFileName(path);
            string version = filename.Split('_')[1].Split('.')[0].Trim();
            bool parsed = Enum.TryParse<GameVersion>(version, true, out var game);
            if (parsed) return game;
            else return GameVersion.GP; // Latest
        }

        [Fact]
        private static void ShowdownTextGenerate()
        {
            var path = ShowdownSetsFolder;
            var files = Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories);
            foreach (var f in files)
                VerifyFile(f);
        }
        
        public static void VerifyFile(string path)
        {
            var lines = File.ReadAllLines(path);
            var sets = ShowdownSet.GetShowdownSets(lines);
            SaveFile provider = SaveUtil.GetBlankSAV(GetGameFromFile(path), "ALM");
            foreach (var s in sets)
            {
                API.SAV = provider;
                var pk = Legalizer.GetLegalFromSet(s, out _, true);
                var la = new LegalityAnalysis(pk);
                la.Valid.Should().BeTrue($"{path}'s set for {s.Species} should generate a legal mon");
            }
        }
    }
}
