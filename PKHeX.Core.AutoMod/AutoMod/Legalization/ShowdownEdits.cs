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
        public static void SetNatureAbility(this PKM pk, IBattleTemplate set, IEncounterable enc, int preference = -1)
        {
            SetNature(pk, set, enc);
            SetAbility(pk, set, preference);
        }

        private static void SetNature(PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            if (pk.Nature == set.Nature)
                return;
            var val = Math.Min((int)Nature.Quirky, Math.Max((int)Nature.Hardy, set.Nature));
            if (pk.Species == (int)Species.Toxtricity)
            {
                if (pk.Form == EvolutionMethod.GetAmpLowKeyResult(val))
                    pk.Nature = val; // StatNature already set
                if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature != 12 && (pk.StatNature > 24 || pk.StatNature % 6 == 0)) // Only Serious Mint for Neutral Natures
                    pk.StatNature = 12;
                return;
            }

            pk.SetNature(val);
            // Try setting the actual nature (in the event the StatNature was set instead)
            var orig = pk.Nature;
            if (orig == val)
                return;

            var la = new LegalityAnalysis(pk);
            pk.Nature = val;
            var la2 = new LegalityAnalysis(pk);
            var enc1 = la.EncounterMatch;
            var enc2 = la2.EncounterMatch;
            if (((!ReferenceEquals(enc1, enc2) && enc1 is not EncounterEgg) || la2.Results.Any(z => z.Identifier == CheckIdentifier.Nature && !z.Valid)) && enc is not EncounterEgg)
                pk.Nature = orig;
            if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature is 0 or 6 or 18 or >= 24) // Only Serious Mint for Neutral Natures
                pk.StatNature = (int)Nature.Serious;
        }

        private static void SetAbility(PKM pk, IBattleTemplate set, int preference)
        {
            if (pk.Ability != set.Ability)
                pk.SetAbility(set.Ability);

            if (preference > 0)
            {
                var abilities = pk.PersonalInfo.Abilities;
                // Set unspecified abilities
                if (set.Ability == -1)
                {
                    pk.SetAbility(abilities[preference >> 1]);
                    pk.AbilityNumber = preference;
                }
                // Set preferred ability number if applicable
                if (abilities[preference >> 1] == set.Ability)
                    pk.AbilityNumber = preference;
                // 3/4/5 transferred to 6+ will have ability 1 if both abilitynum 1 and 2 are the same. Capsule cant convert 1 -> 2 if the abilities arnt unique
                if (pk.Format >= 6 && pk.Generation is 3 or 4 or 5 && pk.AbilityNumber != 4 && abilities[0] == abilities[1])
                    pk.AbilityNumber = 1;
                if (pk is G3PKM g3 && abilities[0] == abilities[1])
                    pk.AbilityNumber = 1;
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
            pk.ApplySetGender(set);

            var evolutionRequired = pk.Species != set.Species;
            if (evolutionRequired)
                pk.Species = set.Species;
            if (Form != pk.Form)
                pk.SetForm(Form);

            // Don't allow invalid tox nature, set random nature first and then statnature later
            if (pk.Species == (int)Species.Toxtricity)
            {
                while (true)
                {
                    var result = EvolutionMethod.GetAmpLowKeyResult(pk.Nature);
                    if (result == pk.Form)
                        break;
                    pk.Nature = Util.Rand.Next(25);
                }
            }

            pk.SetSuggestedFormArgument(enc.Species);
            if (evolutionRequired)
                pk.RefreshAbility(pk.AbilityNumber >> 1);

            pk.CurrentLevel = set.Level;
            if (pk.Met_Level > pk.CurrentLevel)
                pk.Met_Level = pk.CurrentLevel;
            if (set.Level != 100 && set.Level == enc.LevelMin && (pk.Format == 3 || pk.Format == 4))
                pk.EXP = Experience.GetEXP(enc.LevelMin + 1, PersonalTable.HGSS[enc.Species].EXPGrowth) - 1;

            var currentlang = (LanguageID)pk.Language;
            var finallang = lang ?? currentlang;
            if (finallang == LanguageID.Hacked)
                finallang = LanguageID.English;
            pk.Language = (int)finallang;

            // check if nickname even needs to be updated
            if (set.Nickname.Length == 0 && finallang == currentlang && !evolutionRequired)
                return;

            // don't bother checking encountertrade nicknames for length validity
            if (enc is EncounterTrade et && et.HasNickname)
                return;

            var gen = enc.Generation;
            var maxlen = Legal.GetMaxLengthNickname(gen, finallang);
            var nickname = set.Nickname.Length > maxlen ? set.Nickname.Substring(0, maxlen) : set.Nickname;
            if (!WordFilter.IsFiltered(nickname, out _))
                pk.SetNickname(nickname);
            else
                pk.ClearNickname();
        }

        /// <summary>
        /// Applies specified gender (if it exists. Else choose specied gender)
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">IBattleset template to grab the set gender</param>
        private static void ApplySetGender(this PKM pk, IBattleTemplate set)
        {
            if (set.Gender != -1)
                pk.Gender = set.Gender;
            else
                pk.Gender = pk.GetSaneGender();
        }

        /// <summary>
        /// Function to check if there is any PID Gender Mismatch
        /// </summary>
        /// <param name="pkm">PKM to modify</param>
        /// <param name="enc">Base encounter</param>
        /// <returns>boolean indicating if the gender is valid</returns>
        public static bool IsValidGenderPID(this PKM pkm, IEncounterable enc)
        {
            bool genderValid = pkm.IsGenderValid();
            if (!genderValid)
                return IsValidGenderMismatch(pkm);

            // check for mixed->fixed gender incompatibility by checking the gender of the original species
            int original = enc.Species;
            if (Legal.FixedGenderFromBiGender.Contains(original))
                return IsValidFixedGenderFromBiGender(pkm, original);

            return true;
        }

        /// <summary>
        /// Helper function to check if bigender => fixed gender evolution is valid
        /// </summary>
        /// <param name="pkm">pkm to modify</param>
        /// <param name="original">original species (encounter)</param>
        /// <returns>boolean indicating validaity</returns>
        private static bool IsValidFixedGenderFromBiGender(PKM pkm, int original)
        {
            var current = pkm.Gender;
            if (current == 2) // shedinja, genderless
                return true;
            var gender = PKX.GetGenderFromPID(original, pkm.EncryptionConstant);
            return gender == current;
        }

        /// <summary>
        /// Check if a gender mismatch is a valid possibility
        /// </summary>
        /// <param name="pkm">PKM to modify</param>
        /// <returns>boolean indicating validity</returns>
        private static bool IsValidGenderMismatch(PKM pkm) => pkm.Species switch
        {
            // Shedinja evolution gender glitch, should match original Gender
            (int)Species.Shedinja when pkm.Format == 4 => pkm.Gender == PKX.GetGenderFromPIDAndRatio(pkm.EncryptionConstant, 0x7F), // 50M-50F

            // Evolved from Azurill after transferring to keep gender
            (int)Species.Marill or (int)Species.Azumarill when pkm.Format >= 6 => pkm.Gender == 1 && (pkm.EncryptionConstant & 0xFF) > 0x3F,

            _ => false
        };

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        public static void SetMovesEVs(this PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            // If no moves are requested, just keep the encounter moves
            if (set.Moves[0] != 0)
                pk.SetMoves(set.Moves, true);

            var la = new LegalityAnalysis(pk);
            if (la.Parsed && !pk.WasEvent)
            {
                var relearn = new LegalityAnalysis(pk).GetSuggestedRelearnMoves(enc);
                pk.ClearRelearnMoves();
                pk.SetRelearnMoves(relearn);
            }

            if (pk is IAwakened pb7)
            {
                pb7.SetSuggestedAwakenedValues(pk);
                return;
            }
            // In Generation 1/2 Format sets, when EVs are not specified at all, it implies maximum EVs instead!
            // Under this scenario, just apply maximum EVs (65535).
            if (pk is GBPKM gb && set.EVs.All(z => z == 0))
            {
                gb.EV_HP = gb.EV_ATK = gb.EV_DEF = gb.EV_SPC = gb.EV_SPE = gb.MaxEV;
                return;
            }

            pk.EVs = set.EVs;
        }

        /// <summary>
        /// Set encounter trade IVs for a specific encounter trade
        /// </summary>
        /// <param name="t">EncounterTrade</param>
        /// <param name="pk">Pokemon to modify</param>
        public static void SetEncounterTradeIVs(this EncounterTrade t, PKM pk)
        {
            if (t.IVs.Count != 0)
                pk.SetRandomIVs(t.IVs, 0);
            else
                pk.SetRandomIVs(flawless: 3);
        }

        /// <summary>
        /// Set held items after sanity checking for forms and invalid items
        /// </summary>
        /// <param name="pk">Pokemon to modify</param>
        /// <param name="set">IBattleset to grab the item</param>
        public static void SetHeldItem(this PKM pk, IBattleTemplate set)
        {
            pk.ApplyHeldItem(set.HeldItem, set.Format);
            pk.FixInvalidFormItems(); // arceus, silvally, giratina, genesect fix
            if (!ItemRestrictions.IsHeldItemAllowed(pk) || pk is PB7)
                pk.HeldItem = 0; // Remove the item if the item is illegal in its generation
        }

        /// <summary>
        /// Fix invalid form items
        /// </summary>
        /// <param name="pk">Pokemon to modify</param>
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