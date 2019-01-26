using System;
using System.Linq;
using System.Collections.Generic;

using PKHeX.Core;
using System.IO;
using System.Windows.Forms;

namespace AutoLegalityMod
{
    /// <summary>
    /// Logic to load <see cref="SimpleTrainerInfo"/> from a saved text file.
    /// </summary>
    public static class TrainerSettings
    {
        public static int GetConsoleRegionID(string ConsoleRegion) => Util.GetUnsortedCBList("regions3ds").Find(z => z.Text == ConsoleRegion).Value;
        public static int GetSubRegionID(string SubRegion, int country) => Util.GetCBList($"sr_{country:000}", "en").Find(z => z.Text == SubRegion).Value;
        public static int GetCountryID(string Country) => Util.GetCBList("countries", "en").Find(z => z.Text == Country).Value;

        private static string GetTrainerJSONPath() => Path.Combine(Directory.GetCurrentDirectory(), "trainerdata.json");

        /// <summary>
        /// Check the mode for trainerdata.json
        /// </summary>
        /// <param name="jsonstring">string form of trainerdata.json</param>
        public static AutoModMode CheckMode(string jsonstring)
        {
            var mode = AutoModMode.Save;
            if (!jsonstring.Contains("mode"))
                return mode;
            var str = jsonstring.Split(new[] { "\"mode\"" }, StringSplitOptions.None)[1].Split('"')[1].ToLower();
            if (Enum.TryParse(str, true, out AutoModMode v))
                mode = v;
            return mode;
        }

