// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    public sealed class RegenTemplate : IBattleTemplate
    {
        public int Species { get; set; }
        public int Format { get; set; }
        public string Nickname { get; set; }
        public string Gender { get; set; }
        public int HeldItem { get; set; }
        public int Ability { get; set; }
        public int Level { get; set; }
        public bool Shiny { get; set; }
        public int Friendship { get; set; }
        public int Nature { get; set; }
        public string Form { get; set; }
        public int FormIndex { get; set; }
        public int HiddenPowerType { get; set; }
        public bool CanGigantamax { get; set; }

        public int[] EVs { get; }
        public int[] IVs { get; }
        public int[] Moves { get; }

        public Ball Ball { get; set; }
        public Shiny ShinyType { get; set; } = Core.Shiny.Random;
        public LanguageID? Language { get; set; }
        public string OT { get; set; }
        public int TID { get; set; } = 12345;
        public int SID { get; set; } = 54321;
        private int TID7 { get; set; }
        private int SID7 { get; set; }
        public int OT_Gender { get; set; }
        public bool OverrideTrainer { get; set; } = false;

        public RegenTemplate(IBattleTemplate set, int gen = PKX.Generation)
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
            EVs = SanitizeEVs(set.EVs, gen);
            IVs = set.IVs;
            HiddenPowerType = set.HiddenPowerType;
            Moves = set.Moves;
            CanGigantamax = set.CanGigantamax;
        }

        public RegenTemplate(ShowdownSet set, int gen = PKX.Generation) : this((IBattleTemplate) set, gen)
        {
            this.SanitizeForm();
            this.SanitizeBattleMoves();
            LoadExtraInstructions(set.InvalidLines);
        }

        public RegenTemplate(PKM pk, int gen = PKX.Generation) : this(new ShowdownSet(ShowdownSet.GetShowdownText(pk)), gen)
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
                    case "Language":
                        Language = Aesthetics.GetLanguageId(value);
                        break;
                    case "OT":
                        OT = value;
                        OverrideTrainer = true;
                        break;
                    case "TID":
                        if (Format >= 7)
                            TID7 = int.TryParse(value, out int TIDres) ? TIDres : -1;
                        else TID = int.TryParse(value, out int TIDres) ? TIDres : -1;
                        OverrideTrainer = true;
                        break;
                    case "SID":
                        if (Format >= 7)
                            SID7 = int.TryParse(value, out int SIDres) ? SIDres : -1;
                        else SID = int.TryParse(value, out int SIDres) ? SIDres : -1;
                        OverrideTrainer = true;
                        break;
                    case "OTGender":
                        OT_Gender = value == "Female" || value == "F" ? 1 : 0;
                        break;
                    default:
                        continue;
                }

                if (OverrideTrainer)
                {
                    if (TID == -1 || TID7 == -1) TID = 12345;
                    if (SID == -1 || SID7 == -1) SID = 54321;
                    if (Format >= 7 && TID7 != -1 && SID7 != -1)
                    {
                        var oid = (SID7 * 1_000_000) + (TID7 % 1_000_000);
                        TID = (ushort) oid;
                        SID = oid >> 16;
                    }
                }
                // Remove from lines
                lines.RemoveAt(i--);
            }
        }

        private static int[] SanitizeEVs(int[] evs, int gen)
        {
            var copy = (int[])evs.Clone();
            int maxEV = gen >= 6 ? 252 : gen >= 3 ? 255 : 65535;
            for (int i = 0; i < evs.Length; i++)
            {
                if (copy[i] > maxEV)
                    copy[i] = maxEV;
            }
            return copy;
        }
    }
}
