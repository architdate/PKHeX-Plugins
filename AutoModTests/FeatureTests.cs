using System;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class FeatureTests
    {
        static FeatureTests() => TestUtil.InitializePKHeXEnvironment();

        [Fact]
        public static void DefaultRegenNoOT() =>
            RegenSet.Default.HasTrainerSettings.Should().BeFalse();

        [Fact]
        public static void DefaultRegenNoExtra() =>
            RegenSet.Default.HasExtraSettings.Should().BeFalse();

        [Fact]
        public static void DefaultRegenNoBatch() =>
            RegenSet.Default.HasBatchSettings.Should().BeFalse();

        [Fact]
        public static void FallbackNotPolluted()
        {
            var original = TrainerSettings.DefaultFallback();
            var language = TrainerSettings.DefaultFallback(lang: LanguageID.Japanese);
            ReferenceEquals(original, language).Should().BeFalse();
        }

        [Fact]
        public static void FallbackFound()
        {
            const string set = "Ditto";
            var showdown = new RegenTemplate(new ShowdownSet(set));
            showdown.Regen.HasTrainerSettings.Should().BeFalse();
        }

        [Fact]
        public static void FallbackCreated()
        {
            // Creates extra requirements for generating without specifying Trainer Details
            const string set = "Ditto\nLanguage: Japanese";
            var dev = APILegality.EnableDevMode;
            APILegality.EnableDevMode = true;

            try
            {
                var showdown = new RegenTemplate(new ShowdownSet(set));
                showdown.Species.Should().Be((int)Species.Ditto);

                var regen = showdown.Regen;
                regen.HasTrainerSettings.Should().BeFalse();
                regen.HasExtraSettings.Should().BeTrue();
                regen.Extra.Language.Should().Be(LanguageID.Japanese);

                var tr = showdown.Regen.Trainer;
                Assert.NotNull(tr);

                // Default language of fallback trainers should be English.
                tr.Language.Should().Be((int)LanguageID.English);

                // When we generate, our Extra instruction should force it to be generated as Japanese.
                var almres = tr.GetLegalFromSet(showdown);
                almres.Created.Language.Should().Be((int)LanguageID.Japanese);
            }
            finally
            {
                APILegality.EnableDevMode = dev;
            }
        }

        [Fact]
        public static void FallbackNotUsed()
        {
            const string set = "Ditto\nLanguage: English";
            var dev = APILegality.EnableDevMode;
            APILegality.EnableDevMode = true;

            try
            {
                var showdown = new RegenTemplate(new ShowdownSet(set));
                showdown.Species.Should().Be((int)Species.Ditto);

                var regen = showdown.Regen;
                regen.HasTrainerSettings.Should().BeFalse();
                regen.HasExtraSettings.Should().BeTrue();
                regen.Extra.Language.Should().Be(LanguageID.English);

                var tr = showdown.Regen.Trainer;
                Assert.NotNull(tr);

                // Default language of fallback trainers should be English.
                tr.Language.Should().Be((int)LanguageID.English);

                // Register a fake trainer
                var sti = new SimpleTrainerInfo
                {
                    OT = "Test",
                    TID16 = 123,
                    SID16 = 432
                };
                TrainerSettings.Register(sti);

                // When we generate, our Extra instruction should force it to be generated as Japanese.
                var almres = tr.GetLegalFromSet(showdown);
                var pk = almres.Created;
                pk.Language.Should().Be((int)LanguageID.English);
                pk.OT_Name.Should().Be(sti.OT);

                TrainerSettings.Clear();
            }
            finally
            {
                APILegality.EnableDevMode = dev;
            }
        }

        [Fact]
        public static void FallbackNotUsed2()
        {
            const string set = "Ditto\nLanguage: Japanese";
            var dev = APILegality.EnableDevMode;
            APILegality.EnableDevMode = true;

            try
            {
                var showdown = new RegenTemplate(new ShowdownSet(set));
                showdown.Species.Should().Be((ushort)Species.Ditto);

                var regen = showdown.Regen;
                regen.HasTrainerSettings.Should().BeFalse();
                regen.HasExtraSettings.Should().BeTrue();
                regen.Extra.Language.Should().Be(LanguageID.Japanese);

                var tr = showdown.Regen.Trainer;
                Assert.NotNull(tr);

                // Default language of fallback trainers should be English.
                tr.Language.Should().Be((int)LanguageID.English);

                // Register a fake trainer
                var sti = new SimpleTrainerInfo
                {
                    OT = "Test",
                    TID16 = 123,
                    SID16 = 432
                };
                TrainerSettings.Register(sti);

                // When we generate, our Extra instruction should force it to be generated as Japanese.
                var almres = tr.GetLegalFromSet(showdown);
                var pk = almres.Created;
                pk.Language.Should().Be((int)LanguageID.Japanese);
                pk.OT_Name.Should().Be(sti.OT);

                TrainerSettings.Clear();
            }
            finally
            {
                APILegality.EnableDevMode = dev;
            }
        }

        [Fact]
        public static void UpdateNag()
        {
            var set_dev = APILegality.EnableDevMode;
            var set_allowed = APILegality.LatestAllowedVersion;

            APILegality.EnableDevMode = true;
            APILegality.LatestAllowedVersion = "21.01.30";

            var currentCore = new Version("21.01.30");
            var latestCore = new Version("23.01.30");
            var currentAlm = new Version("21.01.30");

            // Should not nag when any of the versions are null.
            ALMVersion.GetIsMismatch(null, currentAlm, latestCore).Should().BeFalse();

            // Should nag because Core version is higher than latest version allowed.
            ALMVersion.GetIsMismatch(currentCore, currentAlm, latestCore).Should().BeTrue();

            // Should not nag as the latest allowed version is equal to Core.
            APILegality.LatestAllowedVersion = "23.01.30";
            ALMVersion.GetIsMismatch(currentCore, currentAlm, latestCore).Should().BeFalse();

            // Should not nag with matching Core and ALM versions when mismatch is disallowed.
            APILegality.EnableDevMode = false;
            ALMVersion.GetIsMismatch(currentCore, currentAlm, latestCore).Should().BeFalse();

            // Should nag because mismatch is disallowed and versions do not match.
            ALMVersion.GetIsMismatch(latestCore, currentAlm, latestCore).Should().BeTrue();

            // Should not nag with matching versions.
            ALMVersion.GetIsMismatch(latestCore, latestCore, latestCore).Should().BeFalse();

            APILegality.EnableDevMode = set_dev;
            APILegality.LatestAllowedVersion = set_allowed;
        }
    }
}
