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
                return;

            var set = new ShowdownSet(pk);
            Species = GameInfo.GetStrings("en").Species[pk.Species];
            Form = set.Form;

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
                var split2 = split1[i].Split(new[] { "\"]}" }, StringSplitOptions.None);

                var tmp = split2[0];
                SetConfig.Add(tmp);

                var morphed = ConvertSetToShowdown(tmp, ShowdownSpeciesName);
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
                case nameof(_K12):
                    return "https://www.smogon.com/dex/gs/pokemon";

                case nameof(CK3):
                case nameof(XK3):
                case nameof(PK3):
                case nameof(_K3):
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

                default: return string.Empty;
            }
        }

        private static string ConvertSetToShowdown(string set, string species)
        {
            var result = GetSetLines(set, species);
            return string.Join(Environment.NewLine, result);
        }

        private static readonly string[] statNames = { "HP", "Atk", "Def", "SpA", "SpD", "Spe" };

        private static IEnumerable<string> GetSetLines(string set, string species)
        {
            var item = string.Empty;
            if (set.Contains("\"items\":[\""))
                item = set.Split(new[] { "\"items\":[\"" }, StringSplitOptions.None)[1].Split('"')[0]; // Acrobatics Possibility

            var ability = set.Split('\"')[1];
            var evs = ParseEVIVs(set.Split(new[] { "\"evconfigs\":" }, StringSplitOptions.None)[1].Split(new[] { ",\"ivconfigs\":" }, StringSplitOptions.None)[0], false);
            var ivs = ParseEVIVs(set.Split(new[] { "\"ivconfigs\":" }, StringSplitOptions.None)[1].Split(new[] { ",\"natures\":" }, StringSplitOptions.None)[0], true);
            var nature = set.Split(new[] { "\"natures\":[\"" }, StringSplitOptions.None)[1].Split('"')[0];
            var movesets = set.Split(new[] { "\"moveslots\":[" }, StringSplitOptions.None)[1].Split(new[] { ",\"evconfigs\"" }, StringSplitOptions.None)[0];

            var result = new List<string>
            {
                item.Length == 0 ? species : $"{species} @ {item}",
                $"Ability: {ability}",
                $"EVs: {string.Join(" / ", statNames.Select((z, i) => evs[i] + z))}",
                $"IVs: {string.Join(" / ", statNames.Select((z, i) => ivs[i] + z))}",
                $"{nature} Nature"
            };
            result.AddRange(GetMoves(movesets).Select(move => $"- {move}"));
            return result;
        }

        private static IEnumerable<string> GetMoves(string movesets)
        {
            var moves = new List<string>();
            var splitmoves = movesets.Split(new[] { "[\"" }, StringSplitOptions.None);
            if (splitmoves.Length > 1)
                moves.Add(GetMove(splitmoves[1]));
            if (splitmoves.Length > 2)
                moves.Add(GetMove(splitmoves[2]));
            if (splitmoves.Length > 3)
                moves.Add(GetMove(splitmoves[3]));
            if (splitmoves.Length > 4)
                moves.Add(GetMove(splitmoves[4]));

            string GetMove(string s) => s.Split('"')[0];
            return moves;
        }

        private static string[] ParseEVIVs(string liststring, bool iv)
        {
            string[] ivdefault = { "31", "31", "31", "31", "31", "31" };
            string[] evdefault = { "0", "0", "0", "0", "0", "0" };
            var val = iv ? ivdefault : evdefault;
            if (!liststring.Contains("{"))
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
            switch (spec)
            {
                case "Nidoran♂": return "nidoran-m";
                case "Nidoran♀": return "nidoran-f";
                case "Farfetch’d": return "farfetchd";
                case "Flabébé": return "flabebe";
                default:
                    return spec;
            }
        }

        // Smogon Quirks
        private static string ConvertFormToURLForm(string form, string spec)
        {
            switch (spec)
            {
                case "Necrozma" when form == "Dusk":
                    return "dusk_mane";
                case "Necrozma" when form == "Dawn":
                    return "dawn_wings";
                case "Oricorio" when form == "Pa'u":
                    return "pau";
                default:
                    return form;
            }
        }

        private static string GetURL(string speciesName, string form, string baseURL)
        {
            if (string.IsNullOrWhiteSpace(form) || ShowdownUtil.IsInvalidForm(form))
            {
                var spec = ConvertSpeciesToURLSpecies(speciesName).ToLower();
                return $"{baseURL}/{spec}/";
            }

            var urlSpecies = ConvertSpeciesToURLSpecies(speciesName);
            {
                var spec = urlSpecies.ToLower();
                var f = ConvertFormToURLForm(form, urlSpecies).ToLower();
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
                var names = format.Split(new[] { "\"name\"" }, StringSplitOptions.None);
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