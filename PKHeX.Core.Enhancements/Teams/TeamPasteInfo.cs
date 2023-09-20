using System;

namespace PKHeX.Core.Enhancements
{
    /// <summary>
    /// Paste Details originating from a common pasting website URL.
    /// </summary>
    public class TeamPasteInfo
    {
        public readonly bool Valid;
        public readonly PasteSource Source;
        public readonly string URL;
        public readonly string Sets = string.Empty;

        public string Summary =>
            $"{Source} data:\nTitle: {Title}\nAuthor: {Author}\nDescription: {Description}";

        public string Author { get; private set; } = "Showdown Paste";
        public string Title { get; private set; } = "Pokémon Trainer";
        public string Description { get; private set; } = "A Mysterious Paste";

        public enum PasteSource
        {
            None,
            Pastebin,
            PokePaste,
        }

        private void GetFromPokePaste(string url)
        {
            var htmldoc = NetUtil.GetPageText(url);
            var pastedata = htmldoc.Split(new[] { "<aside>" }, StringSplitOptions.None)[1].Split(
                new[] { "</aside>" },
                StringSplitOptions.None
            )[0];

            var title = pastedata.Split(new[] { "<h1>" }, StringSplitOptions.None);
            if (title.Length > 1)
                Title = GetVal(title[1]);

            var auth = pastedata.Split(new[] { "<h2>&nbsp;by" }, StringSplitOptions.None);
            if (auth.Length > 1)
                Author = GetVal(auth[1]);

            var desc = pastedata.Split(new[] { "<p>" }, StringSplitOptions.None);
            if (desc.Length > 1)
                Description = GetVal(desc[1]);
        }

        private void GetFromPasteBin(string url)
        {
            var page = NetUtil.GetPageText(url);

            var title = page.Split(new[] { "<h1>" }, StringSplitOptions.None)[1];
            Title = GetVal(title);

            var auth = page.Split(new[] { "<div class=\"username\">" }, StringSplitOptions.None)[
                1
            ].Split('>');
            Author = GetVal(auth[0]);

            var datestr = auth[3];
            var date = GetVal(datestr);
            Description = $"Pastebin created on: {date}";
        }

        private static PasteSource GetSource(string url)
        {
            if (url.Contains("pokepast.es/"))
                return PasteSource.PokePaste;
            if (url.Contains("pastebin.com/"))
                return PasteSource.Pastebin;
            return PasteSource.None;
        }

        private string GetRawURL(string url)
        {
            return Source switch
            {
                PasteSource.PokePaste => url.EndsWith("/raw") ? url : url + "/raw",
                PasteSource.Pastebin
                    => url.Contains("/raw/")
                        ? url
                        : url.Replace("pastebin.com/", "pastebin.com/raw/"),
                _ => url, // This should never happen
            };
        }

        private void LoadMetadata()
        {
            // Passed URL must be non raw
            switch (Source)
            {
                case PasteSource.PokePaste:
                {
                    var url = URL.Replace("/raw", "");
                    GetFromPokePaste(url);
                    return;
                }
                case PasteSource.Pastebin:
                {
                    var url = URL.Replace("/raw/", "/");
                    GetFromPasteBin(url);
                    return;
                }
                default:
                    return; // This should never happen
            }
        }

        private static string GetVal(string s, char c = '<') => s.Split(c)[0].Trim();

        public TeamPasteInfo(string url)
        {
            URL = url;
            var isUri = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
            if (!isUri)
                return;
            Source = GetSource(url);
            if (Source == PasteSource.None)
                return;

            url = GetRawURL(url);
            Sets = NetUtil.GetPageText(url).Trim();
            LoadMetadata();
            Valid = true;
        }
    }
}
