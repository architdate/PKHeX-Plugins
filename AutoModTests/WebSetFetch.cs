using System;
using System.Linq;
using FluentAssertions;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public class WebSetFetch
    {
        [Theory]
        [InlineData(typeof(PK7), GameVersion.MN, 3)] // Venusaur
        public void HasSmogonSets(Type t, GameVersion game, int species, int form = 0)
        {
            var blank = PKMConverter.GetBlank(t);
            blank.Version = (int) game;
            blank.Species = species;
            blank.AltForm = form;

            var smogon = new SmogonSetList(blank);
            smogon.Valid.Should().BeTrue();
            int count = smogon.Sets.Count;
            count.Should().BeGreaterThan(0);
            smogon.SetConfig.Count.Should().Be(count);
            smogon.SetText.Count.Should().Be(count);
        }

        [Theory]
        [InlineData("https://pokepast.es/73c130c81caab03e", "STING LIKE A BEE", 15, 801)] // Beedrill, Magearna
        public void HasPokePasteSets(string url, string name, params int[] speciesPresent)
        {
            var tpi = new TeamPasteInfo(url);
            tpi.Valid.Should().BeTrue();
            tpi.Title.Should().Be(name);

            var team = ShowdownUtil.ShowdownSets(tpi.Sets);
            var species = team.Select(s => s.Species).ToList();
            var hasAll = speciesPresent.All(species.Contains);
            hasAll.Should().BeTrue();
        }

        [Theory]
        [InlineData("https://pastebin.com/0x7jJvB4", "Untitled", 241, 628)] // Miltank...Braviary
        public void HasPastebinSets(string url, string name, params int[] speciesPresent)
        {
            var tpi = new TeamPasteInfo(url);
            tpi.Valid.Should().BeTrue();
            tpi.Title.Should().Be(name);

            var team = ShowdownUtil.ShowdownSets(tpi.Sets);
            var species = team.Select(s => s.Species).ToList();
            var hasAll = speciesPresent.All(species.Contains);
            hasAll.Should().BeTrue();
        }
    }
}
