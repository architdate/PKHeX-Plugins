using System;
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
            int count = smogon.Sets.Count;
            count.Should().BeGreaterThan(0);
            smogon.SetConfig.Count.Should().Be(count);
            smogon.SetText.Count.Should().Be(count);
        }
    }
}
