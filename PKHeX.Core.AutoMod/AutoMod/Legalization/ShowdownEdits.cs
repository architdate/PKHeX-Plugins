using System;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Modifications for a <see cref="PKM"/> based on a <see cref="IBattleTemplate"/>
    /// </summary>
    public static class ShowdownEdits
    {
        /// <summary>
        /// Quick Gender Toggle
        /// </summary>
        /// <param name="pk">PKM whose gender needs to be toggled</param>
        /// <param name="set">Showdown Set for Gender reference</param>
        public static void FixGender(this PKM pk, IBattleTemplate set)
        {
            pk.ApplySetGender(set);
            var la = new LegalityAnalysis(pk);
            if (la.Valid)
                return;
            string Report = la.Report();

            if (Report.Contains(LegalityCheckStrings.LPIDGenderMismatch))
                pk.Gender = pk.Gender == 0 ? 1 : 0;

            if (pk.Gender is not 0 and not 1)
                pk.Gender = pk.GetSaneGender();
        }

        /// <summary>
        /// Set Nature and Ability of the pokemon
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        /// <param name="preference">Ability index (1/2/4) preferred; &lt;= 0 for any</param>
        public static void SetNatureAbility(this PKM pk, IBattleTemplate set, int preference = -1)
        {
            SetNature(pk, set);
            SetAbility(pk, set, preference);
        }

        private static void SetNature(PKM pk, IBattleTemplate set)
        {
            if (pk.Nature == set.Nature)
                return;
            var val = Math.Min((int)Nature.Quirky, Math.Max((int)Nature.Hardy, set.Nature));
            pk.SetNature(val);
            if (pk.Species == (int)Species.Toxtricity)
            {
                if (pk.Form == EvolutionMethod.GetAmpLowKeyResult(val))
                    pk.Nature = val; // StatNature already set
                if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature != 12 && (pk.StatNature > 24 || pk.StatNature % 6 == 0)) // Only Serious Mint for Neutral Natures
                    pk.StatNature = 12;
                return;
            }

            // Try setting the actual nature (in the event the StatNature was set instead)
            var orig = pk.Nature;
            if (orig == val)
                return;

            var la = new LegalityAnalysis(pk);
            pk.Nature = val;
            var la2 = new LegalityAnalysis(pk);
            var enc1 = la.EncounterOriginal;
            var enc2 = la2.EncounterOriginal;
            if ((!ReferenceEquals(enc1, enc2) && !(enc1 is EncounterEgg)) || la2.Results.Any(z => z.Identifier == CheckIdentifier.Nature && !z.Valid))
                pk.Nature = orig;
            if (pk.Format >= 8 && pk.StatNature != 12 && (pk.StatNature > 24 || pk.StatNature % 6 == 0)) // Only Serious Mint for Neutral Natures
                pk.StatNature = 12;
        }

        private static void SetAbility(PKM pk, IBattleTemplate set, int preference)
        {
            if (pk.Ability != set.Ability)
                pk.SetAbility(set.Ability);

            if (preference > 0)
            {
                // Set preferred ability number if applicable
                var abilities = pk.PersonalInfo.Abilities;
                pk.AbilityNumber = abilities[preference >> 1] == set.Ability ? preference : pk.AbilityNumber;
            }
        }

        /// <summary>
        /// Set Species and Level with nickname (Helps with PreEvos)
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Set to use as reference</param>
        /// <param name="Form">Form to apply</param>
        /// <param name="enc">Encounter detail</param>
        /// <param name="lang">Language to apply</param>
        public static void SetSpeciesLevel(this PKM pk, IBattleTemplate set, int Form, IEncounterable enc, LanguageID? lang = null)
        {
            var updatevalues = pk.Species != set.Species;
            if (updatevalues)
                pk.Species = set.Species;
            pk.ApplySetGender(set);
            if (Form != pk.Form)
                pk.SetForm(Form);
            pk.SetFormArgument(enc);
            if (updatevalues)
                pk.RefreshAbility(pk.AbilityNumber >> 1);

            var usedlang = lang ?? (LanguageID) pk.Language;

            var gen = new LegalityAnalysis(pk).Info.Generation;
            var nickname = Legal.GetMaxLengthNickname(gen, usedlang) < set.Nickname.Length ? set.Nickname.Substring(0, Legal.GetMaxLengthNickname(gen, usedlang)) : set.Nickname;
            if (!WordFilter.IsFiltered(nickname, out _))
                pk.SetNickname(nickname);
            else
                pk.ClearNickname();
            pk.CurrentLevel = set.Level;
        }

        public static void SetLanguage(this PKM pk, LanguageID? lang = null) => pk.Language = lang != null ? (int)lang : pk.Language;

        private static void SetFormArgument(this PKM pk, IEncounterable enc)
        {
            if (pk is IFormArgument f)
                f.FormArgument = GetSuggestedFormArgument(pk, enc.Species);
        }

        public static uint GetSuggestedFormArgument(PKM pk, int origSpecies = 0)
        {
            return pk.Species switch
            {
                (int)Species.Hoopa when pk.Form != 0 => 3,
                (int)Species.Furfrou when pk.Form != 0 => 5,
                (int)Species.Runerigus when origSpecies != (int)Species.Runerigus => 49,
                _ => 0
            };
        }

        private static void ApplySetGender(this PKM pk, IBattleTemplate set)
        {
            if (!string.IsNullOrWhiteSpace(set.Gender))
                pk.Gender = set.Gender == "M" ? 0 : 1;
            else
                pk.Gender = pk.GetSaneGender();
        }

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        public static void SetMovesEVs(this PKM pk, IBattleTemplate set)
        {
            if (set.Moves[0] != 0)
                pk.SetMoves(set.Moves, true);
            pk.CurrentFriendship = set.Friendship;
            if (pk is IAwakened pb7)
            {
                pb7.SetSuggestedAwakenedValues(pk);
            }
            else
            {
                // In Generation 1/2 Format sets, when EVs are not specified at all, it implies maximum EVs instead!
                // Under this scenario, just apply maximum EVs (65535).
                if (pk is GBPKM gb && set.EVs.All(z => z == 0))
                    gb.EV_HP = gb.EV_ATK = gb.EV_DEF = gb.EV_SPC = gb.EV_SPE = gb.MaxEV;
                else
                    pk.EVs = set.EVs;
                var la = new LegalityAnalysis(pk);
                if (la.Parsed && !pk.WasEvent)
                    pk.SetRelearnMoves(la.GetSuggestedRelearnMoves());
            }
            pk.SetCorrectMetLevel();
        }

        public static void SetEncounterTradeIVs(this EncounterTrade t, PKM pk)
        {
            if (t.IVs.Count != 0)
                pk.SetRandomIVs(t.IVs, 0);
            else
                pk.SetRandomIVs(flawless: 3);
        }

        public static void SetHeldItem(this PKM pk, IBattleTemplate set)
        {
            pk.ApplyHeldItem(set.HeldItem, set.Format);
            pk.FixInvalidFormItems(); // arceus, silvally, giratina, genesect fix
            if (!ItemRestrictions.IsHeldItemAllowed(pk) || pk is PB7)
                pk.HeldItem = 0; // Remove the item if the item is illegal in its generation
        }

        private static void FixInvalidFormItems(this PKM pk)
        {
            switch ((Species)pk.Species)
            {
                case Species.Arceus:
                    int forma = FormVerifier.GetArceusFormFromHeldItem(pk.HeldItem, pk.Format);
                    pk.HeldItem = pk.Form != forma ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != forma ? 0 : forma;
                    break;
                case Species.Silvally:
                    int forms = FormVerifier.GetSilvallyFormFromHeldItem(pk.HeldItem);
                    pk.HeldItem = pk.Form != forms ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != forms ? 0 : forms;
                    break;
                case Species.Genesect:
                    int formg = FormVerifier.GetGenesectFormFromHeldItem(pk.HeldItem);
                    pk.HeldItem = pk.Form != formg ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != formg ? 0 : formg;
                    break;
                case Species.Giratina when pk.Form == 1 && pk.HeldItem != 112:
                    pk.HeldItem = 122;
                    break;
            }
        }
    }
}