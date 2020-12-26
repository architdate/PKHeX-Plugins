using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class RegenTemplateExtensions
    {
        public static void SanitizeForm(this RegenTemplate set)
        {
            if (!FormInfo.IsBattleOnlyForm(set.Species, set.Form, set.Format))
                return;
            set.Form = FormInfo.GetOutOfBattleForm(set.Species, set.Form, set.Format);
        }

        /// <summary>
        /// Showdown quirks lets you have battle only moves in battle only forms. Transform back to base move.
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
        /// General method to preprocess sets excluding invalid forms. (handled in a future method)
        /// </summary>
        /// <param name="set">Showdown set passed to the function</param>
        /// <param name="personal">Personal data for the desired form</param>
        public static void FixGender(this RegenTemplate set, PersonalInfo personal)
        {
            if (set.Species == (int)Species.Indeedee || set.Species == (int)Species.Meowstic)
            {
                set.Gender = set.Form == 1 ? "F" : "M";
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

        public static string GetRegenText(this PKM pk) => pk.Species == 0 ? string.Empty : new RegenTemplate(pk).Text;
        public static IEnumerable<string> GetRegenSets(this IEnumerable<PKM> data) => data.Where(p => p.Species != 0).Select(GetRegenText);
        public static string GetRegenSets(this IEnumerable<PKM> data, string separator) => string.Join(separator, data.GetRegenSets());
    }
}
