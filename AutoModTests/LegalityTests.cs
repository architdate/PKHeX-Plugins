using System.IO;

using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class LegalityTests
    {
        private static readonly string PKMFolder = TestUtil.GetTestFolder("Legality");
        private static readonly SaveFile SAV = SaveUtil.GetBlankSAV(PKX.Generation, "PKHeX");

        static LegalityTests()
        {
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB();
        }

        [Fact]
        public static void TestFilesPassOrFailLegalityChecks()
        {
            var folder = PKMFolder;
            VerifyAll(folder, "Legal", true);
            VerifyAll(folder, "Illegal", false);
        }

        // ReSharper disable once UnusedParameter.Local
        private static void VerifyAll(string folder, string name, bool isValid)
        {
            var path = Path.Combine(folder, name);
            Directory.Exists(path).Should().BeTrue($"the specified test directory at '{path}' should exist");

            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fi = new FileInfo(file);
                var pk = GetPKM(file, fi);

                // double check initial state
                var la = new LegalityAnalysis(pk);
                la.Valid.Should().Be(isValid, $"because the file '{fi.Directory.Name}\\{fi.Name}' should be {(isValid ? "Valid" : "Invalid")}");

                // try legalizing, should end up as legal
                var updated = SAV.Legalize(pk);
                var la2 = new LegalityAnalysis(updated);
                la2.Valid.Should().Be(true, $"because the file '{fi.Directory.Name}\\{fi.Name}' should be legal");
            }
        }

        private static PKM GetPKM(string file, FileInfo fi)
        {
            fi.Should().NotBeNull($"the test file '{file}' should be a valid file");
            PKX.IsPKM(fi.Length).Should().BeTrue($"the test file '{file}' should have a valid file length");

            var data = File.ReadAllBytes(file);
            var format = PKX.GetPKMFormatFromExtension(file[file.Length - 1], -1);
            if (format > 10)
                format = 6;
            var pkm = PKMConverter.GetPKMfromBytes(data, prefer: format);
            pkm.Should().NotBeNull($"the PKM '{new FileInfo(file).Name}' should have been loaded");
            return pkm;
        }
    }
}