        /// <summary>
        /// Check the mode for trainerdata.json
        /// </summary>
        public static AutoModMode CheckMode()
        {
            var path = GetTrainerJSONPath();
            if (!File.Exists(path))
                return AutoModMode.Save; // Default trainerdata.txt handling

            var jsonstring = File.ReadAllText(path);
            if (!string.IsNullOrWhiteSpace(jsonstring))
                return CheckMode(jsonstring);

            MessageBox.Show("Empty trainerdata.json file", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return AutoModMode.Save;
        }

        /// <summary>
        /// check if the game exists in the json file. If not handle via trainerdata.txt method
        /// </summary>
        /// <param name="jsonstring">Complete trainerdata.json string</param>
        /// <param name="Game">int value of the game</param>
        /// <param name="jsonvalue">internal json: trainerdata[Game]</param>
        public static bool CheckIfGameExists(string jsonstring, int Game, out string jsonvalue)
        {
            jsonvalue = "";
            if (CheckMode(jsonstring) == AutoModMode.Auto)
            {
                jsonvalue = "auto";
                return false;
            }
            if (!jsonstring.Contains($"\"{Game}\""))
                return false;
            foreach (string s in jsonstring.Split(new[] {$"\"{Game}\""}, StringSplitOptions.None))
            {
                if (s.Trim()[0] != ':')
                    continue;
                int index = jsonstring.IndexOf(s, StringComparison.Ordinal);
                jsonvalue = jsonstring.Substring(index).Split('{')[1].Split('}')[0].Trim();
                return true;
            }
            return false;
        }

        /// <summary>
        /// String parse key to find value from final json
        /// </summary>
        public static string GetValueFromKey(string key, string finaljson)
        {
            return finaljson.Split(new[] { key }, StringSplitOptions.None)[1].Split('"')[2].Trim();
        }

        /// <summary>
        /// Convert TID7 and SID7 back to the conventional TID, SID
        /// </summary>
        /// <param name="tid7">TID7 value</param>
        /// <param name="sid7">SID7 value</param>
        public static int[] ConvertTIDSID7toTIDSID(int tid7, int sid7)
        {
            uint repack = ((uint)sid7 * 1_000_000) + (uint)tid7;
            int sid = (ushort)(repack >> 16);
            int tid = (ushort)repack;
            return new[] { tid, sid };
        }

        /// <summary>
        /// Function to extract trainerdata values from trainerdata.json
        /// </summary>
        /// <param name="C_SAV">Current Save Editor</param>
        /// <param name="Game">optional Game value in case of mode being game</param>
        public static string[] ParseTrainerJSON(SaveFile C_SAV, int Game = -1)
        {
            var path = GetTrainerJSONPath();
            if (!File.Exists(path))
                return ParseTrainerData(); // Default trainerdata.txt handling

            string jsonstring = File.ReadAllText(path, System.Text.Encoding.UTF8);
            if (Game == -1)
                Game = C_SAV.Game;

            if (!CheckIfGameExists(jsonstring, Game, out string finaljson))
                return ParseTrainerData(finaljson == "auto");

            string TID = GetValueFromKey("TID", finaljson);
            string SID = GetValueFromKey("SID", finaljson);
            string OT = GetValueFromKey("OT", finaljson);
            string Gender = GetValueFromKey("Gender", finaljson);
            string Country = GetValueFromKey("Country", finaljson);
            string SubRegion = GetValueFromKey("SubRegion", finaljson);
            string ConsoleRegion = GetValueFromKey("3DSRegion", finaljson);

            if (TID.Length == 6 && SID.Length == 4)
            {
                if (new List<int> { 33, 32, 31, 30 }.IndexOf(Game) == -1) MessageBox.Show("Force Converting G7TID/G7SID to TID/SID", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                int[] tidsid = ConvertTIDSID7toTIDSID(int.Parse(TID), int.Parse(SID));
                TID = tidsid[0].ToString();
                SID = tidsid[1].ToString();
            }
            return new[] { TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion };
        }

        /// <summary>
        /// Parser for auto and preset trainerdata.txt files
        /// </summary>
        public static string[] ParseTrainerData(bool auto = false)
        {
            // Defaults
            string TID = "23456";
            string SID = "34567";
            string OT = "Archit";
            string Gender = "M";
            string Country = "Canada";
            string SubRegion = "Alberta";
            string ConsoleRegion = "Americas (NA/SA)";
            var path = GetTrainerJSONPath();
            if (!File.Exists(path))
                return new[] {TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion}; // Default No trainerdata.txt handling

            string[] trainerdataLines = File.ReadAllText(path).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            List<string> lstlines = trainerdataLines.Where(f => f != null).ToList();
            int count = lstlines.Count;
            for (int i = 0; i < count; i++)
            {
                string item = lstlines[0];
                if (item.TrimEnd().Length == 0 || item.TrimEnd() == "auto")
                    continue;
                string key = item.Split(':')[0].TrimEnd();
                string value = item.Split(':')[1].TrimEnd();
                lstlines.RemoveAt(0);
                switch (key)
                {
                    case "TID": TID = value; break;
                    case "SID": SID = value; break;
                    case "OT": OT = value; break;
                    case "Gender": Gender = value; break;
                    case "Country": Country = value; break;
                    case "SubRegion": SubRegion = value; break;
                    case "3DSRegion": ConsoleRegion = value; break;
                }
            }
            // Automatic loading
            if (trainerdataLines[0] == "auto" || auto)
            {
                try
                {
                    int ct = PKMConverter.Country;
                    int sr = PKMConverter.Region;
                    int cr = PKMConverter.ConsoleRegion;
                    if (API.SAV.Gender == 1) Gender = "F";
                    return new[] { API.SAV.TID.ToString("00000"), API.SAV.SID.ToString("00000"), API.SAV.OT, Gender, ct.ToString(), sr.ToString(), cr.ToString()};
                }
                catch
                {
                    return new[] { TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion };
                }
            }
            if (TID.Length == 6 && SID.Length == 4)
            {
                int[] tidsid = ConvertTIDSID7toTIDSID(int.Parse(TID), int.Parse(SID));
                TID = tidsid[0].ToString();
                SID = tidsid[1].ToString();
            }
            return new[] { TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion };
        }
    }
}
