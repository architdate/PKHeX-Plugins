using System.IO;
using System.Net;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Internet web request Utility
    /// </summary>
    public static class NetUtil
    {
        /// <summary>
        /// Gets the html string from the requested <see cref="address"/>.
        /// </summary>
        /// <param name="address">Address to fetch from</param>
        /// <returns>Page response</returns>
        public static string GetPageText(string address)
        {
            var request = WebRequest.Create(address);
            return GetStringResponse(request);
        }

        /// <summary>
        /// Downloads a string response with hard-coded <see cref="HttpWebRequest"/> parameters.
        /// </summary>
        /// <param name="address">Address to fetch from</param>
        /// <returns>Page response</returns>
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
