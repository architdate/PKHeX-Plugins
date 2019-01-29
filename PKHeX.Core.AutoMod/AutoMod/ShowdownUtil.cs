using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PKHeX.Core.AutoMod
{
    public static class ShowdownUtil
    {
        /// <summary>
        /// Checks whether a paste is a showdown team backup
        /// </summary>
        /// <param name="paste">paste to check</param>
        /// <returns>Returns bool</returns>
        public static bool IsTeamBackup(string paste) => paste.StartsWith("===");

        public static bool IsInvalidForm(string form) => form != null && (form.Contains("Mega") || form == "Primal" || form == "Busted");

        /// <summary>
        /// A method to get a list of ShowdownSet(s) from a string paste
        /// Needs to be extended to hold several teams
        /// </summary>
        /// <param name="paste"></param>
        /// <param name="TeamData"></param>
        public static List<ShowdownSet> ShowdownSets(string paste, out Dictionary<int, string[]> TeamData)
        {
            TeamData = null;
            paste = paste.Trim(); // Remove White Spaces
            if (IsTeamBackup(paste))
                TeamData = GenerateTeamData(paste, out paste);
            string[] lines = paste.Split(new[] { "\n" }, StringSplitOptions.None);
            return ShowdownSet.GetShowdownSets(lines).ToList();
        }

        /// <summary>
        /// Method to generate team data based on the given paste if applicable.
        /// </summary>
        /// <param name="paste">input paste</param>
        /// <param name="modified">modified paste for normal importing</param>
        /// <returns>null or dictionary with the teamdata</returns>
        public static Dictionary<int, string[]> GenerateTeamData(string paste, out string modified)
        {
            string[] IndividualTeams = Regex.Split(paste, @"={3} \[.+\] .+ ={3}").Select(team => team.Trim()).ToArray();
            Dictionary<int, string[]> TeamData = new Dictionary<int, string[]>();
            modified = string.Join(Environment.NewLine + Environment.NewLine, IndividualTeams);
            Regex title = new Regex(@"={3} \[(?<format>.+)\] (?<teamname>.+) ={3}");
            MatchCollection titlematches = title.Matches(paste);
            for (int i = 0; i < titlematches.Count; i++)
            {
                TeamData[i] = new[] { titlematches[i].Groups["format"].Value, titlematches[i].Groups["teamname"].Value };
            }
            return TeamData.Count == 0 ? null : TeamData;
        }

        /// <summary>
        /// Convert Team Data into an alert for the main function
        /// </summary>
        /// <param name="TeamData">Dictionary with format as key and team name as value</param>
        public static string TeamDataAlert(Dictionary<int, string[]> TeamData)
        {
            string alert = "Generating the following teams:" + Environment.NewLine + Environment.NewLine;
            var lines = TeamData.Select(z => $"Format: {z.Value[0]}, Team Name: {z.Value[1]}");
            return alert + string.Join(Environment.NewLine, lines);
        }
    }
}