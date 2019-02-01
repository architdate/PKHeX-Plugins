using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace PKHeX.Core.AutoMod
{
    public static class EventsGallery
    {
        public const string mgdbURL = "https://github.com/projectpokemon/EventsGallery/archive/master.zip";
        public const string releaseURL = "https://api.github.com/repos/projectpokemon/EventsGallery/releases/latest";

        public static string GetMGDBDownloadURL()
        {
            string json_data = NetUtil.DownloadString(releaseURL);
            return json_data.Split(new[] { "browser_download_url" }, StringSplitOptions.None)[1].Substring(3).Split('"')[0];
        }

        public static void DownloadMGDBFromGitHub(string dest, bool entire)
        {
            if (entire)
                DownloadEntireRepo(dest);
            else
                DownloadRelease(dest);
        }

        private static void DownloadRelease(string dest)
        {
            string downloadURL = GetMGDBDownloadURL();
            DownloadAndExtractZip(downloadURL, dest);
        }

        private static void DownloadEntireRepo(string dest)
        {
            DownloadAndExtractZip(mgdbURL, dest);

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
            WebClient client = new WebClient();
            const string temp = "temp.zip";
            client.DownloadFile(new Uri(url), temp);
            ZipFile.ExtractToDirectory(temp, dest);
            File.Delete(temp);
        }
    }
}