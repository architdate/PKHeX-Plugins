using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using Xunit;

namespace AutoModTests
{
    public static class FeatureTests
    {
        static FeatureTests() => TestUtil.InitializePKHeXEnvironment();

        [Fact] public static void DefaultRegenNoOT() => RegenSet.Default.HasTrainerSettings.Should().BeFalse();
        [Fact] public static void DefaultRegenNoExtra() => RegenSet.Default.HasExtraSettings.Should().BeFalse();
        [Fact] public static void DefaultRegenNoBatch() => RegenSet.Default.HasBatchSettings.Should().BeFalse();

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
            var pk = tr.GetLegalFromSet(showdown, out _);
            pk.Language.Should().Be((int)LanguageID.Japanese);
        }

        [Fact]
        public static void FallbackNotUsed()
        {
            const string set = "Ditto\nLanguage: English";
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
            var sti = new SimpleTrainerInfo { OT = "Test", TID16 = 123, SID16 = 432 };
            TrainerSettings.Register(sti);

            // When we generate, our Extra instruction should force it to be generated as Japanese.
            var pk = tr.GetLegalFromSet(showdown, out _);
            pk.Language.Should().Be((int)LanguageID.English);
            pk.OT_Name.Should().Be(sti.OT);

            TrainerSettings.Clear();
        }

        [Fact]
        public static void FallbackNotUsed2()
        {
            const string set = "Ditto\nLanguage: Japanese";
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

            // Register a fake trainer
            var sti = new SimpleTrainerInfo { OT = "Test", TID16 = 123, SID16 = 432 };
            TrainerSettings.Register(sti);

            // When we generate, our Extra instruction should force it to be generated as Japanese.
            var pk = tr.GetLegalFromSet(showdown, out _);
            pk.Language.Should().Be((int)LanguageID.Japanese);
            pk.OT_Name.Should().Be(sti.OT);

            TrainerSettings.Clear();
        }

        [Fact]
        public static void UpdateNag()
        {
            var set_mismatch = APILegality.AllowMismatch;
            var set_allowed = APILegality.LatestAllowedVersion;

            APILegality.AllowMismatch = true;
            APILegality.LatestAllowedVersion = "21.01.30";

            var currentCore = new Version("21.01.30");
            var latestCore = new Version("23.01.30");
            var currentAlm = new Version("21.01.30");
            var latestAlm = new Version("23.01.30");

            // Should not nag when any of the versions are null.
            ALMVersion.GetIsMismatch(null, currentAlm, latestCore, latestAlm).Should().BeFalse();

            // Should nag because Core version is higher than latest version allowed.
            ALMVersion.GetIsMismatch(currentCore, currentAlm, latestCore, latestAlm).Should().BeTrue();

            // Should not nag when mismatch is allowed.
            bool almUpdate = !APILegality.AllowMismatch && (latestAlm > currentAlm);
            almUpdate.Should().BeFalse();

            // Should not nag as the latest allowed version is equal to Core.
            APILegality.LatestAllowedVersion = "23.01.30";
            ALMVersion.GetIsMismatch(currentCore, currentAlm, latestCore, latestAlm).Should().BeFalse();

            // Should not nag with matching Core and ALM versions when mismatch is disallowed.
            APILegality.AllowMismatch = false;
            ALMVersion.GetIsMismatch(currentCore, currentAlm, latestCore, latestAlm).Should().BeFalse();

            // Should nag because mismatch is disallowed and versions do not match.
            ALMVersion.GetIsMismatch(latestCore, currentAlm, latestCore, latestAlm).Should().BeTrue();

            // Should nag when mismatch is disallowed.
            almUpdate = !APILegality.AllowMismatch && (latestAlm > currentAlm);
            almUpdate.Should().BeTrue();

            // Should not nag with matching versions.
            ALMVersion.GetIsMismatch(latestCore, latestCore, latestCore, latestCore).Should().BeFalse();

            APILegality.AllowMismatch = set_mismatch;
            APILegality.LatestAllowedVersion = set_allowed;
        }
    }
}
