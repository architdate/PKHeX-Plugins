using System;
using System.IO;
using System.Net;
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
            using var response = request.GetResponse();
            using var dataStream = response.GetResponseStream();
            if (dataStream == null)
                return string.Empty;
            using var reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// GPSS upload function. POST request using multipart form-data
        /// </summary>
        /// <param name="data">pkm data in bytes.</param>
        /// <param name="Url">location to fetch from</param>
        /// <returns></returns>
        public static string GPSSPost(byte[] data, string Url = "flagbrew.org")
        {
            WebRequest request = WebRequest.Create($"https://{Url}/gpss/share");
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
                return $"Pokemon added to the GPSS database. Here is your URL (has been copied to the clipboard):\n https://{Url}/gpss/view/" + responseFromServer;
            }
            catch (WebException e)
            {
                var exstr = "Exception: \n";
                if (e.Status == WebExceptionStatus.ProtocolError)
                    exstr += $"Status Code : {((HttpWebResponse)e.Response).StatusCode}\nStatus Description : {((HttpWebResponse)e.Response).StatusDescription}";
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
        public static byte[] GPSSDownload(long code, string Url = "flagbrew.org")
        {
            // code is returned as a long
            var request = (HttpWebRequest)WebRequest.Create($"https://{Url}/gpss/download/{code}");
            request.Method = "GET";
            request.UserAgent = "PKHeX-Auto-Legality-Mod";
            var b64 = GetStringResponse(request);
            return Convert.FromBase64String(b64);
        }
    }
}
