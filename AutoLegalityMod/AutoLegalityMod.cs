using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AutoLegalityMod
{
    public partial class AutoLegalityMod : IPlugin
    {

        /// <summary>
        /// Main Plugin Variables
        /// </summary>
        public string Name => "Import with Auto-Legality Mod";
        public ISaveFileProvider SaveFileEditor { get; private set; }
        public IPKMView PKMEditor { get; private set; }

        /// <summary>
        /// Global Variables for Auto Legality Mod
        /// </summary>
        int TID_ALM = -1;
        int SID_ALM = -1;
        string OT_ALM = "";
        int gender_ALM = 0;
        string Country_ALM = "";
        string SubRegion_ALM = "";
        string ConsoleRegion_ALM = "";
        bool APILegalized = false;
        string MGDatabasePath = Path.Combine(Directory.GetCurrentDirectory(), "mgdb");

        public void Initialize(params object[] args)
        {
            Console.WriteLine($"[Auto Legality Mod] Loading {Name}");
            if (args == null) return;
            SaveFileEditor = (ISaveFileProvider)Array.Find(args, z => z is ISaveFileProvider);
            SAV = SaveFileEditor.SAV;
            PKMEditor = (IPKMView)Array.Find(args, z => z is IPKMView);
            var menu = (ToolStrip)Array.Find(args, z => z is ToolStrip);
            LoadMenuStrip(menu);
        }

        private void LoadMenuStrip(ToolStrip menuStrip)
        {
            var items = menuStrip.Items;
            var tools = items.Find("Menu_Tools", false)[0] as ToolStripDropDownItem;
            AddPluginControl(tools);
        }

        private void AddPluginControl(ToolStripDropDownItem tools)
        {
            var ctrl = new ToolStripMenuItem(Name);
            tools.DropDownItems.Add(ctrl);
            ctrl.Click += new EventHandler(ClickShowdownImportPKMModded);
        }

        public void NotifySaveLoaded()
        {
            Console.WriteLine($"{Name} was notified that a Save File was just loaded.");
            SAV = SaveFileEditor.SAV;
        }

        public bool TryLoadFile(string filePath)
        {
            Console.WriteLine($"{Name} was provided with the file path, but chose to do nothing with it.");
            return false; // no action taken
        }

        /// <summary>
        /// Main function to be called by the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ClickShowdownImportPKMModded(object sender, EventArgs e)
        {
            Stopwatch timer = Stopwatch.StartNew();
            // TODO: Check for Auto Legality Mod Updates
            bool allowAPI = true; // Use true to allow experimental API usage
            APILegalized = false; // Initialize to false everytime command is used

            // Check for lack of showdown data provided
            CheckLoadFromText(out bool valid);
            if (!valid) return;

            // Make a blank MGDB directory and initialize trainerdata
            if (!Directory.Exists(MGDatabasePath)) Directory.CreateDirectory(MGDatabasePath);
            if (checkMode() != "game") LoadTrainerData();

            // Get Text source from clipboard and convert to ShowdownSet(s)
            string source = Clipboard.GetText().TrimEnd();
            List<ShowdownSet> Sets = ShowdownSets(source, out Dictionary<int, string[]> TeamData);
            if (TeamData != null) Alert(TeamDataAlert(TeamData));

            // Import Showdown Sets and alert user of any messages intended
            ImportSets(Sets, (Control.ModifierKeys & Keys.Control) == Keys.Control, out string message, allowAPI);

            // Debug Statements
            Debug.WriteLine(LogTimer(timer));
            if (message.StartsWith("[DEBUG]")) Debug.WriteLine(message);
            else Alert(message);
        }

        /// <summary>
        /// Check whether the showdown text is supposed to be loaded via a text file. If so, set the clipboard to its contents.
        /// </summary>
        /// <param name="valid">output boolean that tells if the data provided is valid or not</param>
        private void CheckLoadFromText(out bool valid)
        {
            valid = true;
            if (!showdownData() || (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (OpenSAVPKMDialog(new string[] { "txt" }, out string path))
                {
                    Clipboard.SetText(File.ReadAllText(path).TrimEnd());
                    if (!showdownData())
                    {
                        Alert("Text file with invalid data provided. Please provide a text file with proper Showdown data");
                        valid = false;
                        return;
                    }
                }
                else
                {
                    Alert("No data provided.");
                    valid = false;
                    return;
                }
            }
        }

        /// <summary>
        /// Loads the trainerdata variables into the global variables for AutoLegalityMod
        /// </summary>
        /// <param name="legal">Optional legal PKM for loading trainerdata on a per game basis</param>
        private void LoadTrainerData(PKM legal = null)
        {
            bool checkPerGame = (checkMode() == "game");
            // If mode is not set as game: (auto or save)
            string[] tdataVals;
            if (!checkPerGame || legal == null) tdataVals = parseTrainerJSON(SAV);

            else tdataVals = parseTrainerJSON(SAV, legal.Version);
            TID_ALM = Convert.ToInt32(tdataVals[0]);
            SID_ALM = Convert.ToInt32(tdataVals[1]);
            if (legal != null)
                SID_ALM = legal.VC ? 0 : SID_ALM;
            OT_ALM = tdataVals[2];
            if (OT_ALM == "PKHeX") OT_ALM = "Archit(TCD)"; // Avoids secondary handler error
            gender_ALM = 0;
            if (tdataVals[3] == "F" || tdataVals[3] == "Female") gender_ALM = 1;
            Country_ALM = tdataVals[4];
            SubRegion_ALM = tdataVals[5];
            ConsoleRegion_ALM = tdataVals[6];
            if ((checkPerGame && legal != null) || APILegalized)
                legal = SetTrainerData(OT_ALM, TID_ALM, SID_ALM, gender_ALM, legal, APILegalized);
        }

        /// <summary>
        /// Function that generates legal PKM objects from ShowdownSets and views them/sets them in boxes
        /// </summary>
        /// <param name="sets">A list of ShowdownSet(s) that need to be genned</param>
        /// <param name="replace">A boolean that determines if current pokemon will be replaced or not</param>
        /// <param name="message">Output message to be displayed for the user</param>
        /// <param name="allowAPI">Use of generators before bruteforcing</param>
        private void ImportSets(List<ShowdownSet> sets, bool replace, out string message, bool allowAPI = true)
        {
            message = "[DEBUG] Commencing Import";
            List<int> emptySlots = new List<int> { };
            IList<PKM> BoxData = SAV.BoxData;
            int BoxOffset = SaveFileEditor.CurrentBox * SAV.BoxSlotCount;
            if (replace) emptySlots = Enumerable.Range(0, sets.Count).ToList();
            else emptySlots = PopulateEmptySlots(BoxData, SaveFileEditor.CurrentBox);
            if (emptySlots.Count < sets.Count && sets.Count != 1) { message = "Not enough space in the box"; return; }
            int apiCounter = 0;
            List<ShowdownSet> invalidAPISets = new List<ShowdownSet>();
            for (int i = 0; i < sets.Count; i++)
            {
                ShowdownSet Set = sets[i];
                if (sets.Count == 1 && DialogResult.Yes != Prompt(MessageBoxButtons.YesNo, "Import this set?", Set.Text))
                    return;
                if (Set.InvalidLines.Count > 0)
                    Alert("Invalid lines detected:", string.Join(Environment.NewLine, Set.InvalidLines));
                bool resetForm = false;
                if (Set.Form != null && (Set.Form.Contains("Mega") || Set.Form == "Primal" || Set.Form == "Busted")) resetForm = true;
                PKM roughPKM = SAV.BlankPKM;
                roughPKM.ApplySetDetails(Set);
                roughPKM.Version = (int)GameVersion.MN; // Avoid the blank version glitch
                PKM legal = SAV.BlankPKM;
                bool satisfied = false;
                if (allowAPI)
                {
                    AutoLegalityMod mod = new AutoLegalityMod();
                    mod.SAV = SAV;
                    PKM APIGeneratedPKM = SAV.BlankPKM;
                    try { APIGeneratedPKM = mod.APILegality(roughPKM, Set, out satisfied); }
                    catch { satisfied = false; }
                    if (satisfied)
                    {
                        legal = APIGeneratedPKM;
                        apiCounter++;
                        APILegalized = true;
                    }
                }
                if (!allowAPI || !satisfied)
                {
                    invalidAPISets.Add(Set);
                    BruteForce b = new BruteForce { SAV = SAV };
                    legal = b.LoadShowdownSetModded_PKSM(roughPKM, Set, resetForm, TID_ALM, SID_ALM, OT_ALM, gender_ALM);
                    APILegalized = false;
                }
                PKM pk = SetTrainerData(legal, sets.Count == 1);
                if (sets.Count > 1) BoxData[BoxOffset + emptySlots[i]] = pk;
            }
            if (sets.Count > 1)
            {
                SAV.BoxData = BoxData;
                SaveFileEditor.ReloadSlots();
                message = "[DEBUG] API Genned Sets: " + apiCounter + Environment.NewLine + Environment.NewLine + "Number of sets not genned by the API: " + invalidAPISets.Count;
                foreach (ShowdownSet i in invalidAPISets) Debug.WriteLine(i.Text);
            }
            else message = "[DEBUG] Set Genning Complete";
        }

        /// <summary>
        /// Set trainer data for a legal PKM
        /// </summary>
        /// <param name="legal">Legal PKM for setting the data</param>
        /// <returns>PKM with the necessary values modified to reflect trainerdata changes</returns>
        private PKM SetTrainerData(PKM legal, bool display)
        {
            bool intRegions = false;
            LoadTrainerData(legal);
            if (int.TryParse(Country_ALM, out int n) && int.TryParse(SubRegion_ALM, out int m) && int.TryParse(ConsoleRegion_ALM, out int o))
            {
                legal = SetPKMRegions(n, m, o, legal);
                intRegions = true;
            }
            if (display) PKMEditor.PopulateFields(legal);
            if (!intRegions)
            {
                SetRegions(Country_ALM, SubRegion_ALM, ConsoleRegion_ALM, legal);
                return legal;
            }
            return legal;
        }

        /// <summary>
        /// Method to find all empty slots in a current box
        /// </summary>
        /// <param name="BoxData">Box Data of the SAV file</param>
        /// <param name="CurrentBox">Index of the current box</param>
        /// <returns>A list of all indices in the current box that are empty</returns>
        private List<int> PopulateEmptySlots(IList<PKM> BoxData, int CurrentBox)
        {
            List<int> emptySlots = new List<int>();
            int BoxCount = SAV.BoxSlotCount;
            for (int i = 0; i < BoxCount; i++)
            {
                if (BoxData[CurrentBox * BoxCount + i].Species < 1) emptySlots.Add(i);
            }
            return emptySlots;
        }

        /// <summary>
        /// A method to get a list of ShowdownSet(s) from a string paste
        /// Needs to be extended to hold several teams
        /// </summary>
        /// <param name="paste"></param>
        /// <returns></returns>
        private List<ShowdownSet> ShowdownSets(string paste, out Dictionary<int, string[]> TeamData)
        {
            TeamData = null;
            paste = paste.Trim(); // Remove White Spaces
            if (TeamBackup(paste)) TeamData = GenerateTeamData(paste, out paste);
            string[] lines = paste.Split(new string[] { "\n" }, StringSplitOptions.None);
            return ShowdownSet.GetShowdownSets(lines).ToList();
        }

        /// <summary>
        /// Checks whether a paste is a showdown team backup
        /// </summary>
        /// <param name="paste">paste to check</param>
        /// <returns>Returns bool</returns>
        private bool TeamBackup(string paste) => paste.StartsWith("===");

        /// <summary>
        /// Method to generate team data based on the given paste if applicable.
        /// </summary>
        /// <param name="paste">input paste</param>
        /// <param name="modified">modified paste for normal importing</param>
        /// <returns>null or dictionary with the teamdata</returns>
        private Dictionary<int, string[]> GenerateTeamData(string paste, out string modified)
        {
            string[] IndividualTeams = Regex.Split(paste, @"={3} \[.+\] .+ ={3}").Select(team => team.Trim()).ToArray();
            Dictionary<int, string[]> TeamData = new Dictionary<int, string[]>();
            modified = string.Join(Environment.NewLine + Environment.NewLine, IndividualTeams);
            Regex title = new Regex(@"={3} \[(?<format>.+)\] (?<teamname>.+) ={3}");
            MatchCollection titlematches = title.Matches(paste);
            for (int i = 0; i < titlematches.Count; i++)
            {
                TeamData[i] = new string[] { titlematches[i].Groups["format"].Value, titlematches[i].Groups["teamname"].Value };
            }
            if (TeamData.Count == 0) return null;
            return TeamData;
        }

        /// <summary>
        /// Convert Team Data into an alert for the main function
        /// </summary>
        /// <param name="TeamData">Dictionary with format as key and team name as value</param>
        /// <returns></returns>
        private string TeamDataAlert(Dictionary<int, string[]> TeamData)
        {
            string alert = "Generating the following teams:" + Environment.NewLine + Environment.NewLine;
            foreach (KeyValuePair<int, string[]> entry in TeamData)
            {
                alert += string.Format("Format: {0}, Team Name: {1}", entry.Value[0], entry.Value[1] + Environment.NewLine);
            }
            return alert;
        }

        /// <summary>
        /// Debug tool to help log the time needed for a function to execute. Pass Stopwatch class
        /// </summary>
        /// <param name="timer">Stopwatch to stop and read time from</param>
        /// <returns></returns>
        private string LogTimer(Stopwatch timer)
        {
            timer.Stop();
            TimeSpan timespan = timer.Elapsed;
            return String.Format("[DEBUG] Time to complete function: {0:00} minutes {1:00} seconds {2:00} milliseconds", timespan.Minutes, timespan.Seconds, timespan.Milliseconds / 10);
        }

        /// <summary>
        /// Checks the input text is a showdown set or not
        /// </summary>
        /// <returns>boolean of the summary</returns>
        private bool showdownData()
        {
            if (!Clipboard.ContainsText()) return false;
            string source = Clipboard.GetText().TrimEnd();
            if (TeamBackup(source)) return true;
            string[] stringSeparators = new string[] { "\n\r" };
            string[] result;

            // ...
            result = source.Split(stringSeparators, StringSplitOptions.None);
            if (new ShowdownSet(result[0]).Species < 0) return false;
            return true;
        }

        /// TODO
        /// Method to check for updates to AutoLegalityMod
        /// TODO


        /// <summary>
        /// Parse release GitHub tag into a PKHeX style version
        /// </summary>
        /// <param name="v">Tag String</param>
        /// <returns>PKHeX style version int</returns>
        public int ParseTagAsVersion(string v)
        {
            string[] date = v.Split('.');
            if (date.Length != 3) return -1;
            int.TryParse(date[0], out int a);
            int.TryParse(date[1], out int b);
            int.TryParse(date[2], out int c);
            return (a + 2000) * 10000 + b * 100 + c;
        }

        /// <summary>
        /// GET request to a url with UserAgent Header being AutoLegalityMod
        /// </summary>
        /// <param name="url">URL on which the GET request is to be executed</param>
        /// <returns>GET Response</returns>
        private string GetPage(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "AutoLegalityMod";
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                return responseString;
            }
            catch (Exception e)
            {
                Debug.WriteLine("An error occured while trying to obtain the contents of the URL. This is most likely an issue with your Internet Connection. The exact error is as follows: " + e.ToString() + "\nURL tried to access: " + url);
                return "Error :" + e.ToString();
            }
        }

        private DialogResult Alert(params string[] lines)
        {
            System.Media.SystemSounds.Asterisk.Play();
            string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            return MessageBox.Show(msg, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private DialogResult Prompt(MessageBoxButtons btn, params string[] lines)
        {
            System.Media.SystemSounds.Question.Play();
            string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            return MessageBox.Show(msg, "Prompt", btn, MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// Opens a dialog to open a <see cref="SaveFile"/>, <see cref="PKM"/> file, or any other supported file.
        /// </summary>
        /// <param name="Extensions">Misc extensions of <see cref="PKM"/> files supported by the SAV.</param>
        /// <param name="path">Output result path</param>
        /// <returns>Result of whether or not a file is to be loaded from the output path.</returns>
        public static bool OpenSAVPKMDialog(IEnumerable<string> Extensions, out string path)
        {
            string supported = string.Join(";", Extensions.Select(s => $"*.{s}").Concat(new[] { "*.pkm" }));
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "All Files|*.*" +
                         $"|Supported Files (*.*)|main;*.bin;{supported};*.bak" +
                         "|Save Files (*.sav)|main" +
                         "|Decrypted PKM File (*.pkm)|" + supported +
                         "|Binary File|*.bin" +
                         "|Backup File|*.bak"
            };
            path = null;
            if (ofd.ShowDialog() != DialogResult.OK)
                return false;

            path = ofd.FileName;
            return true;
        }
    }
}
