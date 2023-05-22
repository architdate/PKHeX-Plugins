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

        [Fact]
        public static void TestLegal() => VerifyAll(PKMFolder, "Legal", true);

        [Fact]
        public static void TestIllegal() => VerifyAll(PKMFolder, "Illegal", false);

        // ReSharper disable once UnusedParameter.Local
        private static void VerifyAll(string folder, string name, bool isValid)
        {
            var path = Path.Combine(folder, name);
            Directory.Exists(path).Should().BeTrue($"the specified test directory at '{path}' should exist");

            var dev = APILegality.EnableDevMode;
            APILegality.EnableDevMode = true;

            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
            try
            {
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    var pk = GetPKM(file, fi);

                    // double check initial state
                    var la = new LegalityAnalysis(pk);
                    var dir = fi.Directory!;
                    la.Valid.Should().Be(isValid, $"because the file '{dir.Name}\\{fi.Name}' should be {(isValid ? "Valid" : "Invalid")}");

                    // try legalizing, should end up as legal
                    var updated = pk.Legalize();
                    var la2 = new LegalityAnalysis(updated);
                    la2.Valid.Should().Be(true, $"because the file '{dir.Name}\\{fi.Name}' should be legal");
                }
            }
            finally
            {
                APILegality.EnableDevMode = dev;
            }
        }

        private static PKM GetPKM(string file, FileInfo fi)
        {
            fi.Should().NotBeNull($"the test file '{file}' should be a valid file");
            EntityDetection.IsSizePlausible(fi.Length).Should().BeTrue($"the test file '{file}' should have a valid file length");

            var data = File.ReadAllBytes(file);
            var prefer = EntityFileExtension.GetContextFromExtension(file, EntityContext.None);
            var pkm = EntityFormat.GetFromBytes(data, prefer: prefer);
            pkm.Should().NotBeNull($"the PKM '{new FileInfo(file).Name}' should have been loaded");
            return pkm!;
        }
    }
}
