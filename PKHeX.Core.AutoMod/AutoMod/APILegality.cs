using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="IBattleTemplate"/>.
    /// </summary>
    public static class APILegality
    {
        /// <summary>
        /// Settings
        /// </summary>
        public static bool UseTrainerData { get; set; } = true;
        public static bool SetMatchingBalls { get; set; } = true;
        public static bool ForceSpecifiedBall { get; set; }
        public static bool SetAllLegalRibbons { get; set; } = true;
        public static bool UseCompetitiveMarkings { get; set; }
        public static bool UseMarkings { get; set; } = true;
        public static bool EnableDevMode { get; set; } = false;
        public static string LatestAllowedVersion { get; set; } = "0.0.0.0";
        public static bool PrioritizeGame { get; set; } = true;
        public static GameVersion PrioritizeGameVersion { get; set; }
        public static bool SetBattleVersion { get; set; }
        public static bool AllowTrainerOverride { get; set; }
        public static bool AllowBatchCommands { get; set; } = true;
        public static bool ForceLevel100for50 { get; set; } = true;
        public static bool AllowHOMETransferGeneration { get; set; } = true;
        public static int Timeout { get; set; } = 15;

        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <remarks>Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="IBattleTemplate"/>.</remarks>
        /// <param name="dest">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is legal or not</param>
        public static PKM GetLegalFromTemplate(this ITrainerInfo dest, PKM template, IBattleTemplate set, out LegalizationResult satisfied, bool nativeOnly = false)
        {
            RegenSet regen;
            if (set is RegenTemplate t)
            {
                t.FixGender(template.PersonalInfo);
                regen = t.Regen;
            }
            else
            {
                regen = RegenSet.Default;
            }

            if (template.Version == 0)
                template.Version = dest.Game;

            template.ApplySetDetails(set);
            template.SetRecordFlags(Array.Empty<ushort>()); // Validate TR/MS moves for the encounter

            if (template.Species == (ushort)Species.Unown) // Force unown form on template
                template.Form = set.Form;

            var abilityreq = GetRequestedAbility(template, set);
            var batchedit = AllowBatchCommands && regen.HasBatchSettings;
            var native = ModLogic.cfg.NativeOnly && nativeOnly;
            var destType = template.GetType();
            var destVer = (GameVersion)dest.Game;
            if (destVer <= 0 && dest is SaveFile s)
                destVer = s.Version;

            var timer = Stopwatch.StartNew();
            var gamelist = FilteredGameList(template, destVer, AllowBatchCommands, set, native);
            if (dest.Generation <= 2)
                template.EXP = 0; // no relearn moves in gen 1/2 so pass level 1 to generator

            var encounters = GetAllEncounters(pk: template, moves: new ReadOnlyMemory<ushort>(set.Moves), gamelist);
            var criteria = EncounterCriteria.GetCriteria(set, template.PersonalInfo);
            criteria.ForceMinLevelRange = true;
            if (regen.EncounterFilters.Count() != 0)
                encounters = encounters.Where(enc => BatchEditing.IsFilterMatch(regen.EncounterFilters, enc));

            PKM? last = null;
            foreach (var enc in encounters)
            {
                // Return out if set times out
                if (timer.Elapsed.TotalSeconds >= Timeout)
                {
                    timer.Stop();
                    satisfied = LegalizationResult.Timeout;
                    return template;
                }

                // Look before we leap -- don't waste time generating invalid / incompatible junk.
                if (!IsEncounterValid(set, enc, abilityreq, destVer))
                    continue;

                if (enc is IFixedNature { IsFixedNature: true } fixedNature)
                    criteria = criteria with { Nature = Nature.Random };
                criteria = SetSpecialCriteria(criteria, enc, set);

                // Create the PKM from the template.
                var tr = SimpleEdits.IsUntradeableEncounter(enc) ? dest : GetTrainer(regen, enc, set);
                var raw = enc.GetPokemonFromEncounter(tr, criteria, set);
                if (raw.OT_Name.Length == 0)
                {
                    raw.Language = tr.Language;
                    tr.ApplyTo(raw);
                }
                raw = raw.SanityCheckLocation(enc);
                if (raw.IsEgg) // PGF events are sometimes eggs. Force hatch them before proceeding
                    raw.HandleEggEncounters(enc, tr);

                raw.PreSetPIDIV(enc, set, criteria);

                // Transfer any VC1 via VC2, as there may be GSC exclusive moves requested.
                if (dest.Generation >= 7 && raw is PK1 basepk1)
                    raw = basepk1.ConvertToPK2();

                // Bring to the target generation and filter
                var pk = EntityConverter.ConvertToType(raw, destType, out _);
                if (pk == null)
                    continue;
                if (EntityConverter.IsIncompatibleGB(pk, template.Japanese, pk.Japanese))
                    continue;

                if (HomeTrackerUtil.IsRequired(enc, pk) && !AllowHOMETransferGeneration)
                    continue;

                // Apply final details
                ApplySetDetails(pk, set, dest, enc, regen);

                // Apply final tweaks to the data.
                if (pk is IGigantamax gmax && gmax.CanGigantamax != set.CanGigantamax)
                {
                    if (!gmax.CanToggleGigantamax(pk.Species, pk.Form, enc.Species, enc.Form))
                        continue;
                    gmax.CanGigantamax = set.CanGigantamax; // soup hax
                }

                // Try applying batch editor values.
                if (batchedit)
                {
                    pk.RefreshChecksum();
                    var b = regen.Batch;
                    BatchEditing.ScreenStrings(b.Filters);
                    BatchEditing.ScreenStrings(b.Instructions);
                    var modified = BatchEditing.TryModify(pk, b.Filters, b.Instructions);
                    if (!modified && b.Filters.Count > 0)
                        continue;
                    pk.ApplyPostBatchFixes();
                }

                if (pk is PK1 pk1 && pk1.TradebackValid())
                {
                    satisfied = LegalizationResult.Regenerated;
                    return pk;
                }

                // Verify the Legality of what we generated, and exit if it is valid.
                var la = new LegalityAnalysis(pk);
                if (la.Valid && pk.Species == set.Species) // Encounter Trades that evolve may cause higher tahn expected species
                {
                    satisfied = LegalizationResult.Regenerated;
                    return pk;
                }
                last = pk;
                Debug.WriteLine($"{la.Report()}\n");
            }
            satisfied = LegalizationResult.Failed;
            return last ?? template;
        }

        private static PKM GetPokemonFromEncounter(this IEncounterable enc, ITrainerInfo tr, EncounterCriteria criteria, IBattleTemplate set)
        {
            var basepkm = enc.ConvertToPKM(tr, criteria);

            // If the encounter is a Wurmple, we need to make sure the evolution is correct.
            if (enc.Species == (int)Species.Wurmple && set.Species != (int)Species.Wurmple)
            {
                var wdest = WurmpleUtil.GetWurmpleEvoGroup(set.Species);
                while (WurmpleUtil.GetWurmpleEvoVal(basepkm.PID) != wdest)
                    basepkm = enc.ConvertToPKM(tr, criteria);
            }

            return basepkm;
        }

        private static IEnumerable<IEncounterable> GetAllEncounters(PKM pk, ReadOnlyMemory<ushort> moves, IReadOnlyList<GameVersion> vers)
        {
            var orig_encs = EncounterMovesetGenerator.GenerateEncounters(pk, moves, vers);
            foreach (var enc in orig_encs)
                yield return enc;
            var pi = pk.PersonalInfo;
            var orig_form = pk.Form;
            var fc = pi.FormCount;
            if (fc == 0) // not present in game
            {
                // try again using past-gen table
                pi = PersonalTable.USUM.GetFormEntry(pk.Species, 0);
                fc = pi.FormCount;
            }
            for (byte f = 0; f < fc; f++)
            {
                if (f == orig_form)
                    continue;
                if (FormInfo.IsBattleOnlyForm(pk.Species, f, pk.Format))
                    continue;
                pk.Form = f;
                pk.SetGender(pk.GetSaneGender());
                var encs = EncounterMovesetGenerator.GenerateEncounters(pk, moves, vers);
                foreach (var enc in encs)
                    yield return enc;
            }
        }

        public static AbilityRequest GetRequestedAbility(PKM template, IBattleTemplate set)
        {
            if (template.AbilityNumber == 4)
                return AbilityRequest.Hidden;

            var pi = template.PersonalInfo;
            var abils_count = pi.AbilityCount;
            if (abils_count <= 2 || pi is not IPersonalAbility12H h)
                return AbilityRequest.NotHidden;

            if (h.AbilityH == template.Ability)
                return AbilityRequest.PossiblyHidden;

            // if no set ability is specified, it is assumed as the first ability which can be the same as the HA
            if (set.Ability == -1 && h.Ability1 == h.AbilityH)
                return AbilityRequest.PossiblyHidden;

            var default_ability = set.Ability == -1 ? AbilityRequest.Any : AbilityRequest.NotHidden;  // Will allow any ability if ability is unspecified
            return default_ability;
        }

        private static bool TradebackValid(this PK1 pk1)
        {
            var valid = new LegalityAnalysis(pk1).Valid;
            if (!valid)
                pk1.Catch_Rate = (byte)pk1.Gen2Item;
            return valid;
        }

        /// <summary>
        /// Filter down the gamelist to search based on requested sets
        /// </summary>
        /// <param name="template">Template pokemon with basic details set</param>
        /// <param name="destVer">Version in which the pokemon needs to be imported</param>
        /// <param name="batchEdit">Whether settings currently allow batch commands</param>
        /// <param name="set">Set information to be used to filter the game list</param>
        /// <param nativeOnly="set">Whether to only return encounters from the current version</param>
        /// <returns>List of filtered games to check encounters for</returns>
        internal static GameVersion[] FilteredGameList(PKM template, GameVersion destVer, bool batchEdit, IBattleTemplate set, bool nativeOnly = false)
        {
            if (batchEdit && set is RegenTemplate { Regen.VersionFilters: { Count: not 0 } x } && TryGetSingleVersion(x, out var single))
                return single;

            var gamelist = !nativeOnly
                           ? GameUtil.GetVersionsWithinRange(template, template.Format).OrderByDescending(c => c.GetGeneration()).ToArray()
                           : GetPairedVersions(destVer);

            if (PrioritizeGame && !nativeOnly)
                gamelist = PrioritizeGameVersion == GameVersion.Any ? PrioritizeVersion(gamelist, SimpleEdits.GetIsland(destVer)) : PrioritizeVersion(gamelist, PrioritizeGameVersion);

            if (template.AbilityNumber == 4 && destVer.GetGeneration() < 8)
                gamelist = gamelist.Where(z => z.GetGeneration() is not 3 and not 4).ToArray();
            return gamelist;
        }

        private static bool TryGetSingleVersion(IReadOnlyList<StringInstruction> filters, [NotNullWhen(true)] out GameVersion[]? gamelist)
        {
            gamelist = null;
            foreach (var filter in filters)
            {
                if (filter.PropertyName != nameof(IVersion.Version))
                    continue;

                GameVersion value;
                if (int.TryParse(filter.PropertyValue, out var i))
                    value = (GameVersion)i;
                else if (Enum.TryParse<GameVersion>(filter.PropertyValue, out var g))
                    value = g;
                else
                    return false;

                GameVersion[] result;
                if (value.IsValidSavedVersion())
                    result = new GameVersion[] { value };
                else
                    result = GameUtil.GameVersions.Where(z => value.Contains(z)).ToArray();

                gamelist = filter.Comparer switch
                {
                    InstructionComparer.IsEqual => result,
                    InstructionComparer.IsNotEqual => GameUtil.GameVersions.Where(z => !result.Contains(z)).ToArray(),
                    InstructionComparer.IsGreaterThan => GameUtil.GameVersions.Where(z => result.Any(g => z > g)).ToArray(),
                    InstructionComparer.IsGreaterThanOrEqual => GameUtil.GameVersions.Where(z => result.Any(g => z >= g)).ToArray(),
                    InstructionComparer.IsLessThan => GameUtil.GameVersions.Where(z => result.Any(g => z < g)).ToArray(),
                    InstructionComparer.IsLessThanOrEqual => GameUtil.GameVersions.Where(z => result.Any(g => z <= g)).ToArray(),
                    _ => result,
                };
                return gamelist.Length != 0;
            }
            return false;
        }

        /// <summary>
        /// Grab a trainer from trainer database with mutated language
        /// </summary>
        /// <param name="regen">Regenset</param>
        /// <param name="ver">Gameversion for the saved trainerdata</param>
        /// <param name="gen">Generation of the saved trainerdata</param>
        /// <returns>ITrainerInfo of the trainerdetails</returns>
        private static ITrainerInfo GetTrainer(RegenSet regen, IEncounterable enc, IBattleTemplate set)
        {
            var ver = enc.Version;
            var gen = enc.Generation;
            var mutate = regen.Extra.Language;

            // Edge case override for Meister Magikarp
            var nicknames = new string[] { "", "ポッちゃん", "Foppa", "Bloupi", "Mossy", "Pador", "", "", "", "", "" };
            var idx = Array.IndexOf(nicknames, set.Nickname);
            if (idx > 0)
                mutate = (LanguageID)idx;

            if (AllowTrainerOverride && regen.HasTrainerSettings && regen.Trainer != null)
                return regen.Trainer.MutateLanguage(mutate, ver);
            if (UseTrainerData)
                return TrainerSettings.GetSavedTrainerData(ver, gen).MutateLanguage(mutate, ver);
            return TrainerSettings.DefaultFallback(ver, regen.Extra.Language);
        }

        /// <summary>
        /// Gives the currently loaded save priority over other saves in the same generation. Otherwise generational order is preserved
        /// </summary>
        /// <param name="gamelist">Array of gameversions which needs to be prioritized</param>
        /// <param name="game">Gameversion to prioritize</param>
        /// <returns>A prioritized gameversion list</returns>
        private static GameVersion[] PrioritizeVersion(GameVersion[] gamelist, GameVersion game)
        {
            var matched = 0;
            var retval = new List<GameVersion>();
            foreach (GameVersion poss in gamelist)
            {
                if (poss == game || game.Contains(poss))
                {
                    retval.Insert(matched, poss);
                    matched++;
                }
                else
                {
                    retval.Add(poss);
                }
            }
            return retval.ToArray();
        }

        /// <summary>
        /// Checks if the encounter is even valid before processing it
        /// </summary>
        /// <param name="set">showdown set</param>
        /// <param name="enc">encounter object</param>
        /// <param name="abilityreq">is HA requested</param>
        /// <param name="destVer">version to generate in</param>
        /// <returns>if the encounter is valid or not</returns>
        private static bool IsEncounterValid(IBattleTemplate set, IEncounterable enc, AbilityRequest abilityreq, GameVersion destVer)
        {
            // Don't process if encounter min level is higher than requested level
            if (!IsRequestedLevelValid(set, enc))
                return false;

            // Don't process if the ball requested is invalid
            if (!IsRequestedBallValid(set, enc))
                return false;

            // Don't process if encounter and set shinies dont match
            if (!IsRequestedShinyValid(set, enc))
                return false;

            // Don't process if the requested set is Alpha and the Encounter is not
            if (!IsRequestedAlphaValid(set, enc))
                return false;

            // Don't process if the gender does not match the set
            if (set.Gender != -1 && enc is IFixedGender { IsFixedGender: true } fg && fg.Gender != set.Gender)
                return false;

            // Don't process if PKM is definitely Hidden Ability and the PKM is from Gen 3 or Gen 4 and Hidden Capsule doesn't exist
            var gen = enc.Generation;
            if (abilityreq == AbilityRequest.Hidden && gen is 3 or 4 && destVer.GetGeneration() < 8)
                return false;

            if (set.Species == (ushort)Species.Pikachu)
            {
                switch (enc.Generation)
                {
                    case 6 when set.Form != (enc is EncounterStatic6 ? enc.Form : 0):
                    case >= 7 when set.Form != (enc is EncounterInvalid or EncounterEgg ? 0 : enc.Form):
                        return false;
                }
            }

            return destVer.ExistsInGame(set.Species, set.Form);
        }

        public static bool IsRequestedLevelValid(IBattleTemplate set, IEncounterable enc)
        {
            if (enc.Generation <= 2)
                return true;
            if (enc.LevelMin > enc.LevelMax)
                return false;
            if (enc.LevelMin > set.Level)
            {
                var isRaid = enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U;
                if (enc is EncounterSlot6AO s)
                {
                    if (s.LevelMin - 4 > set.Level)
                        return false;
                }
                else if (!isRaid)
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsRequestedBallValid(IBattleTemplate set, IEncounterable enc)
        {
            if (set is RegenTemplate rt && enc.FixedBall != Ball.None && ForceSpecifiedBall)
            {
                var reqball = rt.Regen.Extra.Ball;
                if (reqball != enc.FixedBall && reqball != Ball.None)
                    return false;
            }
            return true;
        }

        public static bool IsRequestedAlphaValid(IBattleTemplate set, IEncounterable enc)
        {
            // No Alpha setting in base showdown
            if (set is not RegenTemplate rt)
                return true;

            // Check alpha request
            var requested = false;
            if (rt.Regen.HasExtraSettings)
                requested = rt.Regen.Extra.Alpha;

            // Requested alpha but encounter isn't an alpha
            if (enc is not IAlphaReadOnly a)
                return !requested;

            return a.IsAlpha == requested;
        }

        public static bool IsRequestedShinyValid(IBattleTemplate set, IEncounterable enc)
        {
            if (enc is MysteryGift mg && mg.CardID >= 9000)
                return true;

            // Don't process if shiny value doesnt match
            if (set.Shiny && enc.Shiny == Shiny.Never)
                return false;
            if (!set.Shiny && enc.Shiny.IsShiny())
                return false;

            // Further shiny filtering if set is regentemplate
            if (set is RegenTemplate regent && regent.Regen.HasExtraSettings)
            {
                var shinytype = regent.Regen.Extra.ShinyType;
                if (shinytype == Shiny.AlwaysStar && enc.Shiny == Shiny.AlwaysSquare)
                    return false;
                if (shinytype == Shiny.AlwaysSquare && enc.Shiny == Shiny.AlwaysStar)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Method to check if PIDIV has already set
        /// </summary>
        /// <param name="pk">pkm to check</param>
        /// <param name="enc">enc to check</param>
        /// <returns></returns>
        public static bool IsPIDIVSet(PKM pk, IEncounterable enc)
        {
            // If PID and IV is handled in PreSetPIDIV, don't set it here again and return out
            if (enc is ITeraRaid9)
                return true;
            if (enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U)
                return true;
            if (enc is IOverworldCorrelation8 o && o.GetRequirement(pk) == OverworldCorrelation8Requirement.MustHave)
                return true;
            if (enc is IStaticCorrelation8b s && s.GetRequirement(pk) == StaticCorrelation8bRequirement.MustHave)
                return true;
            if (enc is EncounterEgg && GameVersion.BDSP.Contains(enc.Version))
                return true;
            return false;
        }

        /// <summary>
        /// Sanity checking locations before passing them into ApplySetDetails.
        /// Some encounters may have an empty met location leading to an encounter mismatch. Use this function for all encounter pre-processing!
        /// </summary>
        /// <param name="pk">Entity to fix</param>
        /// <param name="enc">Matched encounter</param>
        private static PKM SanityCheckLocation(this PKM pk, IEncounterable enc)
        {
            const int SharedNest = 162; // Shared Nest for online encounter
            const int MaxLair = 244; // Dynamax Adventures
            pk.Met_Location = enc switch
            {
                EncounterStatic8N or EncounterStatic8ND or EncounterStatic8NC => SharedNest,
                EncounterStatic8U => MaxLair,
                _ => pk.Met_Location,
            };
            return pk;
        }

        /// <summary>
        /// Modifies the provided <see cref="pk"/> to the specifications required by <see cref="set"/>.
        /// </summary>
        /// <param name="pk">Converted final pkm to apply details to</param>
        /// <param name="set">Set details required</param>
        /// <param name="handler">Trainer to handle the Pokémon</param>
        /// <param name="enc">Encounter details matched to the Pokémon</param>
        /// <param name="regen">Regeneration information</param>
        private static void ApplySetDetails(PKM pk, IBattleTemplate set, ITrainerInfo handler, IEncounterable enc, RegenSet regen)
        {
            byte Form = set.Form;
            var language = regen.Extra.Language;
            var pidiv = MethodFinder.Analyze(pk);

            pk.SetPINGA(set, pidiv.Type, set.HiddenPowerType, enc);
            pk.SetSpeciesLevel(set, Form, enc, language);
            pk.SetDateLocks(enc);
            pk.SetHeldItem(set);

            // Actions that do not affect set legality
            pk.SetHandlerandMemory(handler, enc);
            pk.SetFriendship(enc);
            pk.SetRecordFlags(set.Moves);

            // Legality Fixing
            pk.SetMovesEVs(set, enc);
            pk.SetCorrectMetLevel();
            pk.SetGVs();
            pk.SetHyperTrainingFlags(set, enc); // Hypertrain
            pk.SetEncryptionConstant(enc);
            pk.SetShinyBoolean(set.Shiny, enc, regen.Extra.ShinyType);
            pk.FixGender(set);

            // Final tweaks
            pk.SetGimmicks(set);
            pk.SetGigantamaxFactor(set, enc);
            pk.SetSuggestedRibbons(set, enc, SetAllLegalRibbons);
            pk.SetBelugaValues();
            pk.SetSuggestedContestStats(enc);
            pk.FixEdgeCases(enc);

            // Aesthetics
            pk.ApplyHeightWeight(enc);
            pk.SetSuggestedBall(SetMatchingBalls, ForceSpecifiedBall, regen.Extra.Ball, enc);
            pk.ApplyMarkings(UseMarkings);
            pk.ApplyBattleVersion(handler);
        }

        /// <summary>
        /// Validate and Set the gender if needed
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        private static void ValidateGender(PKM pk)
        {
            bool genderValid = pk.IsGenderValid();
            if (!genderValid)
            {
                if (pk.Format == 4 && pk.Species == (ushort)Species.Shedinja) // Shedinja glitch
                {
                    // should match original gender
                    var gender = EntityGender.GetFromPIDAndRatio(pk.PID, 0x7F); // 50-50
                    if (gender == pk.Gender)
                        genderValid = true;
                }
                else if (pk.Format > 5 && pk.Species is (ushort)Species.Marill or (ushort)Species.Azumarill)
                {
                    var gv = pk.PID & 0xFF;
                    if (gv > 63 && pk.Gender == 1) // evolved from azurill after transferring to keep gender
                        genderValid = true;
                }
            }
            else
            {
                // check for mixed->fixed gender incompatibility by checking the gender of the original species
                if (SpeciesCategory.IsFixedGenderFromDual(pk.Species) && pk.Gender != 2) // shedinja
                {
                    pk.Gender = EntityGender.GetFromPID(new LegalInfo(pk, new List<CheckResult>()).EncounterMatch.Species, pk.EncryptionConstant);
                    // genderValid = true; already true if we reach here
                }
            }

            if (genderValid)
                return;

            switch (pk.Gender)
            {
                case 0: pk.Gender = 1; break;
                case 1: pk.Gender = 0; break;
                default: pk.GetSaneGender(); break;
            }
        }

        /// <summary>
        /// Comptitive IVs or PKHeX default IVs implementation
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="apply">boolean to apply or not to apply markings</param>
        /// <param name="competitive">boolean to apply competitive IVs instead of the default behaviour</param>
        private static void ApplyMarkings(this PKM pk, bool apply = true)
        {
            if (!apply || pk.Format <= 3) // No markings if pk.Format is less than or equal to 3
                return;
            pk.SetMarkings();
        }

        /// <summary>
        /// Custom Marking applicator method
        /// </summary>
        /// <param name="pk">PK input</param>
        /// <returns></returns>
        public static Func<int, int, int> CompetitiveMarking(PKM pk)
        {
            if (pk.Format < 7)
                return GetSimpleMarking;
            return GetComplexMarking;

            static int GetSimpleMarking(int val, int _) => val == 31 ? 1 : 0;
            static int GetComplexMarking(int val, int _) => val switch
            {
                31 => 1,
                30 => 2,
                _ => 0,
            };
        }

        /// <summary>
        /// Proper method to hypertrain based on Showdown Sets. Also handles edge cases like ultrabeasts
        /// </summary>
        /// <param name="pk">passed pkm object</param>
        /// <param name="set">showdown set to base hypertraining on</param>
        private static void SetHyperTrainingFlags(this PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            if (pk is not IHyperTrain t || pk.Species == (ushort)Species.Stakataka)
                return;

            // Game exceptions (IHyperTrain exists because of the field but game disallows hypertraining)
            if (!t.IsHyperTrainingAvailable(EvolutionChain.GetEvolutionChainsAllGens(pk, enc)))
                return;

            pk.HyperTrain(set.IVs);

            // Handle special cases here for ultrabeasts
            switch (pk.Species)
            {
                case (int)Species.Kartana when pk.Nature == (int)Nature.Timid && set.IVs[1] <= 21: // Speed boosting Timid Kartana ATK IVs <= 19
                    t.HT_ATK = false;
                    break;
                case (int)Species.Stakataka when pk.StatNature == (int)Nature.Lonely && set.IVs[2] <= 17: // Atk boosting Lonely Stakataka DEF IVs <= 15
                    t.HT_DEF = false;
                    break;
                case (int)Species.Pyukumuku when set.IVs[2] == 0 && set.IVs[5] == 0 && pk.Ability == (int)Ability.InnardsOut: // 0 Def / 0 Spd Pyukumuku with innards out
                    t.HT_DEF = false;
                    t.HT_SPD = false;
                    break;
            }
        }

        /// <summary>
        /// Sets past-generation Pokemon as Battle Ready for games that support it
        /// </summary>
        /// <param name="pk">Return PKM</param>
        /// <param name="trainer">Trainer to handle the <see cref="pk"/></param>
        private static void ApplyBattleVersion(this PKM pk, ITrainerInfo trainer)
        {
            if (!SetBattleVersion)
                return;
            if (pk.IsNative && !pk.GO)
                return;
            if (pk is not IBattleVersion bvPk)
                return;

            var oldBattleVersion = bvPk.BattleVersion;
            var relearn = pk.RelearnMoves;

            pk.ClearRelearnMoves();
            bvPk.BattleVersion = (byte)trainer.Game;

            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
            {
                bvPk.BattleVersion = oldBattleVersion;
                pk.SetRelearnMoves(relearn);
            }
        }

        /// <summary>
        /// Set matching colored pokeballs based on the color API in personal table
        /// </summary>
        /// <param name="pk">Return PKM</param>
        public static void SetMatchingBall(this PKM pk) => BallApplicator.ApplyBallLegalByColor(pk);

        /// <summary>
        /// Set forms of specific species to form 0 since they cannot have a form while boxed
        /// </summary>
        /// <param name="pk">pokemon passed to the method</param>
        public static void SetBoxForm(this PKM pk)
        {
            if (pk.Format > 6)
                return;
            switch (pk.Species)
            {
                case (int)Species.Shaymin when pk.Form != 0:
                case (int)Species.Hoopa when pk.Form != 0:
                case (int)Species.Furfrou when pk.Form != 0:
                    pk.Form = 0;
                    if (pk is IFormArgument f) f.FormArgument = 0;
                    break;
            }
        }

        /// <summary>
        /// Handle Egg encounters (for example PGF events that were distributed as eggs)
        /// </summary>
        /// <param name="pk">pkm distributed as an egg</param>
        /// <param name="enc">encounter detail</param>
        /// <param name="tr">save file</param>
        private static void HandleEggEncounters(this PKM pk, IEncounterable enc, ITrainerInfo tr)
        {
            if (!pk.IsEgg)
                return; // should be checked before, but condition added for future usecases
            // Handle egg encounters. Set them as traded and force hatch them before proceeding
            pk.ForceHatchPKM();
            if (enc is MysteryGift { IsEgg: true })
            {
                if (enc is WC3)
                    pk.Met_Level = 0; // hatched
                pk.Language = tr.Language;
                pk.SetTrainerData(tr);
            }
            pk.Egg_Location = Locations.TradedEggLocation(pk.Generation, (GameVersion)pk.Version);
        }


        /// <summary>
        /// Set IV Values for the Pokémon
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="set"></param>
        /// <param name="method"></param>
        /// <param name="hpType"></param>
        /// <param name="enc"></param>
        private static void SetPINGA(this PKM pk, IBattleTemplate set, PIDType method, int hpType, IEncounterable enc)
        {
            var ivprop = enc.GetType().GetProperty("IVs");
            if (enc is not EncounterStatic4Pokewalker && enc.Generation > 2)
                ShowdownEdits.SetNature(pk, set, enc);

            // If PID and IV is handled in PreSetPIDIV, don't set it here again and return out
            var hascurry = set.GetBatchValue(nameof(IRibbonSetMark8.RibbonMarkCurry));
            var changeec = hascurry != null && string.Equals(hascurry, "true", StringComparison.OrdinalIgnoreCase) && AllowBatchCommands;

            if (IsPIDIVSet(pk, enc) && !changeec)
                return;

            if (pk.Context == EntityContext.Gen8 && changeec)
                pk.SetRandomEC(); // break correlation

            if (enc is MysteryGift mg)
            {
                var ivs = pk.IVs;
                for (int i = 0; i < mg.IVs.Length; i++)
                    ivs[i] = mg.IVs[i] > 31 ? set.IVs[i] : mg.IVs[i];
                pk.IVs = ivs;
                if (enc.Generation is not (3 or 4))
                    return;
            }

            if (ivprop != null && ivprop.PropertyType == typeof(IndividualValueSet))
            {
                var ivset = ivprop.GetValue(enc);
                if (ivset != null && ((IndividualValueSet)ivset).IsSpecified)
                    return;
            }

            if (enc.Generation is not (3 or 4))
            {
                pk.IVs = set.IVs;
                if (pk is IAwakened)
                {
                    pk.SetAwakenedValues(set);
                    return;
                }
                return;
            }
            // TODO: Something about the gen 5 events. Maybe check for nature and shiny val and not touch the PID in that case?
            // Also need to figure out hidden power handling in that case.. for PIDType 0 that may isn't even be possible.

            switch (enc)
            {
                case EncounterSlot3XD:
                case PCD:
                case EncounterEgg:
                    return;
                // EncounterTrade4 doesn't have fixed PIDs, so don't early return
                case EncounterTrade3:
                case EncounterTrade4PID:
                case EncounterTrade4RanchGift:
                    ShowdownEdits.SetEncounterTradeIVs(pk);
                    return; // Fixed PID, no need to mutate
                default:

                    FindPIDIV(pk, method, hpType, set.Shiny, enc, set);
                    ValidateGender(pk);
                    break;
            }

            // Handle mismatching abilities due to a PID re-roll
            // Check against ability index because the pokemon could be a pre-evo at this point
            if (pk.Ability != set.Ability)
                pk.RefreshAbility(pk is PK5 { HiddenAbility: true } ? 2 : pk.AbilityNumber >> 1);
            var ability_idx = GetRequiredAbilityIdx(pk, set);
            if (set.Ability == -1 || set.Ability == pk.Ability || pk.AbilityNumber >> 1 == ability_idx || ability_idx == -1)
                return;

            var abilitypref = enc.Ability;
            ShowdownEdits.SetAbility(pk, set, abilitypref);
        }

        /// <summary>
        /// Set Ganbaru Values after IVs are fully set
        /// </summary>
        /// <param name="pk">PKM to set GVs on</param>
        private static void SetGVs(this PKM pk)
        {
            if (pk is not IGanbaru g)
                return;
            g.SetSuggestedGanbaruValues(pk);
        }

        /// <summary>
        /// Set PIDIV for raid PKM via XOROSHIRO incase it is transferred to future generations to preserve the IVs
        /// </summary>
        /// <param name="pk">Pokemon to be edited</param>
        /// <param name="enc">Raid encounter encounterable</param>
        /// <param name="set">Set to pass in requested IVs</param>
        private static void PreSetPIDIV(this PKM pk, IEncounterable enc, IBattleTemplate set, EncounterCriteria criteria)
        {
            if (enc is ITeraRaid9)
            {
                var pk9 = (PK9)pk;
                switch (enc)
                {
                    case EncounterTera9 e: FindTeraPIDIV(pk9, e, set, criteria); break;
                    case EncounterDist9 e: FindTeraPIDIV(pk9, e, set, criteria); break;
                    case EncounterMight9 e: FindTeraPIDIV(pk9, e, set, criteria); break;
                }
                if (set.TeraType != MoveType.Any && set.TeraType != pk9.TeraType)
                    pk9.SetTeraType(set.TeraType);
            }
            else if (enc is EncounterStatic8U && set.Shiny)
            {
                // Dynamax Adventure shinies are always XOR 1 (thanks santacrab!)
                pk.PID = SimpleEdits.GetShinyPID(pk.TID16, pk.SID16, pk.PID, 1);
            }
            else if (enc is IOverworldCorrelation8 eo)
            {
                var flawless = 0;
                if (enc is EncounterStatic8 estatic8)
                {
                    if (estatic8.ScriptedNoMarks || estatic8.Gift)
                        return;
                    flawless = estatic8.FlawlessIVCount;
                }

                var pk8 = (PK8)pk;
                if (eo.GetRequirement(pk8) != OverworldCorrelation8Requirement.MustHave)
                    return;

                Shiny shiny;
                if (set is RegenTemplate r)
                    shiny = r.Regen.Extra.ShinyType;
                else
                    shiny = set.Shiny ? Shiny.Always : Shiny.Never;

                var cloned = new int[set.IVs.Length];

                // Attempt to give them requested 0 ivs at the very least unless they specifically request for all random ivs
                if (set.IVs.Contains(31) || set.IVs.Contains(0))
                {
                    for (int i = 0; i < set.IVs.Length; i++)
                        cloned[i] = set.IVs[i] != 0 ? 31 : 0;
                }
                else
                {
                    cloned = set.IVs;
                }

                if (!SimpleEdits.TryApplyHardcodedSeedWild8(pk8, enc, cloned, shiny))
                    FindWildPIDIV8(pk8, shiny, flawless);
            }
            else if (enc is IStaticCorrelation8b esc)
            {
                var flawless = 0;
                if (enc is EncounterStatic8b estatic8b)
                    flawless = estatic8b.FlawlessIVCount;

                if (esc.GetRequirement(pk) != StaticCorrelation8bRequirement.MustHave)
                    return;

                Shiny shiny;
                if (set is RegenTemplate r)
                    shiny = r.Regen.Extra.ShinyType;
                else
                    shiny = set.Shiny ? Shiny.Always : Shiny.Never;

                Roaming8bRNG.ApplyDetails(pk, EncounterCriteria.Unrestricted, shiny, flawless);
                pk.Met_Location = SimpleEdits.Roaming_MetLocation_BDSP[0];
            }
            else if (enc is EncounterEgg && GameVersion.BDSP.Contains(enc.Version))
            {
                pk.IVs = set.IVs;
                Shiny shiny;
                if (set is RegenTemplate r)
                    shiny = r.Regen.Extra.ShinyType;
                else
                    shiny = set.Shiny ? Shiny.Always : Shiny.Never;
                FindEggPIDIV8b(pk, shiny, set.Gender);
            }
        }

        private static void FindTeraPIDIV<T>(PK9 pk, T enc, IBattleTemplate set, EncounterCriteria criteria) where T : ITeraRaid9, IEncounterTemplate
        {
            if (IsMatchCriteria9(pk, set, criteria))
                return;

            var count = 0;
            var compromise = false;
            do
            {
                ulong seed = Util.Rand.Rand64();
                const byte rollCount = 1;
                const byte undefinedSize = 0;
                var pi = PersonalTable.SV.GetFormEntry(enc.Species, enc.Form);
                var param = enc switch
                {
                    EncounterDist9 e => new GenerateParam9(pk.Species, pi.Gender, e.FlawlessIVCount, rollCount,
                        undefinedSize, undefinedSize, e.ScaleType, e.Scale, e.Ability, e.Shiny, IVs: e.IVs),
                    EncounterMight9 e => new GenerateParam9(pk.Species, pi.Gender, e.FlawlessIVCount, rollCount,
                        undefinedSize, undefinedSize, e.ScaleType, e.Scale, e.Ability, e.Shiny, e.Nature, e.IVs),
                    EncounterTera9 e => new GenerateParam9(pk.Species, pi.Gender, e.FlawlessIVCount, rollCount,
                        undefinedSize, undefinedSize, undefinedSize, undefinedSize, e.Ability, e.Shiny),
                    _ => throw new NotImplementedException("Unknown ITeraRaid9 type detected"),
                };
                var valid = enc.TryApply32(pk, seed, param, criteria);
                if (valid && IsMatchCriteria9(pk, set, criteria, compromise))
                    break;
                if (count == 1_000)
                    compromise = true;
            } while (++count < 15_000);
        }

        /// <summary>
        /// Wild PID IVs being set through XOROSHIRO128
        /// </summary>
        /// <param name="pk">pokemon to edit</param>
        /// <param name="shiny">Shinytype requested</param>
        /// <param name="flawless">number of flawless ivs</param>
        /// <param name="fixedseed">Optional fixed RNG seed</param>
        public static void FindWildPIDIV8(PK8 pk, Shiny shiny, int flawless = 0, uint? fixedseed = null)
        {
            // Modified version of the standard XOROSHIRO algorithm (32 bit seed 0, same const seed 1)
            // EC -> PID -> Flawless IV rolls -> Non Flawless IVs -> height -> weight
            uint seed;
            Xoroshiro128Plus rng;
            var ivs = new[] { -1, -1, -1, -1, -1, -1 };

            if (fixedseed != null)
            {
                seed = (uint)fixedseed;
                rng = new Xoroshiro128Plus(seed);

                pk.EncryptionConstant = (uint)rng.NextInt();
                pk.PID = (uint)rng.NextInt();
            }
            else
            {
                while (true)
                {
                    seed = Util.Rand32();
                    rng = new Xoroshiro128Plus(seed);

                    pk.EncryptionConstant = (uint)rng.NextInt();
                    pk.PID = (uint)rng.NextInt();

                    var xor = pk.ShinyXor;
                    switch (shiny)
                    {
                        case Shiny.AlwaysStar when xor is 0 or > 15:
                        case Shiny.Never when xor < 16:
                            continue;
                    }

                    // Every other case can be valid and genned, so break out
                    break;
                }
            }

            // Square shiny: if not xor0, force xor0
            // Always shiny: if not xor0-15, force xor0
            var editnecessary = (shiny == Shiny.AlwaysSquare && pk.ShinyXor != 0) || (shiny == Shiny.Always && pk.ShinyXor > 15);
            if (editnecessary)
                pk.PID = SimpleEdits.GetShinyPID(pk.TID16, pk.SID16, pk.PID, 0);

            // RNG is fixed now and you have the requested shiny!
            const int UNSET = -1;
            const int MAX = 31;
            for (int i = ivs.Count(z => z == MAX); i < flawless; i++)
            {
                int index = (int)rng.NextInt(6);
                while (ivs[index] != UNSET)
                    index = (int)rng.NextInt(6);
                ivs[index] = MAX;
            }

            for (int i = 0; i < 6; i++)
            {
                if (ivs[i] == UNSET)
                    ivs[i] = (int)rng.NextInt(32);
            }

            pk.IV_HP = ivs[0];
            pk.IV_ATK = ivs[1];
            pk.IV_DEF = ivs[2];
            pk.IV_SPA = ivs[3];
            pk.IV_SPD = ivs[4];
            pk.IV_SPE = ivs[5];

            var height = (int)rng.NextInt(0x81) + (int)rng.NextInt(0x80);
            var weight = (int)rng.NextInt(0x81) + (int)rng.NextInt(0x80);
            pk.HeightScalar = (byte)height;
            pk.WeightScalar = (byte)weight;
        }

        /// <summary>
        /// Egg PID IVs being set through XOROSHIRO1288b
        /// </summary>
        /// <param name="pk">pokemon to edit</param>
        /// <param name="shiny">Shinytype requested</param>
        /// <param name="gender"></param>
        public static void FindEggPIDIV8b(PKM pk, Shiny shiny, int gender)
        {
            var ivs = new[] { -1, -1, -1, -1, -1, -1 };
            var IVs = pk.IVs;
            var required_ivs = new[] { IVs[0], IVs[1], IVs[2], IVs[4], IVs[5], IVs[3] };
            var pi = PersonalTable.BDSP.GetFormEntry(pk.Species, pk.Form);
            var ratio = pi.Gender;

            while (true)
            {
                var seed = Util.Rand32();
                var rng = new Xoroshiro128Plus8b(seed);

                var nido_family_f = new[] { (int)Species.NidoranF, (int)Species.Nidorina, (int)Species.Nidoqueen };
                var nido_family_m = new[] { (int)Species.NidoranM, (int)Species.Nidorino, (int)Species.Nidoking };
                if (nido_family_m.Contains(pk.Species) || nido_family_f.Contains(pk.Species))
                {
                    var nido_roll = rng.NextUInt(2);
                    if (nido_roll == 1 && nido_family_m.Contains(pk.Species)) // Nidoran F
                        continue;
                    if (nido_roll == 0 && nido_family_f.Contains(pk.Species)) // Nidoran M
                        continue;
                }

                if (pk.Species is (int)Species.Illumise or (int)Species.Volbeat)
                {
                    if (rng.NextUInt(2) != (int)Species.Illumise - pk.Species)
                        continue;
                }

                if (pk.Species == (int)Species.Indeedee)
                {
                    if (rng.NextUInt(2) != pk.Form)
                        continue;
                }

                if (ratio != PersonalInfo.RatioMagicMale && ratio != PersonalInfo.RatioMagicFemale && ratio != PersonalInfo.RatioMagicGenderless)
                {
                    var gender_roll = rng.NextUInt(252) + 1;
                    var fin_gender = gender_roll < ratio ? 1 : 0;
                    if (gender != -1 && gender != fin_gender)
                        continue;
                }

                // nature
                _ = rng.NextUInt(25); // assume one parent always carry an everstone

                // ability
                _ = rng.NextUInt(100); // assume the ability is changed using capsule/patch (assume parent is ability 0/1)

                // assume other parent always has destiny knot
                const int inheritCount = 5;
                var inherited = 0;
                while (inherited < inheritCount)
                {
                    var stat = rng.NextUInt(6);
                    if (ivs[stat] != -1)
                        continue;

                    rng.NextUInt(2); // decides which parents iv to inherit, assume that parent has the required IV
                    ivs[stat] = required_ivs[stat];
                    inherited++;
                }
                Span<uint> ivs2 = stackalloc[]
                {
                    rng.NextUInt(32),
                    rng.NextUInt(32),
                    rng.NextUInt(32),
                    rng.NextUInt(32),
                    rng.NextUInt(32),
                    rng.NextUInt(32),
                };
                for (int i = 0; i < 6; i++)
                {
                    if (ivs[i] == -1)
                        ivs[i] = (int)ivs2[i];
                }
                // if (!ivs.SequenceEqual(required_ivs))
                //    continue;
                pk.IV_HP = ivs[0];
                pk.IV_ATK = ivs[1];
                pk.IV_DEF = ivs[2];
                pk.IV_SPA = ivs[3];
                pk.IV_SPD = ivs[4];
                pk.IV_SPE = ivs[5];

                pk.EncryptionConstant = rng.NextUInt();

                // PID dissociated completely (assume no masuda and no shiny charm)
                if (shiny is Shiny.Never or Shiny.Random)
                    pk.SetUnshiny();
                else pk.PID = SimpleEdits.GetShinyPID(pk.TID16, pk.SID16, pk.PID, shiny == Shiny.AlwaysSquare ? 0 : 1);
                break;
            }
        }

        private static bool IsMatchCriteria9(PK9 pk, IBattleTemplate template, EncounterCriteria criteria, bool compromise = false)
        {
            // compromise on nature since they can be minted
            if (criteria.Nature != Nature.Random && criteria.Nature != (Nature)pk.Nature && !compromise) // match nature
                return false;
            if ((uint)template.Gender < 2 && template.Gender != pk.Gender) // match gender
                return false;
            if (template.Form != pk.Form && !FormInfo.IsFormChangeable(pk.Species, pk.Form, template.Form, EntityContext.Gen9, pk.Context)) // match form -- Toxtricity etc
                return false;
            if (template.Shiny != pk.IsShiny)
                return false;
            return true;
        }


        /// <summary>
        /// Method to set PID, IV while validating nature.
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="Method">Given Method</param>
        /// <param name="HPType">HPType INT for preserving Hidden powers</param>
        /// <param name="shiny">Only used for CHANNEL RNG type</param>
        /// <param name="enc"></param>
        private static void FindPIDIV(PKM pk, PIDType Method, int HPType, bool shiny, IEncounterable enc, IBattleTemplate set)
        {
            if (Method == PIDType.None)
            {
                if (enc is WC3 wc3)
                    Method = wc3.Method;
                else
                    Method = FindLikelyPIDType(pk);

                if (pk.Version == (int)GameVersion.CXD && Method != PIDType.PokeSpot)
                    Method = PIDType.CXD;
                if (Method == PIDType.None && pk.Generation >= 3)
                    pk.SetPIDGender(pk.Gender);
            }
            switch (Method)
            {
                case PIDType.Method_1_Roamer when pk.HPType != (int)MoveType.Fighting - 1: // M1 Roamers can only be HP fighting
                case PIDType.Pokewalker when (pk.Nature >= 24 || pk.AbilityNumber == 4): // No possible pokewalker matches
                    return;
            }

            var iterPKM = pk.Clone();
            // Requested pokemon may be an evolution, guess index based on requested species ability
            var ability_idx = GetRequiredAbilityIdx(iterPKM, set);

            if (iterPKM.AbilityNumber >> 1 != ability_idx && set.Ability != -1 && ability_idx != -1)
                iterPKM.SetAbilityIndex(ability_idx);
            var count = 0;
            var isWishmaker = Method == PIDType.BACD_R && shiny && enc is WC3 { OT_Name: "WISHMKR" };
            var compromise = false;
            var gr = pk.PersonalInfo.Gender;
            do
            {
                if (count >= 2_500_000)
                    compromise = true;
                uint seed = Util.Rand32();
                if (isWishmaker)
                {
                    seed = WC3Seeds.GetShinyWishmakerSeed((Nature)iterPKM.Nature);
                    isWishmaker = false;
                }
                if (PokeWalkerSeedFail(seed, Method, pk, iterPKM))
                    continue;
                PIDGenerator.SetValuesFromSeed(pk, Method, seed);
                if (pk.AbilityNumber != iterPKM.AbilityNumber && !compromise && pk.Nature != iterPKM.Nature)
                    continue;
                if (pk.PIDAbility != iterPKM.PIDAbility && !compromise)
                    continue;
                if (HPType >= 0 && pk.HPType != HPType)
                    continue;
                if (pk.PID % 25 != iterPKM.Nature) // Util.Rand32 is the way to go
                    continue;
                if (pk.Gender != EntityGender.GetFromPIDAndRatio(pk.PID, gr))
                    continue;
                if (pk.Version == (int)GameVersion.CXD && Method == PIDType.CXD) // verify locks
                {
                    pk.EncryptionConstant = pk.PID;
                    var ec = pk.PID;
                    bool xorPID = ((pk.TID16 ^ pk.SID16 ^ (int)(ec & 0xFFFF) ^ (int)(ec >> 16)) & ~0x7) == 8;
                    if (enc is EncounterStatic3XD && enc.Species == (int)Species.Eevee && (shiny != pk.IsShiny || xorPID)) // Starter Correlation
                        continue;
                    var la = new LegalityAnalysis(pk);
                    if ((la.Info.PIDIV.Type != PIDType.CXD && la.Info.PIDIV.Type != PIDType.CXD_ColoStarter) || !la.Info.PIDIVMatches || !pk.IsValidGenderPID(enc))
                        continue;
                }
                if (pk.Species == (int)Species.Unown)
                {
                    if (pk.Form != iterPKM.Form)
                        continue;
                    if (enc.Generation == 3 && pk.Form != EntityPID.GetUnownForm3(pk.PID))
                        continue;
                }
                var pidxor = ((pk.TID16 ^ pk.SID16 ^ (int)(pk.PID & 0xFFFF) ^ (int)(pk.PID >> 16)) & ~0x7) == 8;
                if (Method == PIDType.Channel && (shiny != pk.IsShiny || pidxor))
                    continue;
                break;
            } while (++count < 5_000_000);
        }

        private static int GetRequiredAbilityIdx(PKM pkm, IBattleTemplate set)
        {
            if (set.Ability == -1)
                return -1;
            var temp = pkm.Clone();
            temp.Species = set.Species;
            temp.SetAbilityIndex(pkm.AbilityNumber >> 1);
            if (temp.Ability == set.Ability)
                return -1;
            var idx = temp.PersonalInfo.GetIndexOfAbility(set.Ability);
            return idx;
        }

        /// <summary>
        /// Checks if a Pokewalker seed failed, and if it did, randomizes TID and SID (to retry in the future)
        /// </summary>
        /// <param name="seed">Seed</param>
        /// <param name="method">RNG method (every method except pokewalker is ignored)</param>
        /// <param name="pk">PKM object</param>
        /// <param name="original">original encounter pkm</param>
        private static bool PokeWalkerSeedFail(uint seed, PIDType method, PKM pk, PKM original)
        {
            if (method != PIDType.Pokewalker)
                return false;
            if (seed % 24 != original.Nature)
                return true;
            pk.TID16 = (ushort)Util.Rand.Next(65535);
            pk.SID16 = (ushort)Util.Rand.Next(65535);
            return false;
        }

        /// <summary>
        /// Secondary fallback if PIDType.None to slot the PKM into its most likely type
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <returns>PIDType that is likely used</returns>
        private static PIDType FindLikelyPIDType(PKM pk)
        {
            if (pk.Species == (int)Species.Manaphy && pk.Gen4)
            {
                pk.Egg_Location = Locations.LinkTrade4; // todo: really shouldn't be doing this, don't modify pkm
                return PIDType.Method_1;
            }
            var info = new LegalInfo(pk, new List<CheckResult>());
            EncounterFinder.FindVerifiedEncounter(pk, info);
            return pk.Generation switch
            {
                3 => info.EncounterMatch switch
                {
                    WC3 g => g.Method,

                    EncounterStatic3 when pk.Version == (int)GameVersion.CXD => PIDType.CXD,
                    EncounterStatic3Colo when pk.Version == (int)GameVersion.CXD => PIDType.CXD,
                    EncounterStatic3XD when pk.Version == (int)GameVersion.CXD => PIDType.CXD,
                    EncounterStatic3 => pk.Version switch
                    {
                        (int)GameVersion.E => PIDType.Method_1,
                        (int)GameVersion.FR or (int)GameVersion.LG => PIDType.Method_1, // roamer glitch
                        _ => PIDType.Method_1,
                    },

                    EncounterSlot3 when pk.Version == (int)GameVersion.CXD => PIDType.PokeSpot,
                    EncounterSlot3XD when pk.Version == (int)GameVersion.CXD => PIDType.PokeSpot,
                    EncounterSlot3 => pk.Species == (int)Species.Unown ? PIDType.Method_1_Unown : PIDType.Method_1,
                    _ => PIDType.None,
                },

                4 => info.EncounterMatch switch
                {
                    EncounterStatic4Pokewalker => PIDType.Pokewalker,
                    EncounterStatic4 s => (s.Shiny == Shiny.Always ? PIDType.ChainShiny : PIDType.Method_1),
                    PGT => PIDType.Method_1,
                    _ => PIDType.None,
                },

                _ => PIDType.None,
            };
        }


        /// <summary>
        /// Method to get the correct met level for a pokemon. Move up the met level till all moves are legal
        /// </summary>
        /// <param name="pk">pokemon</param>
        public static void SetCorrectMetLevel(this PKM pk)
        {
            var lvl = pk.CurrentLevel;
            if (pk.Met_Level > lvl)
                pk.Met_Level = lvl;
            if (pk.Met_Location is not (Locations.Transfer1 or Locations.Transfer2 or Locations.Transfer3 or Locations.Transfer4 or Locations.GO8))
                return;
            var level = pk.Met_Level;
            if (lvl <= level)
                return;
            while (lvl >= pk.Met_Level)
            {
                var la = new LegalityAnalysis(pk);
                if (la.Info.Moves.All(z => z.Valid))
                    return;
                pk.Met_Level++;
            }
            pk.Met_Level = level; // Set back to normal if nothing legalized
        }

        /// <summary>
        /// Edge case memes for weird properties that I have no interest in setting for other pokemon.
        /// </summary>
        /// <param name="pk">Pokemon to edit</param>
        /// <param name="enc">Encounter the <see cref="pk"/> originated rom</param>
        private static void FixEdgeCases(this PKM pk, IEncounterable enc)
        {
            if (pk.Nickname.Length == 0)
                pk.ClearNickname();

            // Shiny Manaphy Egg
            if (enc is MysteryGift { Species: (int)Species.Manaphy, Generation: 4 } && pk.IsShiny)
            {
                pk.Egg_Location = Locations.LinkTrade4;
                if (pk.Format != 4)
                    return;
                pk.Met_Location = pk.HGSS ? Locations.HatchLocationHGSS : Locations.HatchLocationDPPt;
            }

            // CXD only has a male trainer
            if (pk.Version == (int)GameVersion.CXD && pk.OT_Gender == (int)Gender.Female) // Colosseum and XD are sexist games.
                pk.OT_Gender = (int)Gender.Male;

            // VC Games are locked to console region (modify based on language)
            if (pk is PK7 { Generation: <= 2 } pk7)
                pk7.FixVCRegion();

            // Vivillon pattern fixes if necessary
            if (pk is IGeoTrack && pk.Species is (int)Species.Vivillon or (int)Species.Spewpa or (int)Species.Scatterbug)
                pk.FixVivillonRegion();
        }

        /// <summary>
        /// Fix region locked VCs for PK7s
        /// </summary>
        /// <param name="pk7">PK7 to fix</param>
        public static void FixVCRegion(this PK7 pk7)
        {
            var valid = Locale3DS.IsRegionLockedLanguageValidVC(pk7.ConsoleRegion, pk7.Language);
            if (!valid)
            {
                switch (pk7.Language)
                {
                    case (int)LanguageID.English:
                    case (int)LanguageID.Spanish:
                    case (int)LanguageID.French:
                        pk7.ConsoleRegion = 1;
                        pk7.Region = 0;
                        pk7.Country = 49;
                        break;
                    case (int)LanguageID.German:
                    case (int)LanguageID.Italian:
                        pk7.ConsoleRegion = 2;
                        pk7.Region = 0;
                        pk7.Country = 105;
                        break;
                    case (int)LanguageID.Japanese:
                        pk7.ConsoleRegion = 0;
                        pk7.Region = 0;
                        pk7.Country = 1;
                        break;
                    case (int)LanguageID.Korean:
                        pk7.ConsoleRegion = 5;
                        pk7.Region = 0;
                        pk7.Country = 136;
                        break;
                }
            }
        }

        ///<summary>
        /// Handle search criteria for very specific encounters
        /// </summary>
        /// 
        public static EncounterCriteria SetSpecialCriteria(EncounterCriteria criteria, IEncounterable enc, IBattleTemplate set)
        {
            switch (enc.Species)
            {
                case (int)Species.Kartana when criteria.Nature == Nature.Timid && criteria.IV_ATK <= 21: // Speed boosting Timid Kartana ATK IVs <= 19
                    return criteria with { IV_HP = -1, IV_ATK = criteria.IV_ATK, IV_DEF = -1, IV_SPA = -1, IV_SPD = -1, IV_SPE = -1 };

                case (int)Species.Stakataka when criteria.Nature == Nature.Lonely && criteria.IV_DEF <= 17: // Atk boosting Lonely Stakataka DEF IVs <= 15
                    return criteria with { IV_HP = -1, IV_ATK = -1, IV_DEF = criteria.IV_DEF, IV_SPA = -1, IV_SPD = -1, IV_SPE = criteria.IV_SPE };

                case (int)Species.Pyukumuku when criteria.IV_DEF == 0 && criteria.IV_SPD == 0 && set.Ability == (int)Ability.InnardsOut: // 0 Def / 0 Spd Pyukumuku with innards out
                    return criteria with { IV_HP = -1, IV_ATK = -1, IV_DEF = criteria.IV_DEF, IV_SPA = -1, IV_SPD = criteria.IV_SPD, IV_SPE = -1 };
                default: break;
            }
            return criteria with { IV_ATK = criteria.IV_ATK == 0 ? 0 : -1, IV_DEF = -1, IV_HP = -1, IV_SPA = -1, IV_SPD = -1, IV_SPE = criteria.IV_SPE == 0 ? 0 : -1 };
        }

        /// <summary>
        /// Handle edge case vivillon legality if the trainerdata region is invalid
        /// </summary>
        /// <param name="pk">pkm to fix</param>
        public static void FixVivillonRegion(this PKM pk)
        {
            if (pk is not IGeoTrack g)
                return;
            var valid = Vivillon3DS.IsPatternValid(pk.Form, g.ConsoleRegion);
            if (valid)
                return;
            // 5: JP
            // 7, 14: USA
            // else: EUR
            switch (pk.Form)
            {
                case 5:
                    g.ConsoleRegion = 0;
                    g.Region = 0;
                    g.Country = 1;
                    break;
                case 7:
                case 14:
                    g.ConsoleRegion = 1;
                    g.Region = 0;
                    g.Country = 49;
                    break;
                default:
                    g.ConsoleRegion = 2;
                    g.Region = 0;
                    g.Country = 105;
                    break;
            }
        }

        /// <summary>
        /// Wrapper function for GetLegalFromTemplate but with a Timeout
        /// </summary>
        public static PKM GetLegalFromTemplateTimeout(this ITrainerInfo dest, PKM template, IBattleTemplate set, out LegalizationResult satisfied, bool nativeOnly = false)
        {
            AsyncLegalizationResult GetLegal()
            {
                try
                {
                    if (!EnableDevMode && ALMVersion.GetIsMismatch())
                        return new(template, LegalizationResult.VersionMismatch);

                    var res = dest.GetLegalFromTemplate(template, set, out var s, nativeOnly);
                    return new AsyncLegalizationResult(res, s);
                }
                catch (MissingMethodException)
                {
                    return new AsyncLegalizationResult(template, LegalizationResult.VersionMismatch);
                }
            }

            var task = Task.Run(GetLegal);
            var first = task.TimeoutAfter(new TimeSpan(0, 0, 0, Timeout))?.Result;
            if (first == null)
            {
                satisfied = LegalizationResult.Timeout;
                return template;
            }

            var result = first;
            satisfied = result.Status;
            return result.Created;
        }

        /// <summary>
        /// Async Related actions for global timer.
        /// </summary>
        private class AsyncLegalizationResult
        {
            public readonly PKM Created;
            public readonly LegalizationResult Status;

            public AsyncLegalizationResult(PKM pk, LegalizationResult res)
            {
                Created = pk;
                Status = res;
            }
        }

        private static async Task<AsyncLegalizationResult?>? TimeoutAfter(this Task<AsyncLegalizationResult> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var delay = Task.Delay(timeout, cts.Token);
            var completedTask = await Task.WhenAny(task, delay).ConfigureAwait(false);
            if (completedTask != task)
                return null;

            return await task.ConfigureAwait(false); // will re-fire exception if present
        }

        private static GameVersion[] GetPairedVersions(GameVersion version)
        {
            var group = version switch
            {
                GameVersion.RD or GameVersion.C => version,
                _ => GameUtil.GetMetLocationVersionGroup(version),
            };

            return group switch
            {
                GameVersion.RBY => new[] { GameVersion.RD, GameVersion.GN, GameVersion.BU, GameVersion.YW },
                GameVersion.RSE => new[] { GameVersion.R, GameVersion.S, GameVersion.E },
                GameVersion.FRLG => new[] { GameVersion.SL, GameVersion.VL },
                GameVersion.DPPt => new[] { GameVersion.D, GameVersion.P, GameVersion.Pt },
                GameVersion.HGSS => new[] { GameVersion.HG, GameVersion.SS },
                GameVersion.BW => new[] { GameVersion.B, GameVersion.W },
                GameVersion.B2W2 => new[] { GameVersion.B2, GameVersion.W2 },
                GameVersion.XY => new[] { GameVersion.X, GameVersion.Y },
                GameVersion.ORAS => new[] { GameVersion.OR, GameVersion.AS },
                GameVersion.SM => new[] { GameVersion.SN, GameVersion.MN },
                GameVersion.USUM => new[] { GameVersion.US, GameVersion.UM },
                GameVersion.GG => new[] { GameVersion.GP, GameVersion.GE },
                GameVersion.SWSH => new[] { GameVersion.SW, GameVersion.SH },
                GameVersion.BDSP => new[] { GameVersion.BD, GameVersion.SP },
                GameVersion.SV => new[] { GameVersion.SL, GameVersion.VL },
                _ => new[] { version },
            };
        }
    }
}
