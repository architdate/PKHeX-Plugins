using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKHeX.Core.Enhancements
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
            var stream = GetStreamFromURL(address);
            return GetStringResponse(stream);
        }
        private static Stream GetStreamFromURL(string url)
        {
            using var client = new HttpClient();
            const string agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/60.0.3112.113 Safari/537.36";
            client.DefaultRequestHeaders.Add("User-Agent", agent);
            var response = client.GetAsync(url).Result;
            return response.Content.ReadAsStreamAsync().Result;
        }

        /// <summary>
        /// Downloads a string response with hard-coded <see cref="HttpWebRequest"/> parameters.
        /// </summary>
        /// <param name="address">Address to fetch from</param>
        /// <returns>Page response</returns>
        public static string DownloadString(string address)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "PKHeX-Auto-Legality-Mod");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var response = client.GetAsync(address).Result;
            var stream = response.Content.ReadAsStreamAsync().Result;
            return GetStringResponse(stream);
        }

        private static string GetStringResponse(Stream? dataStream)
        {
            if (dataStream == null)
                return string.Empty;
            using var reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }

        private static byte[]? GetByteResponse(Stream? dataStream)
        {
            if (dataStream == null)
                return null;
            MemoryStream ms = new();
            dataStream.CopyTo(ms);
            return ms.ToArray();
        }

        /// <summary>
        /// GPSS upload function. POST request using multipart form-data
        /// </summary>
        /// <param name="data">pkm data in bytes.</param>
        /// <param name="Url">location to fetch from</param>
        /// <param name="generation">The generation for the game the pokemon is being uploaded from.</param>
        /// <returns></returns>
        public async static Task<HttpResponseMessage> GPSSPost(byte[] data, int generation, string Url = "flagbrew.org")
        {
            using var client = new HttpClient();

            var uploadData = new MultipartFormDataContent
            {
                { new ByteArrayContent(data), "pkmn", "pkmn" }
            };

            uploadData.Headers.Add("source", "PKHeX AutoMod Plugins");
            uploadData.Headers.Add("generation", generation.ToString());

            var response = await client.PostAsync($"https://{Url}/api/v2/gpss/upload/pokemon", uploadData);
            return response;
        }

        /// <summary>
        /// GPSS downloader
        /// </summary>
        /// <param name="code">url long</param>
        /// <param name="Url">location to fetch from</param>
        /// <returns>byte array corresponding to a pkm</returns>
        public static byte[]? GPSSDownload(long code, string Url = "flagbrew.org")
        {
            // code is returned as a long
            var json = DownloadString($"https://{Url}/api/v2/gpss/download/pokemon/{code}");
            if (!json.Contains("\"pokemon\":\""))
                return null;
            var b64 = json.Split("\"pokemon\":\"")[1].Split("\"")[0];
            return System.Convert.FromBase64String(b64);
        }
    }
}
