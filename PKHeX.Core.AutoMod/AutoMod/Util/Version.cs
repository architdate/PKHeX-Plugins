using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;

namespace PKHeX.Core.AutoMod
{
    public static partial class ALMVersion
    {
        [GeneratedRegex("\\\"tag_name\"\\s*\\:\\s*\\\"([0-9]+\\.[0-9]+\\.[0-9]+)")]
        private static partial Regex GetLatestGitTag();
        private static readonly Regex LatestGitTagRegex = GetLatestGitTag(); // Match `"tag_name": "18.12.02"`. Group 1 is `18.12.02`
        private static readonly Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// Gets the current version of the specified assembly.
        /// </summary>
        /// <returns>A version representing the current version of the specified assembly, or null if the assembly cannot be found or has no version available.</returns>
        public static Version? GetCurrentVersion(string assemblyName)
        {
            var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == assemblyName);
            return assembly?.GetName().Version;
        }

        /// <summary>
        /// Gets the latest version of ALM according to the Github API
        /// </summary>
        /// <returns>A version representing the latest available version of PKHeX, or null if the latest version could not be determined.</returns>
        public static async Task<Version?> GetLatestALMVersion(CancellationToken token)
        {
            const string apiEndpoint = "https://api.github.com/repos/architdate/pkhex-plugins/releases/latest";
            var responseJson = await GetStringFromURL(apiEndpoint, token).ConfigureAwait(false);
            if (string.IsNullOrEmpty(responseJson))
                return null;

            // Using a regex to get the tag to avoid importing an entire JSON parsing library
            var tagMatch = LatestGitTagRegex.Match(responseJson);
            if (!tagMatch.Success)
                return null;

            var tagString = tagMatch.Groups[1].Value;
            return !Version.TryParse(tagString, out var latestVersion) ? null : latestVersion;
        }

        private static async Task<string?> GetStringFromURL(string url, CancellationToken token)
        {
            try
            {
                var stream = await GetStreamFromURL(url, token).ConfigureAwait(false);
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

        private static async Task<Stream> GetStreamFromURL(string url, CancellationToken token)
        {
            using var client = new HttpClient();
            const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            client.DefaultRequestHeaders.Add("User-Agent", agent);
            var response = await client.GetAsync(url, token).ConfigureAwait(false);
            return await response.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
        }
    }
}
