using System;
using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
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
