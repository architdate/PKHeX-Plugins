using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class FeatureTests
    {
        static FeatureTests() => TestUtil.InitializePKHeXEnvironment();

        [Fact] public static void DefaultRegenNoOT() => RegenSet.Default.HasTrainerSettings.Should().BeFalse();
        [Fact] public static void DefaultRegenNoExtra() => RegenSet.Default.HasExtraSettings.Should().BeFalse();
        [Fact] public static void DefaultRegenNoBatch() => RegenSet.Default.HasBatchSettings.Should().BeFalse();

        [Fact] public static void FallbackNotPolluted()
        {
            var original = TrainerSettings.DefaultFallback();
            var language = TrainerSettings.DefaultFallback(lang: LanguageID.Japanese);
            ReferenceEquals(original, language).Should().BeFalse();
        }

        [Fact] public static void FallbackFound()
        {
            const string set = "Ditto";
            var showdown = new RegenTemplate(new ShowdownSet(set));
            showdown.Regen.HasTrainerSettings.Should().BeFalse();
        }

        [Fact] public static void FallbackCreated()
        {
            // Creates extra requirements for generating without specifying Trainer Details
            const string set = "Ditto\nLanguage: Japanese";
            var showdown = new RegenTemplate(new ShowdownSet(set));
            showdown.Species.Should().Be((int) Species.Ditto);
            var regen = showdown.Regen;
            regen.HasTrainerSettings.Should().BeFalse();
            regen.HasExtraSettings.Should().BeTrue();
            regen.Extra.Language.Should().BeEquivalentTo(LanguageID.Japanese);

            var tr = showdown.Regen.Trainer;
            Assert.NotNull(tr);

            // Default language of fallback trainers should be English.
            tr.Language.Should().Be((int) LanguageID.English);

            // When we generate, our Extra instruction should force it to be generated as Japanese.
            var pk = tr.GetLegalFromSet(showdown, out _);
            pk.Language.Should().Be((int) LanguageID.Japanese);
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
            regen.Extra.Language.Should().BeEquivalentTo(LanguageID.English);

            var tr = showdown.Regen.Trainer;
            Assert.NotNull(tr);

            // Default language of fallback trainers should be English.
            tr.Language.Should().Be((int)LanguageID.English);

            // Register a fake trainer
            var sti = new SimpleTrainerInfo { OT = "Test", TID = 123, SID = 432 };
            TrainerSettings.Register(sti);

            // When we generate, our Extra instruction should force it to be generated as Japanese.
            var pk = tr.GetLegalFromSet(showdown, out _);
            pk.Language.Should().Be((int)LanguageID.English);
            pk.OT_Name.Should().Be(sti.OT);

            TrainerSettings.Clear();
        }
    }
}
