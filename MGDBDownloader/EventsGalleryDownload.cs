using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace MGDBDownloader
{
    public static class EventsGalleryDownload
    {
        public static void DownloadMGDBFromGitHub(string dest, bool entire)
        {
            if (entire)
                DownloadEntireRepo(dest);
            else
                DownloadRelease(dest);
        }

        private static void DownloadRelease(string dest)
        {
            string json_data = DownloadString("https://api.github.com/repos/projectpokemon/EventsGallery/releases/latest");
            string mgdbURL = json_data.Split(new[] { "browser_download_url" }, StringSplitOptions.None)[1].Substring(3).Split('"')[0];
            DownloadAndExtractZip(mgdbURL, dest);
        }

        private static void DownloadEntireRepo(string dest)
        {
            const string mgdbURL = "https://github.com/projectpokemon/EventsGallery/archive/master.zip";
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

        private static string DownloadString(string address)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            request.Method = "GET";
            request.UserAgent = "PKHeX-Auto-Legality-Mod";
            request.Accept = "application/json";
            WebResponse response = request.GetResponse(); //Error Here
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }
    }
}