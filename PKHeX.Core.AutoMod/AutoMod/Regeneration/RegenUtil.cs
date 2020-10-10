using System;
using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    public static class RegenUtil
    {
        public static bool GetTrainerInfo(IEnumerable<string> lines, int format, out ITrainerInfo tr)
        {
            var sti = new SimpleTrainerInfo {Generation = format};

            var split = Split(lines);
            bool any = false;
            foreach (var s in split)
            {
                var key = s.Key;
                var value = s.Value;
                switch (key)
                {
                    case "Language":
                        var lang = Aesthetics.GetLanguageId(value);
                        if (lang != null)
                            sti.Language = (int)lang;
                        break;
                    case "OT":
                        sti.OT = value;
                        break;
                    case "TID" when int.TryParse(value, out int TIDres):
                        sti.TID = TIDres;
                        break;
                    case "SID" when int.TryParse(value, out int SIDres):
                        sti.TID = SIDres;
                        break;
                    case "OTGender":
                        sti.Gender = value == "Female" || value == "F" ? 1 : 0;
                        break;
                    default:
                        continue;
                }

                any = true;
            }
            tr = sti;
            return any;
        }

        private const char Splitter = ':';

        public static IEnumerable<KeyValuePair<string, string>> Split(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var index = line.IndexOf(Splitter);
                if (index < 0)
                    continue;

                var key = line.Substring(0, index - 1);
                var value = line.Substring(index + 1, line.Length - index + 1).Trim();
                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        public static string GetSummary(RegenSetting extra) => extra.GetSummary();

        public static string GetSummary(ITrainerInfo trainer)
        {
            var tid = trainer.TID;
            var sid = trainer.SID;
            if (trainer.Generation >= 7)
            {
                const int mil = 1_000_000;
                var repack = (sid * mil) + tid;
                tid = repack / mil;
                sid = repack % mil;
            }

            var result = new[]
            {
                $"OT: {trainer.OT}",
                $"OTGender: {(trainer.Gender == 1 ? "F" : "M")}",
                $"TID: {tid}",
                $"SID: {sid}"
            };
            return string.Join(Environment.NewLine, result);
        }

        public static string GetSummary(StringInstructionSet trainer)
        {
            throw new NotImplementedException();
        }
    }

    public class RegenSetting
    {
        public Ball Ball { get; set; }
        public Shiny ShinyType { get; set; } = Shiny.Random;
        public LanguageID? Language { get; set; }

        public bool IsShiny => ShinyType != Shiny.Never;

        public bool SetRegenSettings(IEnumerable<string> lines)
        {
            var split = RegenUtil.Split(lines);
            bool any = false;
            foreach (var s in split)
            {
                var key = s.Key;
                var value = s.Value;
                switch (key)
                {
                    case nameof(Ball):
                        Ball = Aesthetics.GetBallFromString(value);
                        break;
                    case nameof(Shiny):
                        ShinyType = Aesthetics.GetShinyType(value);
                        break;
                    case nameof(Language):
                        Language = Aesthetics.GetLanguageId(value);
                        break;
                    default:
                        continue;
                }
                any = true;
            }
            return any;
        }

        public string GetSummary()
        {
            var result = new List<string>();
            if (Ball != Ball.None)
                result.Add($"Ball: {Ball} Ball");

            if (ShinyType == Shiny.AlwaysStar)
                result.Add("Shiny: Star");
            else if (ShinyType == Shiny.AlwaysSquare)
                result.Add("Shiny: Square");
            else if (ShinyType == Shiny.Always)
                result.Add("Shiny: Yes");

            if (Language != null)
                result.Add($"Language: {Language}");
            return string.Join(Environment.NewLine, result);
        }
    }
}
