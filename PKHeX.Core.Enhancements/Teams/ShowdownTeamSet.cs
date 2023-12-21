using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Enhancements
{
    /// <summary>
    /// Full party worth of <see cref="ShowdownSet"/> data, and page metadata.
    /// </summary>
    public class ShowdownTeamSet(string name, List<ShowdownSet> sets, string format)
    {
        public readonly List<ShowdownSet> Team = sets;
        public readonly string Format = format;
        public readonly string TeamName = name;

        public string Summary => $"{Format}: {TeamName}";

        public static bool IsLineShowdownTeam(string line) =>
            line.TrimStart().StartsWith("===") && line.TrimEnd().EndsWith("===");

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

                var format = split2[0][1..];
                var name = split2[1].TrimStart();
                // find end

                int end = i + 1;
                while (end < lines.Length)
                {
                    if (IsLineShowdownTeam(lines[end]))
                        break;
                    end++;
                }

                var teamlines = lines.Skip(i + 1).Take(end - i - 1);
                var sets = ShowdownParsing.GetShowdownSets(teamlines).ToList();
                if (sets.Count == 0)
                    continue;
                result.Add(new ShowdownTeamSet(name, sets, format));

                i = end - 1;
            }
            return result;
        }
    }
}
