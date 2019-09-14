using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Full party worth of <see cref="ShowdownSet"/> data, and page metadata.
    /// </summary>
    public class ShowdownTeamSet
    {
        public List<ShowdownSet> Team { get; set; }
        public string Format { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;

        public string Summary => $"=== [{Format}] {TeamName} ===";

        public static bool IsLineShowdownTeam(string line) => line.TrimStart().StartsWith("===") && line.TrimEnd().EndsWith("===");

        public static List<ShowdownTeamSet> GetTeams(string paste)
        {
            string[] lines = paste.Split(new[] { "\n" }, StringSplitOptions.None);
            var result = new List<ShowdownTeamSet>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!IsLineShowdownTeam(line))
                    continue;

                var split = line.Split(new[] { "===" }, 0);
                if (split.Length != 3)
                    continue;

                var split2 = split[1].Trim().Split(']');
                if (split2.Length != 2)
                    continue;

                var format = split2[0].Substring(1);
                var name = split2[1].TrimStart();
                // find end

                int end = i + 1;
                while (end < lines.Length)
                {
                    if (IsLineShowdownTeam(lines[end]))
                        break;
                    end++;
                }

                var teamlines = lines.Skip(i + 1).Take(end - i - 1).Where(z => !string.IsNullOrWhiteSpace(z));
                var sets = ShowdownSet.GetShowdownSets(teamlines).ToList();
                if (sets.Count == 0)
                    continue;
                result.Add(new ShowdownTeamSet { Format = format, TeamName = name, Team = sets });

                i = end;
            }
            return result;
        }
    }
}