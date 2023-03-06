using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PKHeX.Core.AutoMod
{
    public static partial class ALMVersion
    {
        [GeneratedRegex("\\\"tag_name\"\\s*\\:\\s*\\\"([0-9]+\\.[0-9]+\\.[0-9]+)")]
        private static partial Regex GetLatestGitTag();
        private static readonly Regex LatestGitTagRegex = GetLatestGitTag(); // Match `"tag_name": "18.12.02"`. Group 1 is `18.12.02`
        private static readonly Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        public static readonly AssemblyVersions Versions = new();

        public record AssemblyVersions
        {
            public readonly Version? AlmVersionCurrent = GetCurrentVersion("PKHeX.Core.AutoMod");
            public readonly Version? AlmVersionLatest = GetLatestALMVersion();

            public readonly Version? CoreVersionCurrent = GetCurrentVersion("PKHeX.Core");
            public readonly Version? CoreVersionLatest = GetLatestCoreVersion();
        }

        /// <summary>
        /// Checks for plugin mismatch. If "AllowMismatch" is enabled it will allow a user to skip update warnings until the next release. Will otherwise check plugin mismatch for current versions.
        /// </summary>
        /// <returns>True if a plugin mismatch is found. False if any of the versions are null or no mismatch found.</returns>
        public static bool GetIsMismatch()
            => GetIsMismatch(currentCore: Versions.CoreVersionCurrent, currentALM: Versions.AlmVersionCurrent, latestCore: Versions.CoreVersionLatest, latestALM: Versions.AlmVersionLatest);

        public static bool GetIsMismatch(Version? currentCore, Version? currentALM, Version? latestCore, Version? latestALM)
        {
            if (currentCore is null || currentALM is null || latestCore is null || latestALM is null)
                return false;

            var latestAllowed = new Version(APILegality.LatestAllowedVersion);
            if (APILegality.AllowMismatch && (latestCore > latestAllowed))
                return true;
            return !APILegality.AllowMismatch && (currentCore > currentALM);
        }

        /// <summary>
        /// Gets the current version of the specified assembly.
        /// </summary>
        /// <returns>A version representing the current version of the specified assembly, or null if the assembly cannot be found or has no version available.</returns>
        private static Version? GetCurrentVersion(string assemblyName)
        {
            var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == assemblyName);
            return assembly?.GetName().Version;
        }

        /// <summary>
        /// Gets the latest version of ALM according to the Github API
        /// </summary>
        /// <returns>A version representing the latest available version of PKHeX, or null if the latest version could not be determined.</returns>
        private static Version? GetLatestALMVersion()
        {
            try
            {
                const string apiEndpoint = "https://api.github.com/repos/architdate/pkhex-plugins/releases/latest";
                var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
                if (responseJson is null)
                    return null;

                // Using a regex to get the tag to avoid importing an entire JSON parsing library
                var tagMatch = LatestGitTagRegex.Match(responseJson);
                if (!tagMatch.Success)
                    return null;

                var tagString = tagMatch.Groups[1].Value;
                return !Version.TryParse(tagString, out var latestVersion) ? null : latestVersion;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        private static Version? GetLatestCoreVersion()
        {
            try
            {
                return UpdateUtil.GetLatestPKHeXVersion();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
