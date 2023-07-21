using System;
using System.Globalization;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Modifications for a <see cref="PKM"/> based on a <see cref="IBattleTemplate"/>
    /// </summary>
    public static class ShowdownEdits
    {
        private static readonly CompareInfo CompareInfo = CultureInfo.CurrentCulture.CompareInfo;

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
        /// <param name="enc">Encounter to reference</param>
        /// <param name="preference">Ability index (1/2/4) preferred; &lt;= 0 for any</param>
        public static void SetNatureAbility(this PKM pk, IBattleTemplate set, IEncounterable enc, AbilityPermission preference = AbilityPermission.Any12H)
        {
            SetNature(pk, set, enc);
            SetAbility(pk, set, preference);
        }

        private static void SetNature(PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            if (pk.Nature == set.Nature || set.Nature == -1)
                return;
            var val = Math.Min((int)Nature.Quirky, Math.Max((int)Nature.Hardy, set.Nature));
            if (pk.Species == (ushort)Species.Toxtricity)
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
            if (((!ReferenceEquals(enc1, enc2) && enc1 is not EncounterEgg) ||
                la2.Results.Any(z => (z.Identifier == CheckIdentifier.Nature || z.Identifier == CheckIdentifier.Encounter) && !z.Valid)) && enc is not EncounterEgg)
                pk.Nature = orig;
            if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature is 0 or 6 or 18 or >= 24) // Only Serious Mint for Neutral Natures
                pk.StatNature = (int)Nature.Serious;
        }

        private static void SetAbility(PKM pk, IBattleTemplate set, AbilityPermission preference)
        {
            if (pk.Ability != set.Ability && set.Ability != -1)
                pk.SetAbility(set.Ability);

            if (preference <= 0)
                return;

            var pi = pk.PersonalInfo;
            var pref = preference.GetSingleValue();
            // Set unspecified abilities
            if (set.Ability == -1)
            {
                pk.RefreshAbility(pref);
                if (pk is PK5 pk5 && preference == AbilityPermission.OnlyHidden) pk5.HiddenAbility = true;
            }
            // Set preferred ability number if applicable
            if (pref == 2 && pi is IPersonalAbility12H h && h.AbilityH == set.Ability)
                pk.AbilityNumber = (int)preference;
            // 3/4/5 transferred to 6+ will have ability 1 if both abilitynum 1 and 2 are the same. Capsule cant convert 1 -> 2 if the abilities arnt unique
            if (pk.Format >= 6 && pk.Generation is 3 or 4 or 5 && pk.AbilityNumber != 4 && pi is IPersonalAbility12 a && a.Ability1 == a.Ability2)
                pk.AbilityNumber = 1;
            if (pk is G3PKM && pi is IPersonalAbility12 b && b.Ability1 == b.Ability2)
                pk.AbilityNumber = 1;
        }

        /// <summary>
        /// Set Species and Level with nickname (Helps with PreEvos)
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Set to use as reference</param>
        /// <param name="Form">Form to apply</param>
        /// <param name="enc">Encounter detail</param>
        /// <param name="lang">Language to apply</param>
        public static void SetSpeciesLevel(this PKM pk, IBattleTemplate set, byte Form, IEncounterable enc, LanguageID? lang = null)
        {
            pk.ApplySetGender(set);
            pk.SetRecordFlags(set.Moves); // Set record flags before evolution (TODO: what if middle evolution has exclusive record moves??)

            var evolutionRequired = pk.Species != set.Species;
            var formchange = Form != pk.Form;
            if (evolutionRequired)
                pk.Species = set.Species;
            if (formchange)
                pk.Form = Form;

            if ((evolutionRequired || formchange) && pk is IScaledSizeValue sv)
            {
                sv.HeightAbsolute = sv.CalcHeightAbsolute;
                sv.WeightAbsolute = sv.CalcWeightAbsolute;
            }

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
            if (set.Level != 100 && set.Level == enc.LevelMin && pk.Format is 3 or 4)
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
            if (enc is EncounterTrade { HasNickname: true } et)
            {
                // Nickname matches the requested nickname already
                if (pk.Nickname == set.Nickname)
                    return;
                // This should be illegal except Meister Magikarp in BDSP, however trust the user and set corresponding OT
                const CompareOptions options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth;
                var index = et.Nicknames.ToList().FindIndex(z => CompareInfo.Compare(set.Nickname, z, options) == 0);
                if (index >= 0)
                {
                    pk.Nickname = et.Nicknames[index];
                    if (pk.Format >= 3)
                        pk.OT_Name = et.TrainerNames[index];
                    return;
                }
            }

            var gen = enc.Generation;
            var maxlen = Legal.GetMaxLengthNickname(gen, finallang);
            var newnick = RegenUtil.MutateNickname(set.Nickname, finallang, (GameVersion)pk.Version);
            var nickname = newnick.Length > maxlen ? newnick[..maxlen] : newnick;
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
            pk.Gender = set.Gender != -1 ? set.Gender : pk.GetSaneGender();
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
            if (SpeciesCategory.IsFixedGenderFromDual(pkm.Species))
                return IsValidFixedGenderFromBiGender(pkm, enc.Species);

            return true;
        }

        /// <summary>
        /// Helper function to check if bigender => fixed gender evolution is valid
        /// </summary>
        /// <param name="pkm">pkm to modify</param>
        /// <param name="original">original species (encounter)</param>
        /// <returns>boolean indicating validaity</returns>
        private static bool IsValidFixedGenderFromBiGender(PKM pkm, ushort original)
        {
            var current = pkm.Gender;
            if (current == 2) // shedinja, genderless
                return true;
            var gender = EntityGender.GetFromPID(original, pkm.EncryptionConstant);
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
            (int)Species.Shedinja when pkm.Format == 4 => pkm.Gender == EntityGender.GetFromPIDAndRatio(pkm.EncryptionConstant, 0x7F), // 50M-50F

            // Evolved from Azurill after transferring to keep gender
            (int)Species.Marill or (int)Species.Azumarill when pkm.Format >= 6 => pkm.Gender == 1 && (pkm.EncryptionConstant & 0xFF) > 0x3F,

            _ => false,
        };

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        /// <param name="enc">Encounter to reference</param>
        public static void SetMovesEVs(this PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            // If no moves are requested, just keep the encounter moves
            if (set.Moves[0] != 0)
                pk.SetMoves(set.Moves, pk is not PA8);

            var la = new LegalityAnalysis(pk);
            // Remove invalid encounter moves (eg. Kyurem Encounter -> Requested Kyurem black)
            if (set.Moves[0] == 0 && la.Info.Moves.Any(z => z.Judgement == Severity.Invalid))
            {
                Span<ushort> moves = stackalloc ushort[4];
                la.GetSuggestedCurrentMoves(moves);
                pk.SetMoves(moves, pk is not PA8);
                pk.FixMoves();
            }

            if (la.Parsed && !pk.FatefulEncounter)
            {
                // For dexnav. Certain encounters come with "random" relearn moves, and our requested moves might require one of them.
                Span<ushort> moves = stackalloc ushort[4];
                la.GetSuggestedRelearnMoves(moves, enc);
                pk.ClearRelearnMoves();
                pk.SetRelearnMoves(moves);
            }
            la = new LegalityAnalysis(pk);
            if (la.Info.Relearn.Any(z => z.Judgement == Severity.Invalid))
                pk.ClearRelearnMoves();

            if (pk is IAwakened)
            {
                pk.SetAwakenedValues(set);
                return;
            }
            // In Generation 1/2 Format sets, when EVs are not specified at all, it implies maximum EVs instead!
            // Under this scenario, just apply maximum EVs (65535).
            if (pk is GBPKM gb && set.EVs.All(z => z == 0))
            {
                gb.EV_HP = gb.EV_ATK = gb.EV_DEF = gb.EV_SPC = gb.EV_SPE = gb.MaxEV;
                return;
            }

            pk.SetEVs(set.EVs);
        }

        /// <summary>
        /// Set encounter trade IVs for a specific encounter trade
        /// </summary>
        /// <param name="t">EncounterTrade</param>
        /// <param name="pk">Pokemon to modify</param>
        public static void SetEncounterTradeIVs(this EncounterTrade t, PKM pk)
        {
            if (t.IVs.IsSpecified)
                pk.SetRandomIVsTemplate(t.IVs, 0);
            else
                pk.SetRandomIVs(minFlawless: 3);
        }

        /// <summary>
        /// Set held items after sanity checking for forms and invalid items
        /// </summary>
        /// <param name="pk">Pokemon to modify</param>
        /// <param name="set">IBattleset to grab the item</param>
        public static void SetHeldItem(this PKM pk, IBattleTemplate set)
        {
            pk.ApplyHeldItem(set.HeldItem, set.Context);
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
            // Ignore games where items don't exist in the first place. They would still allow forms
            if (pk.LA)
                return;

            switch ((Species)pk.Species)
            {
                case Species.Arceus:
                    byte forma = FormVerifier.GetArceusFormFromHeldItem(pk.HeldItem, pk.Format);
                    pk.HeldItem = pk.Form != forma ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != forma ? (byte)0 : forma;
                    break;
                case Species.Silvally:
                    byte forms = FormVerifier.GetSilvallyFormFromHeldItem(pk.HeldItem);
                    pk.HeldItem = pk.Form != forms ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != forms ? (byte)0 : forms;
                    break;
                case Species.Genesect:
                    byte formg = FormVerifier.GetGenesectFormFromHeldItem(pk.HeldItem);
                    pk.HeldItem = pk.Form != formg ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != formg ? (byte)0 : formg;
                    break;
                case Species.Giratina when pk.Form == 1 && pk.HeldItem != 112 && pk.HeldItem != 1779:
                    if (pk.Context >= EntityContext.Gen9)
                        pk.HeldItem = 1779;
                    else
                        pk.HeldItem = 112;
                    break;
                case Species.Dialga when pk.Form == 1 && pk.HeldItem != 1777:
                    pk.HeldItem = 1777;
                    break;
                case Species.Palkia when pk.Form == 1 && pk.HeldItem != 1778:
                    pk.HeldItem = 1778;
                    break;
            }
        }
    }
}