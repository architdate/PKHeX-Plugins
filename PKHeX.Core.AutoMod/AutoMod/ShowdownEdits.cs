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

            if (pk.Gender != 0 && pk.Gender != 1)
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
            var val = Math.Min((int)Nature.Quirky, Math.Max((int)Nature.Hardy, set.Nature));
            pk.SetNature(val);
            if (pk.Species == (int)Species.Toxtricity)
            {
                if (pk.AltForm == EvolutionMethod.GetAmpLowKeyResult(val))
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
            if ((!ReferenceEquals(enc1, enc2) && !(enc1 is EncounterEgg)) || la2.Info.Parse.Any(z => z.Identifier == CheckIdentifier.Nature && !z.Valid))
                pk.Nature = orig;
            if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature != 12 && (pk.StatNature > 24 || pk.StatNature % 6 == 0)) // Only Serious Mint for Neutral Natures
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
        public static void SetSpeciesLevel(this PKM pk, IBattleTemplate set, int Form, IEncounterable enc)
        {
            pk.Species = set.Species;
            pk.ApplySetGender(set);
            pk.SetAltForm(Form);
            pk.SetFormArgument(enc);
            pk.RefreshAbility(pk.AbilityNumber >> 1);

            var gen = new LegalityAnalysis(pk).Info.Generation;
            var nickname = Legal.GetMaxLengthNickname(gen, LanguageID.English) < set.Nickname.Length ? set.Nickname.Substring(0, Legal.GetMaxLengthNickname(gen, LanguageID.English)) : set.Nickname;
            if (!WordFilter.IsFiltered(nickname, out _))
                pk.SetNickname(nickname);
            else
                pk.ClearNickname();
            pk.CurrentLevel = set.Level;
            if (pk.CurrentLevel == 50)
                pk.CurrentLevel = 100; // VGC Override
        }

        private static void SetFormArgument(this PKM pk, IEncounterable enc)
        {
            if (pk is IFormArgument f)
                f.FormArgument = GetSuggestedFormArgument(pk, enc.Species);
        }

        public static uint GetSuggestedFormArgument(PKM pk, int origSpecies = 0)
        {
            return pk.Species switch
            {
                (int)Species.Hoopa when pk.AltForm != 0 => 3,
                (int)Species.Furfrou when pk.AltForm != 0 => 5,
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
                if (pk is GBPKM gb && set is RegenTemplate rt && rt.ChangeEVsAllowed && set.EVs.All(z => z == 0))
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
                    int forma = GetArceusFormFromHeldItem(pk.HeldItem, pk.Format);
                    pk.HeldItem = pk.AltForm != forma ? 0 : pk.HeldItem;
                    pk.AltForm = pk.AltForm != forma ? 0 : forma;
                    break;
                case Species.Silvally:
                    int forms = GetSilvallyFormFromHeldItem(pk.HeldItem);
                    pk.HeldItem = pk.AltForm != forms ? 0 : pk.HeldItem;
                    pk.AltForm = pk.AltForm != forms ? 0 : forms;
                    break;
                case Species.Genesect:
                    int formg = GetGenesectFormFromHeldItem(pk.HeldItem);
                    pk.HeldItem = pk.AltForm != formg ? 0 : pk.HeldItem;
                    pk.AltForm = pk.AltForm != formg ? 0 : formg;
                    break;
                case Species.Giratina when pk.AltForm == 1 && pk.HeldItem != 112:
                    pk.HeldItem = 122;
                    break;
            }
        }

        private static int GetArceusFormFromHeldItem(int item, int format)
        {
            if (777 <= item && item <= 793)
                return Array.IndexOf(Legal.Arceus_ZCrystal, (ushort)item) + 1;

            int form = 0;
            if ((298 <= item && item <= 313) || item == 644)
                form = Array.IndexOf(Legal.Arceus_Plate, (ushort)item) + 1;
            if (format == 4 && form >= 9)
                return form + 1; // ??? type Form shifts everything by 1
            return form;
        }

        private static int GetSilvallyFormFromHeldItem(int item)
        {
            if ((904 <= item && item <= 920) || item == 644)
                return item - 903;
            return 0;
        }

        private static int GetGenesectFormFromHeldItem(int item)
        {
            if (116 <= item && item <= 119)
                return item - 115;
            return 0;
        }
    }
}