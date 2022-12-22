using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace PKHeX.Core.AutoMod
{
    public static class NetUtil
    {
        public static string? GetStringFromURL(string url)
        {
            try
            {
                var stream = GetStreamFromURL(url);
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            // No internet?
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        private static Stream GetStreamFromURL(string url)
        {
            using var client = new HttpClient();
            const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            client.DefaultRequestHeaders.Add("User-Agent", agent);
            var response = client.GetAsync(url).Result;
            return response.Content.ReadAsStreamAsync().Result;
        }

        private static readonly Regex LatestGitTagRegex = new("\\\"tag_name\"\\s*\\:\\s*\\\"([0-9]+\\.[0-9]+\\.[0-9]+)"); // Match `"tag_name": "18.12.02"`. Group 1 is `18.12.02`

        /// <summary>
        /// Gets the latest version of ALM according to the Github API
        /// </summary>
        /// <returns>A version representing the latest available version of PKHeX, or null if the latest version could not be determined</returns>
        public static Version? GetLatestALMVersion()
        {
            const string apiEndpoint = "https://api.github.com/repos/architdate/pkhex-plugins/releases/latest";
            var responseJson = GetStringFromURL(apiEndpoint);
            if (string.IsNullOrEmpty(responseJson))
                return null;

            // Using a regex to get the tag to avoid importing an entire JSON parsing library
            var tagMatch = LatestGitTagRegex.Match(responseJson);
            if (!tagMatch.Success)
                return null;

            var tagString = tagMatch.Groups[1].Value;
            return !Version.TryParse(tagString, out var latestVersion) ? null : latestVersion;
        }
    }
}
