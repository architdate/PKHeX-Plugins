// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    public sealed class RegenTemplate : IBattleTemplate
    {
        public int Species { get; set; }
        public int Format { get; set; } = PKMConverter.Format;
        public string Nickname { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int HeldItem { get; set; }
        public int Ability { get; set; } = -1;
        public int Level { get; set; } = 100;
        public bool Shiny { get; set; }
        public int Friendship { get; set; } = 255;
        public int Nature { get; set; } = -1;
        public string Form { get; set; } = string.Empty;
        public int FormIndex { get; set; }
        public int[] EVs { get; set; } = { 00, 00, 00, 00, 00, 00 };
        public int[] IVs { get; set; } = { 31, 31, 31, 31, 31, 31 };
        public int HiddenPowerType { get; set; } = -1;
        public int[] Moves { get; } = { 0, 0, 0, 0 };
        public bool CanGigantamax { get; set; }

        public Ball Ball { get; set; } = Ball.None;
        public Shiny ShinyType { get; set; } = Core.Shiny.Random;

        public RegenTemplate(IBattleTemplate set)
        {
            Species = set.Species;
            Format = set.Format;
            Nickname = set.Nickname;
            Gender = set.Gender;
            HeldItem = set.HeldItem;
            Ability = set.Ability;
            Level = set.Level;
            Shiny = set.Shiny;
            Friendship = set.Friendship;
            Nature = set.Nature;
            Form = set.Form;
            FormIndex = set.FormIndex;
            EVs = set.EVs;
            IVs = set.IVs;
            HiddenPowerType = set.HiddenPowerType;
            Moves = set.Moves;
            CanGigantamax = set.CanGigantamax;
        }

        public RegenTemplate(ShowdownSet set) : this((IBattleTemplate) set)
        {
            this.SanitizeForm();
            this.SanitizeBattleMoves();
            LoadExtraInstructions(set.InvalidLines);
        }

        public RegenTemplate(PKM pk) : this(new ShowdownSet(ShowdownSet.GetShowdownText(pk)))
        {
            this.FixGender(pk.PersonalInfo);
        }


        private static readonly string[] ExtraSplitter = {": "};

        private void LoadExtraInstructions(List<string> lines)
        {
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                var split = line.Split(ExtraSplitter, 0);
                if (split.Length != 2)
                    continue;
                var type = split[0];
                var value = split[1];

                switch (type)
                {
                    case "Ball":
                        Ball = Aesthetics.GetBallFromString(value);
                        break;
                    case "Shiny":
                        ShinyType = Aesthetics.GetShinyType(value);
                        if (ShinyType != Core.Shiny.Random)
                            Shiny = ShinyType != Core.Shiny.Never;
                        break;
                    default:
                        continue;
                }
                // Remove from lines
                lines.RemoveAt(i--);
            }
        }
    }
}
