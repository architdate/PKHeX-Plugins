using System;
using FluentAssertions;
using PKHeX.Core.AutoMod;
using Xunit;

namespace AutoModTests
{
    public static class FeatureMonitor
    {
        [Fact]
        public static void EventsGalleryExists()
        {
            var url = EventsGallery.GetMGDBDownloadURL();
            string.IsNullOrWhiteSpace(url).Should().BeFalse("Download URL should exist");

            var isUri = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
            isUri.Should().BeTrue("Download URL should be valid");
        }
    }
}