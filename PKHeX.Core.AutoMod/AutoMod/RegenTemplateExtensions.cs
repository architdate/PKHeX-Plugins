using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class RegenTemplateExtensions
    {
        private static readonly HashSet<int> BattleOnly = GetBattleFormSet();

        private static HashSet<int> GetBattleFormSet()
        {
            var hs = new HashSet<int>();
            hs.UnionWith(Legal.BattleForms);
            hs.UnionWith(Legal.BattleMegas);
            hs.UnionWith(Legal.BattlePrimals);
            return hs;
        }

        // Replace with PKHeX.Core method call on next API update (FormVerifier)
        private static bool IsBattleOnlyForm(int species, int form, int format)
        {
            if (!BattleOnly.Contains(species))
                return false;
            if (species == (int)Species.Darmanitan && form == 2 && format >= 8)
                return false; // this one is OK, Galarian non-Zen
            return true;
        }

        public static void SanitizeForm(this RegenTemplate set)
        {
            if (!IsBattleOnlyForm(set.Species, set.FormIndex, set.Format))
                return;

            if (set.Species == (int)Species.Darmanitan)
                set.FormIndex &= 2;
            else
                set.FormIndex = 0;
        }

        /// <summary>
        /// Showdown quirks lets you have battle only moves in battle only formes. Transform back to base move.
        /// </summary>
        /// <param name="set"></param>
        public static void SanitizeBattleMoves(this IBattleTemplate set)
        {
            switch (set.Species)
            {
                case (int)Species.Zacian:
                case (int)Species.Zamazenta:
                {
                    // Behemoth Blade and Behemoth Bash -> Iron Head
                    if (!set.Moves.Contains(781) && !set.Moves.Contains(782))
                        return;

                    for (int i = 0; i < set.Moves.Length; i++)
                    {
                        if (set.Moves[i] == 781 || set.Moves[i] == 782)
                            set.Moves[i] = 442;
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// General method to preprocess sets excluding invalid formes. (handled in a future method)
        /// </summary>
        /// <param name="set">Showdown set passed to the function</param>
        /// <param name="personal">Personal data for the desired form</param>
        public static void FixGender(this RegenTemplate set, PersonalInfo personal)
        {
            if (set.Species == (int) Species.Indeedee || set.Species == (int) Species.Meowstic)
            {
                set.Gender = set.FormIndex == 1 ? "F" : "M";
                return;
            }

            // Validate Gender
            if (personal.Genderless && set.Gender.Length == 0)
                set.Gender = string.Empty;
            else if (personal.OnlyFemale && set.Gender != "F")
                set.Gender = "F";
            else if (personal.OnlyMale && set.Gender != "M")
                set.Gender = "M";
        }
    }
}
