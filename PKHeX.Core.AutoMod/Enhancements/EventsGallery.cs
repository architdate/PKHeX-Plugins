using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Interactions with ProjectPokémon's Event Gallery (hosted on GitHub)
    /// </summary>
    public static class EventsGallery
    {
        private const string RepoURL = "https://github.com/projectpokemon/EventsGallery/archive/master.zip";
        private const string RepoReleaseURL = "https://api.github.com/repos/projectpokemon/EventsGallery/releases/latest";

        public static string GetMGDBDownloadURL()
        {
            var json_data = NetUtil.DownloadString(RepoReleaseURL);
            return json_data.Split(new[] { "browser_download_url" }, StringSplitOptions.None)[1].Substring(3).Split('"')[0];
        }

        /// <summary>
        /// Downloads the entire repository from GitHub and extracts the contents to the <see cref="dest"/>.
        /// </summary>
        /// <param name="dest">Location to extract the repository to</param>
        /// <param name="entire">True to Download the current repository, false to only download the latest release.</param>
        public static void DownloadMGDBFromGitHub(string dest, bool entire)
        {
            if (entire)
                DownloadEntireRepo(dest);
            else
                DownloadRelease(dest);
        }

        private static void DownloadRelease(string dest)
        {
            var downloadURL = GetMGDBDownloadURL();
            DownloadAndExtractZip(downloadURL, dest);
        }

        private static void DownloadEntireRepo(string dest)
        {
            DownloadAndExtractZip(RepoURL, dest);

            // clean up; delete unneeded files
            var path = Path.Combine(dest, "EventsGallery-master");
            string getPath(string s) => Path.Combine(path, s);
            Directory.Delete(getPath("Unreleased"), true);
            Directory.Delete(getPath("Extras"), true);
            File.Delete(getPath(".gitignore"));
            File.Delete(getPath("README.md"));
        }

        private static void DownloadAndExtractZip(string url, string dest)
        {
            const string temp = "temp.zip";
            using (WebClient client = new WebClient())
                client.DownloadFile(new Uri(url), temp);

            ZipFile.ExtractToDirectory(temp, dest);
            File.Delete(temp);
        }
    }
}