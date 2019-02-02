using System;
using System.Linq;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class WebSetFetch
    {
        [Theory]
        [InlineData(typeof(PK7), GameVersion.MN, 3)] // Venusaur
        public static void HasSmogonSets(Type t, GameVersion game, int species, int form = 0)
        {
            var blank = PKMConverter.GetBlank(t);
            blank.Version = (int) game;
            blank.Species = species;
            blank.AltForm = form;

            var smogon = new SmogonSetList(blank);
            smogon.Valid.Should().BeTrue("Sets should exist for this setup");
            int count = smogon.Sets.Count;
            count.Should().BeGreaterThan(0, "At least one set should exist");
            smogon.SetConfig.Count.Should().Be(count, "Unparsed text should be captured and match result count");
            smogon.SetText.Count.Should().Be(count, "Reformatted text should be captured and match result count");
        }

        [Theory]
        [InlineData("https://pokepast.es/73c130c81caab03e", "STING LIKE A BEE", 15, 801)] // Beedrill, Magearna
        public static void HasPokePasteSets(string url, string name, params int[] speciesPresent)
        {
            var tpi = new TeamPasteInfo(url);
            tpi.Source.Should().Be(TeamPasteInfo.PasteSource.PokePaste);
            tpi.VerifyContents(name, speciesPresent);
        }

        [Theory]
        [InlineData("https://pastebin.com/0x7jJvB4", "Untitled", 241, 628)] // Miltank...Braviary
        public static void HasPastebinSets(string url, string name, params int[] speciesPresent)
        {
            var tpi = new TeamPasteInfo(url);
            tpi.Source.Should().Be(TeamPasteInfo.PasteSource.Pastebin);
            tpi.VerifyContents(name, speciesPresent);
        }

        private static void VerifyContents(this TeamPasteInfo tpi, string name, int[] speciesPresent)
        {
            tpi.Valid.Should().BeTrue("Data should exist for this paste");
            tpi.Title.Should().Be(name, "Data should have a title present");

            var team = ShowdownUtil.ShowdownSets(tpi.Sets);
            var species = team.Select(s => s.Species).ToList();
            var hasAll = speciesPresent.All(species.Contains);
            hasAll.Should().BeTrue("Specific species are expected");
        }
    }
}
