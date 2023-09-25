using System;
using System.Linq;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.Enhancements;
using Xunit;

namespace AutoModTests
{
    public static class WebSetFetch
    {
        [Theory]
        [InlineData(typeof(PK7), GameVersion.MN, (ushort)Species.Venusaur)]
        [InlineData(typeof(PK1), GameVersion.RD, (ushort)Species.Charizard)]
        [InlineData(typeof(PK3), GameVersion.E, (ushort)Species.Blastoise)]
        [InlineData(typeof(PK6), GameVersion.X, (ushort)Species.Venomoth)]
        public static void HasSmogonSets(Type t, GameVersion game, ushort species, byte form = 0)
        {
            var blank = EntityBlank.GetBlank(t);
            blank.Version = (int)game;
            blank.Species = species;
            blank.Form = form;

            var smogon = new SmogonSetList(blank);
            smogon.Valid.Should().BeTrue("Sets should exist for this setup");
            var count = smogon.Sets.Count;
            count.Should().BeGreaterThan(0, "At least one set should exist");
            smogon.SetConfig.Count
                .Should()
                .Be(count, "Unparsed text should be captured and match result count");
            smogon.SetText.Count
                .Should()
                .Be(count, "Reformatted text should be captured and match result count");
        }

        [Theory]
        [InlineData(
            "https://pokepast.es/73c130c81caab03e",
            "STING LIKE A BEE",
            (ushort)Species.Beedrill,
            (ushort)Species.Magearna
        )] // Beedrill, Magearna
        public static void HasPokePasteSets(string url, string name, params ushort[] speciesPresent)
        {
            var tpi = new TeamPasteInfo(url);
            tpi.Source.Should().Be(TeamPasteInfo.PasteSource.PokePaste);
            tpi.VerifyContents(name, speciesPresent);
        }

        [Theory]
        [InlineData(
            "https://pastebin.com/0x7jJvB4",
            "Untitled",
            (ushort)Species.Miltank,
            (ushort)Species.Braviary
        )] // Miltank...Braviary
        public static void HasPastebinSets(string url, string name, params ushort[] speciesPresent)
        {
            var tpi = new TeamPasteInfo(url);
            tpi.Source.Should().Be(TeamPasteInfo.PasteSource.Pastebin);
            tpi.VerifyContents(name, speciesPresent);
        }

        private static void VerifyContents(
            this TeamPasteInfo tpi,
            string name,
            ushort[] speciesPresent
        )
        {
            tpi.Valid.Should().BeTrue("Data should exist for this paste");
            tpi.Title.Should().Be(name, "Data should have a title present");

            var team = ShowdownUtil.ShowdownSets(tpi.Sets);
            var species = team.ConvertAll(s => s.Species);
            var hasAll = speciesPresent.All(species.Contains);
            hasAll.Should().BeTrue("Specific species are expected");
        }
    }
}
