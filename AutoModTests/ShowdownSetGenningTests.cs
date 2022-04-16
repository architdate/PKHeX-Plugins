using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class ShowdownSetGenningTests
    {
        static ShowdownSetGenningTests() => TestUtil.InitializePKHeXEnvironment();

        [Theory]
        [InlineData(7, Meowstic)]
        [InlineData(7, Darkrai)]
        [InlineData(5, Genesect)]
        public static void VerifyManually(int gen, string txt)
        {
            var sav = SaveUtil.GetBlankSAV(gen, "ALM");
            TrainerSettings.Register(sav);
            var trainer = TrainerSettings.GetSavedTrainerData(gen);
            RecentTrainerCache.SetRecentTrainer(trainer);
            var set = new ShowdownSet(txt);
            var pkm = sav.GetLegalFromSet(set, out _);
            var la = new LegalityAnalysis(pkm);
            la.Valid.Should().BeTrue();
        }

        private const string Darkrai =
@"Darkrai
IVs: 7 Atk
Ability: Bad Dreams
Shiny: Yes
Timid Nature
- Hypnosis
- Feint Attack
- Nightmare
- Double Team";

        private const string Genesect =
@"Genesect
Ability: Download
Shiny: Yes
Hasty Nature
- Extreme Speed
- Techno Blast
- Blaze Kick
- Shift Gear";

        private const string Meowstic =
@"Meowstic-F @ Life Orb
Ability: Competitive
EVs: 4 Def / 252 SpA / 252 Spe
Timid Nature
- Psyshock
- Signal Beam
- Hidden Power Ground
- Calm Mind";
    }
}
