using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

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
        public static string GPSSPost(byte[] data, int generation, string Url = "flagbrew.org")
        {
#pragma warning disable SYSLIB0014 // Type or member is obsolete
            WebRequest request = WebRequest.Create($"https://{Url}/api/v2/gpss/upload/pokemon");
#pragma warning restore SYSLIB0014 // Type or member is obsolete
            request.Method = "POST";
            const string boundary = "-----------";
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            // Build up the post message header  
            StringBuilder sb = new();
            sb.Append("--");
            sb.Append(boundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"");
            sb.Append("pkmn");
            sb.Append("\"; filename=\"");
            sb.Append("file");
            sb.Append("\"");
            sb.Append("\r\n");
            sb.Append("Content-Type: ");
            sb.Append("application/octet-stream");
            sb.Append("\r\n");
            sb.Append("source: ");
            sb.Append("PKHeX Plugins");
            sb.Append("\r\n");
            sb.Append("generation: ");
            sb.Append(generation);
            sb.Append("\r\n");
            sb.Append("\r\n");

            string postHeader = sb.ToString();
            byte[] postHeaderBytes = Encoding.UTF8.GetBytes(postHeader);
            byte[] boundaryBytes = Encoding.ASCII.GetBytes($"\r\n--{boundary}\r\n");
            long length = postHeaderBytes.Length + data.Length + boundaryBytes.Length;
            request.ContentLength = length;
            using (Stream datastream = request.GetRequestStream())
            {
                datastream.Write(postHeaderBytes, 0, postHeaderBytes.Length);
                datastream.Write(data, 0, data.Length);
                datastream.Write(boundaryBytes, 0, boundaryBytes.Length);
            }
            // Get the response.
            try
            {
                using var response = request.GetResponse();
                using var responseStream = response.GetResponseStream();
                if (responseStream == null)
                    return string.Empty;
                using StreamReader reader = new(responseStream);
                string responseFromServer = reader.ReadToEnd();
                return $"Pokemon added to the GPSS database. Here is your URL (has been copied to the clipboard):\n https://{Url}/gpss/" + responseFromServer;
            }
            catch (WebException e)
            {
                var exstr = "Exception: \n";
                if (e.Status == WebExceptionStatus.ProtocolError)
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    exstr += $"Status Code : {((HttpWebResponse)e.Response).StatusCode}\nStatus Description : {((HttpWebResponse)e.Response).StatusDescription}";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                else
                    exstr += e.Message;
                return exstr;
            }
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
