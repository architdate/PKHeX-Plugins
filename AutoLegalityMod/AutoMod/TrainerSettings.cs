using System;
using System.Linq;
using System.Collections.Generic;

using PKHeX.Core;
using System.IO;
using System.Text;

namespace AutoLegalityMod
{
    /// <summary>
    /// Logic to load <see cref="SimpleTrainerInfo"/> from a saved text file.
    /// </summary>
    public static class TrainerSettings
    {
        private static int GetConsoleRegionID(string val) => Util.GetUnsortedCBList("regions3ds").Find(z => z.Text == val).Value;
        private static int GetSubRegionID(string val, int country) => Util.GetCBList($"sr_{country:000}", "en").Find(z => z.Text == val).Value;
        private static int GetCountryID(string val) => Util.GetCBList("countries", "en").Find(z => z.Text == val).Value;

        private static string GetTrainerJSONPath() => Path.Combine(Directory.GetCurrentDirectory(), "trainerdata.json");

        /// <summary>
        /// Check the mode for trainerdata.json
        /// </summary>
        /// <param name="jsonstring">string form of trainerdata.json</param>
        private static AutoModMode CheckMode(string jsonstring)
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
        private static AutoModMode CheckMode()
        {
            var path = GetTrainerJSONPath();
            if (!File.Exists(path))
                return AutoModMode.Save; // Default trainerdata.txt handling

            var jsonstring = File.ReadAllText(path);
            if (!string.IsNullOrWhiteSpace(jsonstring))
                return CheckMode(jsonstring);

            WinFormsUtil.Alert("Empty trainerdata.json file");
            return AutoModMode.Save;
        }

        /// <summary>
        /// check if the game exists in the json file. If not handle via trainerdata.txt method
        /// </summary>
        /// <param name="jsonstring">Complete trainerdata.json string</param>
        /// <param name="Game">int value of the game</param>
        /// <param name="jsonvalue">internal json: trainerdata[Game]</param>
        private static bool CheckIfGameExists(string jsonstring, int Game, out string jsonvalue)
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
        private static string GetValueFromKey(string key, string finaljson)
        {
            return finaljson.Split(new[] { key }, StringSplitOptions.None)[1].Split('"')[2].Trim();
        }

        /// <summary>
        /// Convert TID7 and SID7 back to the conventional TID, SID
        /// </summary>
        /// <param name="tid7">TID7 value</param>
        /// <param name="sid7">SID7 value</param>
        private static int[] ConvertTIDSID7toTIDSID(int tid7, int sid7)
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
        private static string[] ParseTrainerJSON(SaveFile C_SAV, int Game = -1)
        {
            var path = GetTrainerJSONPath();
            if (!File.Exists(path))
                return ParseTrainerData(); // Default trainerdata.txt handling

            string jsonstring = File.ReadAllText(path, Encoding.UTF8);
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
                if (new List<int> { 33, 32, 31, 30 }.IndexOf(Game) == -1)
                    WinFormsUtil.Alert("Force Converting G7TID/G7SID to TID/SID");
                int[] tidsid = ConvertTIDSID7toTIDSID(int.Parse(TID), int.Parse(SID));
                TID = tidsid[0].ToString();
                SID = tidsid[1].ToString();
            }
            return new[] { TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion };
        }

        /// <summary>
        /// Parser for auto and preset trainerdata.txt files
        /// </summary>
        private static string[] ParseTrainerData(bool auto = false)
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

        private static SimpleTrainerInfo GetTrainer(string[] tdataVals)
        {
            var trainer = new SimpleTrainerInfo
            {
                TID = Convert.ToInt32(tdataVals[0]),
                SID = Convert.ToInt32(tdataVals[1]),
                OT = tdataVals[2]
            };

            if (trainer.OT == "PKHeX")
                trainer.OT = "Archit(TCD)"; // Avoids secondary handler error
            trainer.Gender = tdataVals[3] == "F" || tdataVals[3] == "Female" ? 1 : 0;

            // Load Trainer location details; check first if english string name
            // if not, try to check if they're stored as integers.
            trainer.Country = GetCountryID(tdataVals[4]);
            trainer.SubRegion = GetSubRegionID(tdataVals[5], trainer.Country);
            trainer.ConsoleRegion = GetConsoleRegionID(tdataVals[6]);

            if (trainer.Country < 0 && int.TryParse(tdataVals[4], out var c))
                trainer.Country = c;
            if (trainer.SubRegion < 0 && int.TryParse(tdataVals[5], out var s))
                trainer.SubRegion = s;
            if (trainer.ConsoleRegion < 0 && int.TryParse(tdataVals[6], out var x))
                trainer.ConsoleRegion = x;
            return trainer;
        }

        private static SaveFile SAV => API.SAV;

        public static SimpleTrainerInfo GetSavedTrainerData(PKM legal = null)
        {
            bool checkPerGame = (CheckMode() == AutoModMode.Save);
            // If mode is not set as game: (auto or save)
            var tdataVals = !checkPerGame || legal == null
                ? ParseTrainerJSON(SAV)
                : ParseTrainerJSON(SAV, legal.Version);

            var trainer = GetTrainer(tdataVals);
            if (legal != null)
                trainer.SID = legal.VC ? 0 : trainer.SID;

            return trainer;
        }

        public static SimpleTrainerInfo GetRoughTrainerData(this PKM illegalPK)
        {
            return new SimpleTrainerInfo
            {
                TID = illegalPK.TID,
                SID = illegalPK.SID,
                OT = illegalPK.OT_Name,
                Gender = illegalPK.OT_Gender,
            };
        }
    }
}
