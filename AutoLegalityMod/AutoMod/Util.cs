using System.IO;
using System.Net;

namespace AutoLegalityMod
{
    public static class NetUtil
    {
        public static string GetPageText(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            var response = (HttpWebResponse)request.GetResponse();
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}
