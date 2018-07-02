using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using AutoLegalityMod;
using PKHeX.Core;

namespace SmogonGenner
{
    public class SmogonGenner : IPlugin
    {
        public string Name => "Gen Smogon Sets";
        public int Priority => 1;
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }
        public object[] arguments;

        public void Initialize(params object[] args)
        {
            arguments = args;
            Console.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null)
                return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            var tools = items.Find("Menu_Tools", false)[0] as ToolStripDropDownItem;
            var toolsitems = tools.DropDownItems;
            var modmenusearch = toolsitems.Find("Menu_AutoLegality", false);
            if (modmenusearch.Length == 0)
            {
                var mod = new ToolStripMenuItem("Auto Legality Mod");
                tools.DropDownItems.Insert(0, mod);
                mod.Image = SmogonGennerResources.menuautolegality;
                mod.Name = "Menu_AutoLegality";
                var modmenu = mod;
                AddPluginControl(modmenu);
            }
            else
            {
                var modmenu = modmenusearch[0] as ToolStripMenuItem;
                AddPluginControl(modmenu);
            }
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(SmogonGenning);
            ctrl.Image = SmogonGennerResources.smogongenner;
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }

        private void SmogonGenning(object sender, EventArgs e)
        {
            PKM rough = PKMEditor.PreparePKM();
            string speciesName = Util.GetSpeciesList("en")[rough.Species];
            string form = new ShowdownSet(ShowdownSet.GetShowdownText(rough)).Form;
            string url = "";
            if (form != null)
            {
                string urlSpecies = ConvertSpeciesToURLSpecies(speciesName);
                if (form != "Mega" || form != "") url = String.Format("https://www.smogon.com/dex/sm/pokemon/{0}-{1}/", urlSpecies.ToLower(), ConvertFormToURLForm(form, urlSpecies).ToLower());
            }
            else url = String.Format("https://www.smogon.com/dex/sm/pokemon/{0}/", ConvertSpeciesToURLSpecies(speciesName).ToLower());
            string smogonPage = GetSmogonPage(url);
            string[] split1 = smogonPage.Split(new string[] { "\",\"abilities\":" }, StringSplitOptions.None);
            List<string> sets = new List<string>();
            for (int i = 1; i < split1.Length; i++)
            {
                sets.Add(split1[i].Split(new string[] { "\"]}" }, StringSplitOptions.None)[0]);
            }
            string showdownSpec = speciesName;
            if (form != null)
            {
                if (form != "Mega" || form != "") showdownSpec += ("-" + form);
            }
            string showdownsets = "";
            foreach (string set in sets)
            {
                showdownsets = showdownsets + ConvertSetToShowdown(set, showdownSpec) + Environment.NewLine + Environment.NewLine;
            }
            showdownsets.TrimEnd();
            if (showdownsets == "")
            {
                MessageBox.Show("No movesets available. Perhaps you could help out? Check the Contributions & Corrections forum.\n\nForum: https://www.smogon.com/forums/forums/contributions-corrections.388/");
                return;
            }
            Clipboard.SetText(showdownsets);
            try { AutomaticLegality.ImportModded(); }
            catch { MessageBox.Show("Something went wrong"); }
            MessageBox.Show(alertText(showdownSpec, sets.Count, GetTitles(smogonPage)));
        }

        private Dictionary<string, List<string>> GetTitles(string smogonPage)
        {
            Dictionary<string, List<string>> titles = new Dictionary<string, List<string>>();
            string strats = smogonPage.Split(new string[] { "\"strategies\":[{\"format\"" }, StringSplitOptions.None)[1].Split(new string[] { "</script>" }, StringSplitOptions.None)[0];
            string[] formatList = strats.Split(new string[] { "\"format\"" }, StringSplitOptions.None);
            foreach (string format in formatList)
            {
                string key = format.Split('"')[1];
                List<string> values = new List<string>();
                string[] Names = format.Split(new string[] { "\"name\"" }, StringSplitOptions.None);
                for (int i = 1; i < Names.Count(); i++)
                {
                    values.Add(Names[i].Split('"')[1]);
                }
                titles.Add(key, values);
            }
            return titles;
        }

        private string alertText(string showdownSpec, int count, Dictionary<string, List<string>> titles)
        {
            string alertText = showdownSpec + ":" + String.Concat(Enumerable.Repeat(Environment.NewLine, 2));
            foreach (KeyValuePair<string, List<string>> entry in titles)
            {
                alertText += string.Format("{0}: {1}\n", entry.Key, string.Join(", ", entry.Value));
            }
            alertText += Environment.NewLine + count.ToString() + " sets genned for " + showdownSpec;
            return alertText;
        }

        private string GetSmogonPage(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return responseString;
            }
            catch (Exception e)
            {
                MessageBox.Show("An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: " + e.ToString() + "\nURL tried to access: " + url);
                return "Error :" + e.ToString();
            }
        }

        private string ConvertSetToShowdown(string set, string species)
        {
            string ability = set.Split('\"')[1];
            string item = "";
            if (set.Contains("\"items\":[\"")) item = set.Split(new string[] { "\"items\":[\"" }, StringSplitOptions.None)[1].Split('"')[0]; // Acrobatics Possibility
            string movesets = set.Split(new string[] { "\"moveslots\":[" }, StringSplitOptions.None)[1].Split(new string[] { ",\"evconfigs\"" }, StringSplitOptions.None)[0];
            List<string> moves = new List<string>();
            string[] splitmoves = movesets.Split(new string[] { "[\"" }, StringSplitOptions.None);
            if (splitmoves.Length > 1) moves.Add(splitmoves[1].Split('"')[0]);
            if (splitmoves.Length > 2) moves.Add(splitmoves[2].Split('"')[0]);
            if (splitmoves.Length > 3) moves.Add(splitmoves[3].Split('"')[0]);
            if (splitmoves.Length > 4) moves.Add(splitmoves[4].Split('"')[0]);
            string nature = set.Split(new string[] { "\"natures\":[\"" }, StringSplitOptions.None)[1].Split('"')[0];
            string[] evs = parseEVIVs(set.Split(new string[] { "\"evconfigs\":" }, StringSplitOptions.None)[1].Split(new string[] { ",\"ivconfigs\":" }, StringSplitOptions.None)[0], false);
            string[] ivs = parseEVIVs(set.Split(new string[] { "\"ivconfigs\":" }, StringSplitOptions.None)[1].Split(new string[] { ",\"natures\":" }, StringSplitOptions.None)[0], true);
            var result = new List<string>();
            if (item != "") result.Add(species + " @ " + item);
            else result.Add(species);
            result.Add("Ability: " + ability);
            result.Add("EVs: " + evs[0] + " HP / " + evs[1] + " Atk / " + evs[2] + " Def / " + evs[3] + " SpA / " + evs[4] + " SpD / " + evs[5] + " Spe");
            result.Add("IVs: " + ivs[0] + " HP / " + ivs[1] + " Atk / " + ivs[2] + " Def / " + ivs[3] + " SpA / " + ivs[4] + " SpD / " + ivs[5] + " Spe");
            result.Add(nature + " Nature");
            foreach (string move in moves)
            {
                result.Add("- " + move);
            }
            return string.Join(Environment.NewLine, result);
        }

        private string[] parseEVIVs(string liststring, bool iv)
        {
            string[] ivdefault = new string[] { "31", "31", "31", "31", "31", "31" };
            string[] evdefault = new string[] { "0", "0", "0", "0", "0", "0" };
            if (!liststring.Contains("{"))
            {
                if (iv) return ivdefault;
                return evdefault;
            }
            string[] val = evdefault;
            if (iv) val = ivdefault;
            string hpstat = liststring.Split(new string[] { "\"hp\":" }, StringSplitOptions.None)[1].Split(',')[0];
            string atkstat = liststring.Split(new string[] { "\"atk\":" }, StringSplitOptions.None)[1].Split(',')[0];
            string defstat = liststring.Split(new string[] { "\"def\":" }, StringSplitOptions.None)[1].Split(',')[0];
            string spastat = liststring.Split(new string[] { "\"spa\":" }, StringSplitOptions.None)[1].Split(',')[0];
            string spdstat = liststring.Split(new string[] { "\"spd\":" }, StringSplitOptions.None)[1].Split(',')[0];
            string spestat = liststring.Split(new string[] { "\"spe\":" }, StringSplitOptions.None)[1].Split('}')[0];
            val[0] = hpstat;
            val[1] = atkstat;
            val[2] = defstat;
            val[3] = spastat;
            val[4] = spdstat;
            val[5] = spestat;
            return val;
        }

        // Smogon Quirks
        private static string ConvertSpeciesToURLSpecies(string spec)
        {
            if (spec == "Nidoran♂") return "nidoran-m";
            if (spec == "Nidoran♀") return "nidoran-f";
            if (spec == "Farfetch’d") return "farfetchd";
            if (spec == "Flabébé") return "flabebe";
            return spec;
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
    }
}
