using System.IO;
using System.Net;

namespace PKHeX.Core.AutoMod
{
    public static class NetUtil
    {
        public static string GetPageText(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        public static string DownloadString(string address)
        {
            var request = (HttpWebRequest)WebRequest.Create(address);
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
