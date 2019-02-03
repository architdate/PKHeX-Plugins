using System.IO;
using System.Net;

namespace PKHeX.Core.AutoMod
{
    public static class NetUtil
    {
        public static string GetPageText(string url)
        {
            var request = WebRequest.Create(url);
            return GetStringResponse(request);
        }

        public static string DownloadString(string address)
        {
            var request = (HttpWebRequest)WebRequest.Create(address);
            request.Method = "GET";
            request.UserAgent = "PKHeX-Auto-Legality-Mod";
            request.Accept = "application/json";
            return GetStringResponse(request);
        }

        private static string GetStringResponse(WebRequest request)
        {
            using (var response = request.GetResponse())
            using (var dataStream = response.GetResponseStream())
            using (var reader = new StreamReader(dataStream))
                return reader.ReadToEnd();
        }
    }
}
