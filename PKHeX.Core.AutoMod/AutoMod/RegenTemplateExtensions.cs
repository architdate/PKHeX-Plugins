using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class RegenTemplateExtensions
    {
        public static void SanitizeForm(this RegenTemplate set)
        {
            if (!FormVerifier.IsBattleOnlyForm(set.Species, set.FormIndex, set.Format))
                return;

            if (set.Species == (int)Species.Darmanitan)
                set.FormIndex &= 2;
            else
                set.FormIndex = 0;
        }

        public static SimpleTrainerInfo GetRegenTemplateTrainer(this RegenTemplate set)
        {
            var template = set.Format >= 8 ? new SimpleTrainerInfo(GameVersion.SW) : new SimpleTrainerInfo(GameVersion.US); 
            template.TID = set.TID;
            template.SID = set.SID;
            template.OT = set.OT;
            template.Gender = set.OT_Gender;
            if (set.Language != null)
                template.Language = (int) set.Language;
            return template;
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

        /// <summary>
        /// Add extra metadata to regen template from PKM file
        /// </summary>
        /// <param name="set">RegenTemplate to populate metadata to</param>
        /// <param name="pk">Source PKM file to grab the metadata</param>
        public static void LoadMetadata(this RegenTemplate set, PKM pk)
        {
            set.Ball = (Ball)pk.Ball;
            set.ShinyType = pk.IsShiny ? Shiny.Always : Shiny.Random;
            set.Language = (LanguageID)pk.Language;
            set.OT = pk.OT_Name;
            set.TID = pk.TID;
            set.SID = pk.SID;
            if (pk.Format >= 7)
            {
                set.TID7 = pk.TrainerID7;
                set.SID7 = pk.TrainerSID7;
            }
            set.OT_Gender = pk.OT_Gender;
            set.OverrideTrainer = true;
        }

        public static string GetRegenText(this PKM pk) => pk.Species == 0 ? string.Empty : new RegenTemplate(pk).Text;
        public static IEnumerable<string> GetRegenSets(IEnumerable<PKM> data) => data.Where(p => p.Species != 0).Select(GetRegenText);
        public static string GetRegenSets(IEnumerable<PKM> data, string separator) => string.Join(separator, GetRegenSets(data));
    }
}
