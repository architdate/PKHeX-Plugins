using System.IO;
using FluentAssertions;
using PKHeX.Core;
using Xunit;
using AutoLegalityMod;

namespace AutoModTests
{
    public class LegalityTests
    {
        public static readonly string PKMFolder;

        static LegalityTests()
        {
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
                folder = Directory.GetParent(folder).FullName;

            PKMFolder = Path.Combine(folder, "Legality");

            API.SAV = SaveUtil.GetBlankSAV(PKX.Generation, "PKHeX");
        }

        [Fact]
        public void Test1()
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
                PKM pkm = GetPKM(file, fi);

                // double check initial state
                var la = new LegalityAnalysis(pkm);
                la.Valid.Should().Be(isValid, $"because the file '{fi.Directory.Name}\\{fi.Name}' should be {(isValid ? "Valid" : "Invalid")}");

                // try legalizing, should end up as legal
                var updated = AutomaticLegality.Legalize(pkm);
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
            pkm.Should().NotBe($"the PKM '{new FileInfo(file).Name}' should have been loaded");
            return pkm;
        }
    }
}
