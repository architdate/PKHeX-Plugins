using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Parser for Smogon webpage <see cref="ShowdownSet"/> data.
    /// </summary>
    public class SmogonSetList
    {
        public readonly bool Valid;
        public readonly string URL;
        public readonly string Species;
        public readonly string Form;
        public readonly string ShowdownSpeciesName;
        public readonly string Page;
        public readonly List<string> SetConfig = new List<string>();
        public readonly List<string> SetText = new List<string>();
        public readonly List<ShowdownSet> Sets = new List<ShowdownSet>();

        public string Summary => AlertText(ShowdownSpeciesName, SetText.Count, GetTitles(Page));

        public SmogonSetList(PKM pk)
        {
            var baseURL = GetBaseURL(pk.GetType().Name);
            if (string.IsNullOrWhiteSpace(baseURL))
            {
                URL = Species = Form = ShowdownSpeciesName = Page = string.Empty;
                return;
            }

            var set = new ShowdownSet(pk);
            Species = GameInfo.GetStrings("en").Species[pk.Species];
            Form = ConvertFormToURLForm(set.Form, Species);

            URL = GetURL(Species, Form, baseURL);
            Page = NetUtil.GetPageText(URL);

            Valid = true;
            ShowdownSpeciesName = GetShowdownName(Species, Form);

            LoadSetsFromPage();
        }

        private static string GetShowdownName(string species, string form)
        {
            if (string.IsNullOrWhiteSpace(form) || ShowdownUtil.IsInvalidForm(form))
                return species;
            return $"{species}-{form}";
        }

        private void LoadSetsFromPage()
        {
            var split1 = Page.Split(new[] { "\",\"abilities\":" }, StringSplitOptions.None);
            for (int i = 1; i < split1.Length; i++)
            {
                var shiny = split1[i - 1].Contains("\"shiny\":true");
                var split2 = split1[i].Split(new[] { "\"]}" }, StringSplitOptions.None);

                var tmp = split2[0];
                SetConfig.Add(tmp);

                var morphed = ConvertSetToShowdown(tmp, ShowdownSpeciesName, shiny);
                SetText.Add(morphed);

                var converted = new ShowdownSet(morphed);
                Sets.Add(converted);
            }
        }

        private static string GetBaseURL(string type)
        {
            switch (type)
            {
                case nameof(PK1):
                    return "https://www.smogon.com/dex/rb/pokemon";
                case nameof(PK2):
                    return "https://www.smogon.com/dex/gs/pokemon";

                case nameof(CK3):
                case nameof(XK3):
                case nameof(PK3):
                    return "https://www.smogon.com/dex/rs/pokemon";

                case nameof(BK4):
                case nameof(PK4):
                    return "https://www.smogon.com/dex/dp/pokemon";

                case nameof(PK5):
                    return "https://www.smogon.com/dex/bw/pokemon";
                case nameof(PK6):
                    return "https://www.smogon.com/dex/xy/pokemon";
                case nameof(PK7):
                    return "https://www.smogon.com/dex/sm/pokemon";
                case nameof(PK8):
                    return "https://www.smogon.com/dex/ss/pokemon";

                default: return string.Empty;
            }
        }

        private static string ConvertSetToShowdown(string set, string species, bool shiny)
        {
            var result = GetSetLines(set, species, shiny);
            return string.Join(Environment.NewLine, result);
        }

        private static readonly string[] statNames = { "HP", "Atk", "Def", "SpA", "SpD", "Spe" };

        private static IEnumerable<string> GetSetLines(string set, string species, bool shiny)
        {
            TryGetToken(set, "\"items\":[\"", "\"", out var item);
            TryGetToken(set, "\"moveslots\":", ",\"evconfigs\":", out var movesets);
            TryGetToken(set, "\"evconfigs\":[{", "}],\"ivconfigs\":", out var evstr);
            TryGetToken(set, "\"ivconfigs\":[{", "}],\"natures\":", out var ivstr);
            TryGetToken(set, "\"natures\":[\"", "\"", out var nature);

            var evs = ParseEVIVs(evstr, false);
            var ivs = ParseEVIVs(ivstr, true);
            var ability = set[1] == ']' ? string.Empty : set.Split('\"')[1];

            if (item == "No Item") // LGPE actually lists an item, RBY sets have an empty [].
                item = string.Empty;

            var result = new List<string>(8)
            {
                item.Length == 0 ? species : $"{species} @ {item}",
            };
            if (shiny)
                result.Add($"Shiny: Yes");
            if (!string.IsNullOrWhiteSpace(ability))
                result.Add($"Ability: {ability}");
            if (evstr.Length >= 3)
                result.Add($"EVs: {string.Join(" / ", statNames.Select((z, i) => $"{evs[i]} {z}"))}");
            if (ivstr.Length >= 3)
                result.Add($"IVs: {string.Join(" / ", statNames.Select((z, i) => $"{ivs[i]} {z}"))}");
            if (!string.IsNullOrWhiteSpace(nature))
                result.Add($"{nature} Nature");

            result.AddRange(GetMoves(movesets).Select(move => $"- {move}"));
            return result;
        }

        /// <summary>
        /// Tries to rip out a substring between the provided <see cref="prefix"/> and <see cref="suffix"/>.
        /// </summary>
        /// <param name="line">Line</param>
        /// <param name="prefix">Prefix</param>
        /// <param name="suffix">Suffix</param>
        /// <param name="result">Substring within prefix-suffix.</param>
        /// <returns>True if found a substring, false if no prefix found.</returns>
        private static bool TryGetToken(string line, string prefix, string suffix, out string result)
        {
            var prefixStart = line.IndexOf(prefix, StringComparison.Ordinal);
            if (prefixStart < 0)
            {
                result = string.Empty;
                return false;
            }
            prefixStart += prefix.Length;

            var suffixStart = line.IndexOf(suffix, prefixStart, StringComparison.Ordinal);
            if (suffixStart < 0)
                suffixStart = line.Length;

            result = line.Substring(prefixStart, suffixStart - prefixStart);
            return true;
        }

        private static IEnumerable<string> GetMoves(string movesets)
        {
            var moves = new List<string>();
            var splitmoves = movesets.Split(new[] { "\"move\":\"" }, StringSplitOptions.None).Skip(1).ToArray();
            if (splitmoves.Length > 1)
                moves.Add(GetMove(splitmoves[1]));
            if (splitmoves.Length > 2)
                moves.Add(GetMove(splitmoves[2]));
            if (splitmoves.Length > 3)
                moves.Add(GetMove(splitmoves[3]));
            if (splitmoves.Length > 4)
                moves.Add(GetMove(splitmoves[4]));

            static string GetMove(string s) => s.Split('"')[0];
            return moves;
        }

        private static string[] ParseEVIVs(string liststring, bool iv)
        {
            string[] ivdefault = { "31", "31", "31", "31", "31", "31" };
            string[] evdefault = { "0", "0", "0", "0", "0", "0" };
            var val = iv ? ivdefault : evdefault;
            if (string.IsNullOrWhiteSpace(liststring))
                return val;

            string getStat(string v) => liststring.Split(new[] { v }, StringSplitOptions.None)[1].Split(',')[0];
            val[0] = getStat("\"hp\":");
            val[1] = getStat("\"atk\":");
            val[2] = getStat("\"def\":");
            val[3] = getStat("\"spa\":");
            val[4] = getStat("\"spd\":");
            val[5] = getStat("\"spe\":");

            return val;
        }

        // Smogon Quirks
        private static string ConvertSpeciesToURLSpecies(string spec)
        {
            return spec switch
            {
                "Nidoran♂" => "nidoran-m",
                "Nidoran♀" => "nidoran-f",
                "Farfetch’d" => "farfetchd",
                "Flabébé" => "flabebe",
                _ => spec
            };
        }

        // Smogon Quirks
        private static string ConvertFormToURLForm(string form, string spec)
        {
            return spec switch
            {
                "Necrozma" when form == "Dusk" => "dusk_mane",
                "Necrozma" when form == "Dawn" => "dawn_wings",
                "Oricorio" when form == "Pa'u" => "pau",
                "Darmanitan" when form == "Galarian Standard" => "galar",
                "Meowstic" when form.Length == 0 => "m",
                "Gastrodon" => "",
                "Vivillon" => "",
                "Sawsbuck" => "",
                "Deerling" => "",
                "Furfrou" => "",
                _ => form
            };
        }

        private static string GetURL(string speciesName, string form, string baseURL)
        {
            if (string.IsNullOrWhiteSpace(form) || (ShowdownUtil.IsInvalidForm(form) && form != "Crowned")) // Crowned Formes have separate pages
            {
                var spec = ConvertSpeciesToURLSpecies(speciesName).ToLower();
                return $"{baseURL}/{spec}/";
            }

            var urlSpecies = ConvertSpeciesToURLSpecies(speciesName);
            {
                var spec = urlSpecies.ToLower();
                var f = form.ToLower();
                return $"{baseURL}/{spec}-{f}/";
            }
        }

        private static Dictionary<string, List<string>> GetTitles(string pageData)
        {
            var titles = new Dictionary<string, List<string>>();
            var strats = pageData.Split(new[] { "\"strategies\":[{\"format\"" }, StringSplitOptions.None)[1].Split(new[] { "</script>" }, StringSplitOptions.None)[0];
            var formatList = strats.Split(new[] { "\"format\"" }, StringSplitOptions.None);
            foreach (string format in formatList)
            {
                var key = format.Split('"')[1];
                var values = new List<string>();
                // SS Smogon metadata can be dirtied by credits being flagged as team names
                // TODO: Handle this better
                var cleaned = format.Split(new[] { "credits" }, StringSplitOptions.None)[0];
                var names = cleaned.Split(new[] { "\"name\"" }, StringSplitOptions.None);
                for (int i = 1; i < names.Length; i++)
                {
                    values.Add(names[i].Split('"')[1]);
                }
                titles.Add(key, values);
            }
            return titles;
        }

        private static string AlertText(string showdownSpec, int count, Dictionary<string, List<string>> titles)
        {
            var sb = new StringBuilder();
            sb.Append(showdownSpec).Append(":");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            foreach (var entry in titles)
            {
                sb.Append(entry.Key).Append(": ").Append(string.Join(", ", entry.Value));
                sb.Append(Environment.NewLine);
            }
            sb.Append(Environment.NewLine);
            sb.Append(count).Append(" sets genned for ").Append(showdownSpec);
            return sb.ToString();
        }
    }
}