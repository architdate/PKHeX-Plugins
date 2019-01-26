using System;
using System.Linq;
using System.Collections.Generic;

using PKHeX.Core;
using System.IO;
using System.Windows.Forms;

namespace AutoLegalityMod
{
    public partial class AutoLegalityMod
    {
        /// <summary>
        /// Helper function to print out a byte array as a string that can be used within code
        /// </summary>
        /// <param name="bytes">byte array</param>
        public static void PrintByteArray(byte[] bytes)
        {
            var str = $"new byte[] {{ {string.Join(", ", bytes)} }}";
            Console.WriteLine(str);
        }

        /// <summary>
        /// Set Country, SubRegion and Console in WinForms
        /// </summary>
        /// <param name="Country">String denoting the exact country</param>
        /// <param name="SubRegion">String denoting the exact sub region</param>
        /// <param name="ConsoleRegion">String denoting the exact console region</param>
        /// <param name="pk"></param>
        public static void SetRegions(string Country, string SubRegion, string ConsoleRegion, PKM pk)
        {
            pk.Country = Util.GetCBList("countries", "en").Find(z => z.Text == Country).Value;
            pk.Region = Util.GetCBList($"sr_{pk.Country:000}", "en").Find(z => z.Text == SubRegion).Value;
            pk.ConsoleRegion = Util.GetUnsortedCBList("regions3ds").Find(z => z.Text == ConsoleRegion).Value;
        }

        /// <summary>
        /// Set Country, SubRegion and ConsoleRegion in a PKM directly
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="Country">INT value corresponding to the index of the Country</param>
        /// <param name="SubRegion">INT value corresponding to the index of the sub region</param>
        /// <param name="ConsoleRegion">INT value corresponding to the index of the console region</param>
        public static void SetPKMRegions(PKM pk, int Country, int SubRegion, int ConsoleRegion)
        {
            pk.Country = Country;
            pk.Region = SubRegion;
            pk.ConsoleRegion = ConsoleRegion;
        }

        /// <summary>
        /// Set TID, SID and OT
        /// </summary>
        /// <param name="pk">PKM to set trainer data to</param>
        /// <param name="OT">string value of OT name</param>
        /// <param name="TID">INT value of TID</param>
        /// <param name="SID">INT value of SID</param>
        /// <param name="gender">Trainer Gender</param>
        /// <param name="APILegalized">Was the <see cref="pk"/> legalized by the API</param>
        public static void SetTrainerData(PKM pk, string OT, int TID, int SID, int gender, bool APILegalized = false)
        {
            if (APILegalized)
            {
                if ((pk.TID == 12345 && pk.OT_Name == "PKHeX") || (pk.TID == 34567 && pk.SID == 0 && pk.OT_Name == "TCD"))
                {
                    bool Shiny = pk.IsShiny;
                    pk.TID = TID;
                    pk.SID = SID;
                    pk.OT_Name = OT;
                    pk.OT_Gender = gender;
                    pk.SetShinyBoolean(Shiny);
                }
                return;
            }
            pk.TID = TID;
            pk.SID = SID;
            pk.OT_Name = OT;
        }

        /// <summary>
        /// Check the mode for trainerdata.json
        /// </summary>
        /// <param name="jsonstring">string form of trainerdata.json</param>
        public static string CheckMode(string jsonstring = "")
        {
            if (!string.IsNullOrWhiteSpace(jsonstring))
            {
                string mode = "save";
                if (jsonstring.Contains("mode"))
                    mode = jsonstring.Split(new[] { "\"mode\"" }, StringSplitOptions.None)[1].Split('"')[1].ToLower();
                if (mode != "game" && mode != "save" && mode != "auto")
                    mode = "save"; // User Mistake or for some reason this exists as a value of some other key
                return mode;
            }
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\trainerdata.json"))
                return "save"; // Default trainerdata.txt handling
            jsonstring = File.ReadAllText(Directory.GetCurrentDirectory() + "\\trainerdata.json", System.Text.Encoding.UTF8);

            if (string.IsNullOrWhiteSpace(jsonstring))
            {
                MessageBox.Show("Empty trainerdata.json file", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return "save";
            }

            return CheckMode(jsonstring);
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
            if (CheckMode(jsonstring) == "auto")
            {
                jsonvalue = "auto";
                return false;
            }
            if (!jsonstring.Contains("\"" + Game + "\"")) return false;
            foreach (string s in jsonstring.Split(new[] { "\"" + Game + "\"" }, StringSplitOptions.None))
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
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\trainerdata.json"))
                return ParseTrainerData(); // Default trainerdata.txt handling

            string jsonstring = File.ReadAllText(Directory.GetCurrentDirectory() + "\\trainerdata.json", System.Text.Encoding.UTF8);
            if (Game == -1) Game = C_SAV.Game;
            if(!CheckIfGameExists(jsonstring, Game, out string finaljson)) return ParseTrainerData(finaljson == "auto");
            string TID = GetValueFromKey("TID", finaljson);
            string SID = GetValueFromKey("SID", finaljson);
            string OT = GetValueFromKey("OT", finaljson);
            string Gender = GetValueFromKey("Gender", finaljson);
            string Country = GetValueFromKey("Country", finaljson);
            string SubRegion = GetValueFromKey("SubRegion", finaljson);
            string ConsoleRegion = GetValueFromKey("3DSRegion", finaljson);

            if (TID.Length == 6 && SID.Length == 4)
            {
                if(new List<int> { 33, 32, 31, 30 }.IndexOf(Game) == -1) MessageBox.Show("Force Converting G7TID/G7SID to TID/SID", "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (!File.Exists(Directory.GetCurrentDirectory() + "\\trainerdata.txt"))
            {
                return new[] { TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion}; // Default No trainerdata.txt handling
            }
            string[] trainerdataLines = File.ReadAllText(Directory.GetCurrentDirectory() + "\\trainerdata.txt", System.Text.Encoding.UTF8)
                .Split(new[] { Environment.NewLine }, StringSplitOptions.None);

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
                    if (SAV.Gender == 1) Gender = "F";
                    return new[] { SAV.TID.ToString("00000"), SAV.SID.ToString("00000"),
                                          SAV.OT, Gender, ct.ToString(),
                                          sr.ToString(), cr.ToString()};
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
