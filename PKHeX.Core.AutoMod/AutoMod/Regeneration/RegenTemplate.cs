// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System;
using System.Linq;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    public sealed class RegenTemplate : IBattleTemplate
    {
        public int Species { get; set; }
        public int Format { get; set; }
        public string Nickname { get; set; }
        public int Gender { get; set; }
        public int HeldItem { get; set; }
        public int Ability { get; set; }
        public int Level { get; set; }
        public bool Shiny { get; set; }
        public int Friendship { get; set; }
        public int Nature { get; set; }
        public string FormName { get; set; }
        public int Form { get; set; }
        public int HiddenPowerType { get; set; }
        public bool CanGigantamax { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        public int[] EVs { get; }
        public int[] IVs { get; }
        public int[] Moves { get; }
#pragma warning restore CA1819 // Properties should not return arrays

        public RegenSet Regen { get; set; } = RegenSet.Default;
        public string Text => GetSummary();

        private readonly string ParentLines;

        private RegenTemplate(IBattleTemplate set, int gen = PKX.Generation, string text = "")
        {
            Species = set.Species;
            Format = set.Format;
            Nickname = set.Nickname;
            Gender = set.Gender;
            HeldItem = set.HeldItem;
            Ability = set.Ability;
            Level = set.Level == 50 ? 100 : set.Level;
            Shiny = set.Shiny;
            Friendship = set.Friendship;
            Nature = set.Nature;
            FormName = set.FormName;
            Form = set.Form;
            EVs = SanitizeEVs(set.EVs, gen);
            IVs = set.IVs;
            HiddenPowerType = set.HiddenPowerType;
            Moves = set.Moves;
            CanGigantamax = set.CanGigantamax;

            ParentLines = text;
            SanitizeMoves(set, Moves);
        }

        public RegenTemplate(ShowdownSet set, int gen = PKX.Generation) : this(set, gen, set.Text)
        {
            this.SanitizeForm();
            this.SanitizeBattleMoves();

            var shiny = Shiny ? Core.Shiny.Always : Core.Shiny.Never;
            if (set.InvalidLines.Count != 0)
            {
                Regen = new RegenSet(set.InvalidLines, gen, shiny);
                Shiny = Regen.Extra.IsShiny;
                set.InvalidLines.Clear();
            }
            else
            {
                Regen.Extra.ShinyType = shiny;
            }
        }

        public RegenTemplate(PKM pk, int gen = PKX.Generation) : this(new ShowdownSet(pk), gen)
        {
            this.FixGender(pk.PersonalInfo);
            if (!pk.IsNicknamed) Nickname = string.Empty;
            Regen = new RegenSet(pk);
            Shiny = Regen.Extra.IsShiny;
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

        private static void SanitizeMoves(IBattleTemplate set, int[] moves)
        {
            // Specified moveset, no need to sanitize
            if (moves[0] != 0)
                return;

            // Sanitize keldeo moves to avoid form mismatches
            if (set.Species == (int)Core.Species.Keldeo)
                moves[0] = set.Form == 0 ? (int)Move.AquaJet : (int)Move.SecretSword;
        }

        private string GetSummary()
        {
            var sb = new StringBuilder();
            var text = ParentLines;
            var regen = Regen.GetSummary();
            bool hasRegen = !string.IsNullOrWhiteSpace(regen);

            // Add Showdown content except moves
            var split = text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var group = split.Where(z => !IsIgnored(z, Regen)).GroupBy(z => z.StartsWith("- ")).ToArray();
            sb.AppendLine(string.Join(Environment.NewLine, group[0])); // Not Moves

            // Add non-Showdown content
            if (hasRegen)
                sb.AppendLine(regen.Trim());

            // Add Moves
            if (group.Length > 1)
                sb.AppendLine(string.Join(Environment.NewLine, group[1])); // Moves
            return sb.ToString();
        }

        private static bool IsIgnored(string s, RegenSet regen)
        {
            return regen.HasExtraSettings && s.StartsWith("Shiny: ");
        }
    }
}
