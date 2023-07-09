using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Enhancements
{
    /// <summary>
    /// Logic for handling <see cref="ShowdownSet"/> data.
    /// </summary>
    public static class ShowdownUtil
    {
        /// <summary>
        /// Checks whether a paste is a showdown team backup
        /// </summary>
        /// <param name="paste">paste to check</param>
        /// <returns>Returns bool</returns>
        public static bool IsTeamBackup(string paste) => paste.StartsWith("===");

        // TODO: Update form list with possibly invalid Calyrex forms (non battle)??
        private static string[] InvalidFormes => new[] { "Primal", "Busted", "Crowned", "Noice", "Gulping", "Gorging", "Zen", "Galar-Zen", "Hangry", "Complete" };
        public static bool IsInvalidForm(string form) => form.Contains("Mega") || InvalidFormes.Contains(form);

        /// <summary>
        /// A method to get a list of ShowdownSet(s) from a string paste
        /// Needs to be extended to hold several teams
        /// </summary>
        /// <param name="paste"></param>
        public static List<ShowdownSet> ShowdownSets(string paste)
        {
            paste = paste.Trim(); // Remove White Spaces
            if (IsTeamBackup(paste))
                return ShowdownTeamSet.GetTeams(paste).SelectMany(z => z.Team).ToList();
            var lines = paste.Split(new[] { "\n" }, StringSplitOptions.None);
            return ShowdownParsing.GetShowdownSets(lines).ToList();
        }

        /// <summary>
        /// Checks the input text is a showdown set or not
        /// </summary>
        /// <param name="source">Concatenated showdown strings</param>
        /// <returns>boolean of the summary</returns>
        public static bool IsTextShowdownData(string source)
        {
            source = source.Trim();
            if (IsTeamBackup(source))
                return true;
            string[] stringSeparators = { "\n\r" };

            var result = source.Split(stringSeparators, StringSplitOptions.None);
            return new ShowdownSet(result[0]).Species > 0;
        }
    }
}