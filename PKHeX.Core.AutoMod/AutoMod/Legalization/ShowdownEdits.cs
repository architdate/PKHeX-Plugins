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
        /// <param name="tb"></param>
        public static void FixGender(this PKM pk, IBattleTemplate set, ITracebackHandler tb)
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
            tb.Handle(TracebackType.Gender, $"Set Sane Gender as {(Gender)pk.Gender}");
        }

        public static void SetNature(PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            if (pk.Nature == set.Nature || set.Nature == -1)
                return;
            var val = Math.Min((int)Nature.Quirky, Math.Max((int)Nature.Hardy, set.Nature));
            if (pk.Species == (ushort)Species.Toxtricity)
            {
                if (pk.Form == ToxtricityUtil.GetAmpLowKeyResult(val))
                    pk.Nature = val; // StatNature already set
                if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature != 12 && (pk.StatNature > 24 || pk.StatNature % 6 == 0))
                {
                    // Only Serious Mint for Neutral Natures
                    pk.StatNature = 12;
                }
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
            if (((!ReferenceEquals(enc1, enc2) && enc1 is not EncounterEgg) || la2.Results.Any(z => z.Identifier is CheckIdentifier.Nature or CheckIdentifier.Encounter && !z.Valid)) && enc is not EncounterEgg)
                pk.Nature = orig;
            if (pk.Format >= 8 && pk.StatNature != pk.Nature && pk.StatNature is 0 or 6 or 18 or >= 24)
            {
                // Only Serious Mint for Neutral Natures
                pk.StatNature = (int)Nature.Serious;
            }
        }

        public static void SetAbility(PKM pk, IBattleTemplate set, AbilityPermission preference)
        {
            if (pk.Ability != set.Ability)
                pk.RefreshAbility(pk is PK5 { HiddenAbility: true } ? 2 : pk.AbilityNumber >> 1);
            if (pk.Ability != set.Ability && pk.Context >= EntityContext.Gen6 && set.Ability != -1)
                pk.RefreshAbility(pk is PK5 { HiddenAbility: true } ? 2 : pk.PersonalInfo.GetIndexOfAbility(set.Ability));

            if (preference <= 0)
                return;

            var pi = pk.PersonalInfo;
            var pref = preference.GetSingleValue();
            // Set unspecified abilities
            if (set.Ability == -1)
            {
                pk.RefreshAbility(pref);
                if (pk is PK5 pk5 && preference == AbilityPermission.OnlyHidden)
                    pk5.HiddenAbility = true;
            }
            // Set preferred ability number if applicable
            if (pref == 2 && pi is IPersonalAbility12H h && h.AbilityH == set.Ability)
                pk.AbilityNumber = (int)preference;
            // 3/4/5 transferred to 6+ will have ability 1 if both abilitynum 1 and 2 are the same. Capsule cant convert 1 -> 2 if the abilities arnt unique
            if (pk.Format >= 6 && pk.Generation is 3 or 4 or 5 && pk.AbilityNumber != 4 && pi is IPersonalAbility12 a && a.Ability1 == a.Ability2) pk.AbilityNumber = 1; if (pk is G3PKM && pi is IPersonalAbility12 b && b.Ability1 == b.Ability2)
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
        /// <param name="tb"></param>
        public static void SetSpeciesLevel(
            this PKM pk,
            IBattleTemplate set,
            byte Form,
            IEncounterable enc,
            LanguageID? lang,
            ITracebackHandler tb
        )
        {
            pk.ApplySetGender(set);
            pk.SetRecordFlags(set.Moves, tb); // Set record flags before evolution (TODO: what if middle evolution has exclusive record moves??)

            var evolutionRequired = enc.Species != set.Species;
            var formchange = Form != pk.Form;
            if (evolutionRequired)
            {
                tb.Handle(TracebackType.Species, $"Evolve Pre-Evolution to {set.Species}");
                pk.Species = set.Species;
            }
            if (formchange)
            {
                tb.Handle(TracebackType.Form, $"Fix Form to {Form}");
                pk.Form = Form;
            }

            if ((evolutionRequired || formchange) && pk is IScaledSizeValue sv)
            {
                tb.Handle(TracebackType.Size, "Fix Evolution Height/Weight");
                sv.HeightAbsolute = sv.CalcHeightAbsolute;
                sv.WeightAbsolute = sv.CalcWeightAbsolute;
            }

            // Don't allow invalid tox nature, set random nature first and then statnature later
            if (pk.Species == (int)Species.Toxtricity)
            {
                while (true)
                {
                    var result = ToxtricityUtil.GetAmpLowKeyResult(pk.Nature);
                    if (result == pk.Form)
                    {
                        tb.Handle(TracebackType.Nature, "Toxtricity Nature Reroll");
                        break;
                    }
                    pk.Nature = Util.Rand.Next(25);
                }
            }

            pk.SetSuggestedFormArgument(enc.Species);
            if (evolutionRequired || formchange || pk.Ability != set.Ability)
            {
                tb.Handle(TracebackType.Ability, $"Set Ability after evolution to {set.Ability}");
                var abilitypref = enc.Ability;
                SetAbility(pk, set, abilitypref);
            }
            if (pk.CurrentLevel != set.Level)
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

            if (enc is IFixedTrainer { IsFixedTrainer: true })
            {
                // Set this beforehand in case it is true. Will early return if it is also IFixedNickname
                // Wait for PKHeX to expose this instead of using reflection
            }
            // don't bother checking encountertrade nicknames for length validity
            if (enc is IFixedNickname { IsFixedNickname: true } et)
            {
                // Nickname matches the requested nickname already
                if (pk.Nickname == set.Nickname)
                    return;
                // This should be illegal except Meister Magikarp in BDSP, however trust the user and set corresponding OT
                var nick = et.GetNickname(pk.Language);
                tb.Handle(TracebackType.Encounter, $"Encounter Fixed Nickname set to {nick}");
                pk.Nickname = nick;
                return;
            }

            var gen = enc.Generation;
            var maxlen = Legal.GetMaxLengthNickname(gen, finallang);
            var newnick = RegenUtil.MutateNickname(
                set.Nickname,
                finallang,
                (GameVersion)pk.Version
            );
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
        private static bool IsValidGenderMismatch(PKM pkm) =>
            pkm.Species switch
            {
                // Shedinja evolution gender glitch, should match original Gender
                (int)Species.Shedinja when pkm.Format == 4
                    => pkm.Gender == EntityGender.GetFromPIDAndRatio(pkm.EncryptionConstant, 0x7F), // 50M-50F

                // Evolved from Azurill after transferring to keep gender
                (int)Species.Marill
                or (int)Species.Azumarill when pkm.Format >= 6
                    => pkm.Gender == 1 && (pkm.EncryptionConstant & 0xFF) > 0x3F,

                _ => false,
            };

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        /// <param name="enc">Encounter to reference</param>
        public static void SetMovesEVs(
            this PKM pk,
            IBattleTemplate set,
            IEncounterable enc,
            ITracebackHandler tb
        )
        {
            // If no moves are requested, just keep the encounter moves
            if (set.Moves[0] != 0)
            {
                tb.Handle(TracebackType.Moves, $"Set Requested Moves: {set.Moves}");
                pk.SetMoves(set.Moves, pk is not PA8);
            }

            var la = new LegalityAnalysis(pk);
            // Remove invalid encounter moves (eg. Kyurem Encounter -> Requested Kyurem black)
            if (set.Moves[0] == 0 && la.Info.Moves.Any(z => z.Judgement == Severity.Invalid))
            {
                Span<ushort> moves = stackalloc ushort[4];
                la.GetSuggestedCurrentMoves(moves);
                pk.SetMoves(moves, pk is not PA8);
                pk.FixMoves();
                tb.Handle(TracebackType.Moves, "Fix Invalid Moves based on suggested moves");
            }

            if (la.Parsed && !pk.FatefulEncounter)
            {
                // For dexnav. Certain encounters come with "random" relearn moves, and our requested moves might require one of them.
                Span<ushort> moves = stackalloc ushort[4];
                la.GetSuggestedRelearnMoves(moves, enc);
                pk.ClearRelearnMoves();
                pk.SetRelearnMoves(moves);
                tb.Handle(TracebackType.Moves, "Set Relearn Moves for encounter");
            }
            la = new LegalityAnalysis(pk);
            if (la.Info.Relearn.Any(z => z.Judgement == Severity.Invalid))
            {
                pk.ClearRelearnMoves();
                tb.Handle(TracebackType.Moves, "Clear Invalid Relean Moves");
            }

            if (pk is IAwakened)
            {
                pk.SetAwakenedValues(set);
                tb.Handle(TracebackType.AVs, "Set Awakened Values");
                return;
            }
            // In Generation 1/2 Format sets, when EVs are not specified at all, it implies maximum EVs instead!
            // Under this scenario, just apply maximum EVs (65535).
            if (pk is GBPKM gb && set.EVs.All(z => z == 0))
            {
                gb.EV_HP = gb.EV_ATK = gb.EV_DEF = gb.EV_SPC = gb.EV_SPE = gb.MaxEV;
                tb.Handle(TracebackType.EVs, "Set EVs for GBPKM");
                return;
            }

            pk.SetEVs(set.EVs);
            tb.Handle(TracebackType.EVs, "Set Requested EVs");
        }

        /// <summary>
        /// Set encounter trade IVs for a specific encounter trade
        /// </summary>
        /// <param name="t">EncounterTrade</param>
        /// <param name="pk">Pokemon to modify</param>
        public static void SetEncounterTradeIVs(this PKM pk)
        {
            pk.SetRandomIVs(minFlawless: 3);
        }

        /// <summary>
        /// Set held items after sanity checking for forms and invalid items
        /// </summary>
        /// <param name="pk">Pokemon to modify</param>
        /// <param name="set">IBattleset to grab the item</param>
        public static void SetHeldItem(this PKM pk, IBattleTemplate set, ITracebackHandler tb)
        {
            pk.ApplyHeldItem(set.HeldItem, set.Context);
            pk.FixInvalidFormItems(); // arceus, silvally, giratina, genesect fix
            if (!ItemRestrictions.IsHeldItemAllowed(pk) || pk is PB7)
                pk.HeldItem = 0; // Remove the item if the item is illegal in its generation
            if (set.HeldItem != pk.HeldItem)
                tb.Handle(TracebackType.Item, $"Modified item to {pk.HeldItem}");
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
                case Species.Silvally:
                case Species.Genesect:
                    bool valid = FormItem.TryGetForm(pk.Species, pk.HeldItem, pk.Format, out byte pkform);
                    if (!valid)
                        break;
                    pk.HeldItem = pk.Form != pkform ? 0 : pk.HeldItem;
                    pk.Form = pk.Form != pkform ? (byte)0 : pkform;
                    break;
                case Species.Giratina
                    when pk.Form == 1 && pk.HeldItem != 112 && pk.HeldItem != 1779:
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
                case Species.Ogerpon:
                    pk.HeldItem = FormItem.GetItemOgerpon(pk.Form);
                    break;
            }
        }

        public static MoveType GetValidOpergonTeraType(byte form) =>
            (form & 3) switch
            {
                0 => MoveType.Grass,
                1 => MoveType.Water,
                2 => MoveType.Fire,
                3 => MoveType.Rock,
                _ => (MoveType)TeraTypeUtil.OverrideNone,
            };

        /// <summary>
        /// Randomizes the IVs within game constraints.
        /// </summary>
        /// <param name="template">IV template to generate from</param>
        /// <param name="minFlawless">Count of flawless IVs to set. If none provided, a count will be detected.</param>
        /// <returns>Randomized IVs if desired.</returns>
        private static void SetRandomIVsTemplate(
            this PKM pk,
            IndividualValueSet template,
            int minFlawless = 0
        )
        {
            Span<int> ivs = stackalloc int[6];
            var rnd = Util.Rand;
            do
            {
                for (int i = 0; i < 6; i++)
                    ivs[i] = template[i] < 0 ? rnd.Next(31 + 1) : template[i];
            } while (ivs.Count(31) < minFlawless);

            pk.IV_HP = ivs[0];
            pk.IV_ATK = ivs[1];
            pk.IV_DEF = ivs[2];
            pk.IV_SPE = ivs[3];
            pk.IV_SPA = ivs[4];
            pk.IV_SPD = ivs[5];
        }
    }
}
