using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static bool UseXOROSHIRO { get; set; } = true;
        public static bool PrioritizeGame { get; set; } = true;
        public static bool SetRandomTracker { get; set; }
        public static GameVersion PrioritizeGameVersion { get; set; }
        public static bool SetBattleVersion { get; set; }
        public static bool AllowTrainerOverride { get; set; }
        public static bool AllowBatchCommands { get; set; } = true;
        public static int Timeout { get; set; } = 15;

        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <remarks>Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="IBattleTemplate"/>.</remarks>
        /// <param name="dest">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is legal or not</param>
        public static PKM GetLegalFromTemplate(this ITrainerInfo dest, PKM template, IBattleTemplate set, out LegalizationResult satisfied)
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

            template.ApplySetDetails(set);
            template.SetRecordFlags(); // Validate TR moves for the encounter
            var isHidden = template.AbilityNumber == 4;
            if (template.Generation >= 5)
                isHidden = isHidden || template.PersonalInfo.Abilities[2] == template.Ability;
            var destType = template.GetType();
            var destVer = (GameVersion)dest.Game;
            if (destVer <= 0 && dest is SaveFile s)
                destVer = s.Version;

            var gamelist = FilteredGameList(template, destVer);

            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves, gamelist);
            var timer = Stopwatch.StartNew();
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
                if (!IsEncounterValid(set, enc, isHidden, destVer, out var ver))
                    continue;

                // Create the PKM from the template.
                var tr = GetTrainer(regen, ver, enc.Generation);
                var raw = enc.ConvertToPKM(tr);
                raw = raw.SanityCheckLocation(enc);
                if (raw.IsEgg) // PGF events are sometimes eggs. Force hatch them before proceeding
                    raw.HandleEggEncounters(enc, tr);

                // Bring to the target generation, then apply final details.
                var pk = PKMConverter.ConvertToType(raw, destType, out _);
                if (pk == null)
                    continue;
                ApplySetDetails(pk, set, raw, dest, enc, regen);

                // Apply final tweaks to the data.
                if (pk is IGigantamax gmax && gmax.CanGigantamax != set.CanGigantamax)
                {
                    if (!gmax.CanToggleGigantamax(pk.Species, enc.Species))
                        continue;
                    gmax.CanGigantamax = set.CanGigantamax; // soup hax
                }

                // Try applying batch editor values.
                if (AllowBatchCommands && regen.HasBatchSettings)
                {
                    pk.RefreshChecksum();
                    var b = regen.Batch;
                    if (!BatchEditing.TryModify(pk, b.Filters, b.Instructions))
                        continue;
                }

                if (pk is PK1 pk1 && ParseSettings.AllowGen1Tradeback)
                    pk1.Catch_Rate = pk1.Gen2Item; // Simulate a gen 2 trade/tradeback to allow tradeback moves

                // Verify the Legality of what we generated, and exit if it is valid.
                var la = new LegalityAnalysis(pk);
                if (la.Valid)
                {
                    satisfied = LegalizationResult.Regenerated;
                    return pk;
                }
                Debug.WriteLine($"{la.Report()}\n");
            }
            satisfied = LegalizationResult.Failed;
            return template;
        }

        private static GameVersion[] FilteredGameList(PKM template, GameVersion destVer)
        {
            var gamelist = GameUtil.GetVersionsWithinRange(template, template.Format).OrderByDescending(c => c.GetGeneration()).ToArray();
            if (PrioritizeGame)
                gamelist = PrioritizeGameVersion == GameVersion.Any ? PrioritizeVersion(gamelist, destVer) : PrioritizeVersion(gamelist, PrioritizeGameVersion);
            if (template.AbilityNumber == 4 && destVer.GetGeneration() < 8)
                gamelist = gamelist.Where(z => z.GetGeneration() is not 3 and not 4).ToArray();
            return gamelist;
        }

        private static ITrainerInfo GetTrainer(RegenSet regen, GameVersion ver, int gen)
        {
            if (AllowTrainerOverride && regen.HasTrainerSettings && regen.Trainer != null)
                return regen.Trainer.MutateLanguage(regen.Extra.Language);
            if (UseTrainerData)
                return TrainerSettings.GetSavedTrainerData(ver, gen).MutateLanguage(regen.Extra.Language);
            return TrainerSettings.DefaultFallback(gen, regen.Extra.Language);
        }

        /// <summary>
        /// Gives the currently loaded save priority over other saves in the same generation. Otherwise generational order is preserved
        /// </summary>
        /// <param name="gamelist">Array of gameversions which needs to be prioritized</param>
        /// <param name="game">Gameversion to prioritize</param>
        /// <returns></returns>
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
        /// <param name="isHidden">is HA requested</param>
        /// <param name="destVer">version to generate in</param>
        /// <param name="ver">version of enc/destVer</param>
        /// <returns>if the encounter is valid or not</returns>
        private static bool IsEncounterValid(IBattleTemplate set, IEncounterable enc, bool isHidden, GameVersion destVer, out GameVersion ver)
        {
            // initialize out vars (not calculating here to save time)
            ver = GameVersion.Any;

            // Don't process if encounter min level is higher than requested level
            if (enc.LevelMin > set.Level)
            {
                var isRaid = enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U;
                if (!isRaid)
                    return false;
            }

            // Don't process if encounter is HA but requested pkm is not HA
            if (!isHidden && enc is EncounterStatic { Ability: 4 })
                return false;

            // Don't process if Game is LGPE and requested PKM is not Kanto / Meltan / Melmetal
            // Don't process if Game is SWSH and requested PKM is not from the Galar Dex (Zukan8.DexLookup)
            if (GameVersion.GG.Contains(destVer))
                return set.Species is <= 151 or 808 or 809;
            if (GameVersion.SWSH.Contains(destVer))
                return ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormEntry(set.Species, set.Form)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(set.Species);
            if (set.Species > destVer.GetMaxSpeciesID())
                return false;

            // Encounter should hopefully be possible
            return true;
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
            if (enc is EncounterStatic8N { Location: 0 })
                pk.Met_Location = SharedNest;
            if (enc is EncounterStatic8ND { Location: 0 })
                pk.Met_Location = SharedNest;
            if (enc is EncounterStatic8NC { Location: 0 })
                pk.Met_Location = SharedNest;
            if (enc is EncounterStatic8U { Location: 0 })
                pk.Met_Location = MaxLair;
            return pk;
        }

        /// <summary>
        /// Modifies the provided <see cref="pk"/> to the specifications required by <see cref="set"/>.
        /// </summary>
        /// <param name="pk">Converted final pkm to apply details to</param>
        /// <param name="set">Set details required</param>
        /// <param name="unconverted">Original pkm data</param>
        /// <param name="handler">Trainer to handle the Pokémon</param>
        /// <param name="enc">Encounter details matched to the Pokémon</param>
        /// <param name="regen">Regeneration information</param>
        private static void ApplySetDetails(PKM pk, IBattleTemplate set, PKM unconverted, ITrainerInfo handler, IEncounterable enc, RegenSet regen)
        {
            int Form = set.Form;
            var pidiv = MethodFinder.Analyze(pk);
            var abilitypref = GetAbilityPreference(pk, enc);
            var language = regen.Extra.Language;

            pk.SetVersion(unconverted); // Preemptive Version setting
            pk.SetLanguage(language);
            pk.SetSpeciesLevel(set, Form, enc, language);
            pk.SetRecordFlags(set.Moves);
            pk.SetMovesEVs(set);
            pk.SetHandlerandMemory(handler);
            pk.SetNatureAbility(set, abilitypref);
            pk.GetSuggestedTracker();
            pk.SetIVsPID(set, pidiv.Type, set.HiddenPowerType, unconverted);
            pk.SetHeldItem(set);
            pk.SetHyperTrainingFlags(set); // Hypertrain
            pk.SetEncryptionConstant(enc);
            pk.FixFatefulFlag(enc);
            pk.SetShinyBoolean(set.Shiny, enc, regen.Extra.ShinyType);
            pk.FixGender(set);
            pk.SetSuggestedRibbons(SetAllLegalRibbons);
            pk.SetSuggestedMemories();
            pk.SetHTLanguage();
            pk.SetDynamaxLevel();
            pk.SetFriendship(enc);
            pk.SetBelugaValues();
            pk.FixEdgeCases();
            pk.SetSuggestedBall(SetMatchingBalls, ForceSpecifiedBall, regen.Extra.Ball);
            pk.ApplyMarkings(UseMarkings, UseCompetitiveMarkings);
            pk.ApplyHeightWeight(enc);
            pk.ApplyBattleVersion(handler);

            // Extra legality unchecked by PKHeX
            pk.SetDatelocks(enc);
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
                if (pk.Format == 4 && pk.Species == (int)Species.Shedinja) // Shedinja glitch
                {
                    // should match original gender
                    var gender = PKX.GetGenderFromPIDAndRatio(pk.PID, 0x7F); // 50-50
                    if (gender == pk.Gender)
                        genderValid = true;
                }
                else if (pk.Format > 5 && pk.Species is (int)Species.Marill or (int)Species.Azumarill)
                {
                    var gv = pk.PID & 0xFF;
                    if (gv > 63 && pk.Gender == 1) // evolved from azurill after transferring to keep gender
                        genderValid = true;
                }
            }
            else
            {
                // check for mixed->fixed gender incompatibility by checking the gender of the original species
                if (Legal.FixedGenderFromBiGender.Contains(pk.Species) && pk.Gender != 2) // shedinja
                {
                    pk.Gender = PKX.GetGenderFromPID(new LegalInfo(pk).EncounterMatch.Species, pk.EncryptionConstant);
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
        private static void ApplyMarkings(this PKM pk, bool apply = true, bool competitive = false)
        {
            if (!apply || pk.Format <= 3) // No markings if pk.Format is less than or equal to 3
                return;
            if (!competitive || pk.Format < 7) // Simple markings dont apply with competitive at all
            {
                // Blue for 31/1 IVs and Red for 30/0 IVs (PKHeX default behaviour)
                pk.SetMarkings();
            }
            else
            {
                // Red for 30 denoting imperfect but close to perfect. Blue for 31. No marking for 0 IVs
                var markings = new int[6];
                for (int i = 0; i < pk.IVs.Length; i++)
                {
                    if (pk.IVs[i] is 31 or 30)
                        markings[i] = pk.IVs[i] == 31 ? 1 : 2;
                }

                pk.Markings = PKX.ReorderSpeedLast(markings);
            }
        }

        /// <summary>
        /// Proper method to hypertrain based on Showdown Sets. Also handles edge cases like ultrabeasts
        /// </summary>
        /// <param name="pk">passed pkm object</param>
        /// <param name="set">showdown set to base hypertraining on</param>
        private static void SetHyperTrainingFlags(this PKM pk, IBattleTemplate set)
        {
            if (pk is not IHyperTrain t)
                return;
            pk.SetSuggestedHyperTrainingData(); // Set IV data based on showdownset

            // Fix HT flags as necessary
            t.HT_ATK = (set.IVs[1] >= 3 || !t.HT_ATK) && ((set.IVs[1] >= 3 && pk.IVs[1] < 3 && pk.CurrentLevel == 100) || t.HT_ATK);
            t.HT_SPE = (set.IVs[3] >= 3 || !t.HT_SPE) && ((set.IVs[3] >= 3 && pk.IVs[3] < 3 && pk.CurrentLevel == 100) || t.HT_SPE);

            // Handle special cases here for ultrabeasts
            switch (pk.Species)
            {
                case (int)Species.Kartana when pk.Nature == (int)Nature.Timid && (set.IVs[1] <= 21 && pk.CurrentLevel == 100): // Speed boosting Timid Kartana ATK IVs <= 19
                    t.HT_ATK = false;
                    break;
                case (int)Species.Stakataka when pk.Nature == (int)Nature.Lonely && (set.IVs[2] <= 17 && pk.CurrentLevel == 100): // Atk boosting Lonely Stakataka DEF IVs <= 15
                    t.HT_DEF = false;
                    break;
                case (int)Species.Pyukumuku when set.IVs[2] == 0 && set.IVs[5] == 0 && pk.Ability == (int)Ability.InnardsOut: // 0 Def / 0 Spd Pyukumuku with innards out
                    t.HT_DEF = false;
                    t.HT_SPD = false;
                    break;
            }
        }

        /// <summary>
        /// Set Version override for GSC and RBY games
        /// </summary>
        /// <param name="pk">Return PKM</param>
        /// <param name="original">Generated PKM</param>
        private static void SetVersion(this PKM pk, PKM original)
        {
            switch (original.Version)
            {
                case (int)GameVersion.SWSH:
                    pk.Version = (int)GameVersion.SW;
                    break;
                case (int)GameVersion.GG:
                    pk.Version = (int)GameVersion.GP;
                    break;
                case (int)GameVersion.USUM:
                    pk.Version = (int)GameVersion.US;
                    break;
                case (int)GameVersion.SM:
                    pk.Version = (int)GameVersion.SN;
                    break;
                case (int)GameVersion.ORAS:
                    pk.Version = (int)GameVersion.OR;
                    break;
                case (int)GameVersion.XY:
                    pk.Version = (int)GameVersion.X;
                    break;
                case (int)GameVersion.B2W2:
                    pk.Version = (int)GameVersion.B2;
                    break;
                case (int)GameVersion.BW:
                    pk.Version = (int)GameVersion.B;
                    break;
                case (int)GameVersion.DP:
                case (int)GameVersion.DPPt:
                    pk.Version = (int)GameVersion.D;
                    break;
                case (int)GameVersion.COLO:
                case (int)GameVersion.XD:
                    pk.Version = (int)GameVersion.CXD;
                    break;
                case (int)GameVersion.RS:
                case (int)GameVersion.RSE:
                    pk.Version = (int)GameVersion.R;
                    break;
                case (int)GameVersion.GSC:
                    pk.Version = (int)GameVersion.C;
                    break;
                case (int)GameVersion.RBY:
                    pk.Version = (int)GameVersion.RD;
                    break;
                case (int)GameVersion.UM when original.Species == (int)Species.Greninja && original.Form == 1:
                case (int)GameVersion.US when original.Species == (int)Species.Greninja && original.Form == 1:
                    pk.Version = (int)GameVersion.SN; // Ash-Greninja
                    break;
                default:
                    pk.Version = original.Version;
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
            bvPk.BattleVersion = trainer.Game;

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
                pk.Language = (int)LanguageID.English;
                pk.SetTrainerData(tr);
            }
            pk.Egg_Location = Locations.TradedEggLocation(pk.Generation);
        }

        private static void GetSuggestedTracker(this PKM pk)
        {
            if (pk is not IHomeTrack home)
                return;

            // Check setting
            if (SetRandomTracker && home.Tracker == 0)
                home.Tracker = GetRandomULong();
            else
                home.Tracker = 0;
        }

        /// <summary>
        /// Set IV Values for the pokemon
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="set"></param>
        /// <param name="method"></param>
        /// <param name="hpType"></param>
        /// <param name="original"></param>
        private static void SetIVsPID(this PKM pk, IBattleTemplate set, PIDType method, int hpType, PKM original)
        {
            // Useful Values for computation
            int Species = pk.Species;
            int Nature = pk.Nature;
            int Gender = pk.Gender;
            int AbilityNumber = pk.AbilityNumber; // 1,2,4 (HA)

            // Find the encounter
            var li = EncounterFinder.FindVerifiedEncounter(original);
            if (li.EncounterMatch is MysteryGift mg)
            {
                var ivs = pk.IVs;
                for (int i = 0; i < mg.IVs.Length; i++)
                    ivs[i] = mg.IVs[i] > 31 ? set.IVs[i] : mg.IVs[i];
                pk.IVs = ivs;
            }
            else
            {
                pk.IVs = set.IVs;
            }
            // TODO: Something about the gen 5 events. Maybe check for nature and shiny val and not touch the PID in that case?
            // Also need to figure out hidden power handling in that case.. for PIDType 0 that may isn't even be possible.
            if (li.EncounterMatch is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U)
            {
                var e = (EncounterStatic)li.EncounterMatch;
                if (AbilityNumber == 4 && e.Ability is 0 or 1 or 2)
                    return;

                var pk8 = (PK8)pk;
                switch (e)
                {
                    case EncounterStatic8NC c: FindNestPIDIV(pk8, c, set.Shiny); break;
                    case EncounterStatic8ND c: FindNestPIDIV(pk8, c, set.Shiny); break;
                    case EncounterStatic8N c: FindNestPIDIV(pk8, c, set.Shiny); break;
                    case EncounterStatic8U c: FindNestPIDIV(pk8, c, set.Shiny); break;
                }
            }
            else if (pk.Generation > 4 || pk.VC)
            {
                if (Species == 658 && pk.Form == 1)
                    pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
                switch (li.EncounterMatch)
                {
                    case WC6 { PIDType: Shiny.FixedValue }:
                    case WC7 { PIDType: Shiny.FixedValue }:
                    case WC8 { PIDType: Shiny.FixedValue }:
                        return;
                }

                if (pk.Version >= 24)
                    return; // Don't even bother changing IVs for Gen 6+ because why bother

                if (method == PIDType.G5MGShiny)
                    return; // Don't bother

                var origpid = pk.PID;
                pk.PID = PKX.GetRandomPID(Util.Rand, Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
                if (!li.EncounterMatch.Equals(EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch))
                    pk.PID = origpid; // Bad things happen when you change the PID!
                if (li.Generation != 5)
                    return;
                if (pk is PK5 { NPokémon: true })
                    return;
                if (li.EncounterMatch is EncounterStatic5 s && (s.Gift || s.Roaming || s.Ability == 4 || s.Location == 75))
                    return;

                while (true)
                {
                    var result = (pk.PID & 1) ^ (pk.PID >> 31) ^ (pk.TID & 1) ^ (pk.SID & 1);
                    if (result == 0)
                        break;
                    pk.PID = PKX.GetRandomPID(Util.Rand, Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
                }
            }
            else // Generation 3 and 4
            {
                var encounter = li.EncounterMatch;
                switch (encounter)
                {
                    case PCD d:
                        {
                            if (d.Gift.PK.PID != 1)
                                pk.PID = d.Gift.PK.PID;
                            else if (pk.Nature != pk.PID % 25)
                                pk.SetPIDNature(Nature);
                            return;
                        }
                    case EncounterEgg:
                        pk.SetPIDNature(Nature);
                        return;
                    // EncounterTrade4 doesn't have fixed PIDs, so don't early return
                    case EncounterTrade t when encounter is EncounterTrade3 or EncounterTrade4PID or EncounterTrade4RanchGift:
                        t.SetEncounterTradeIVs(pk);
                        return; // Fixed PID, no need to mutate
                    default:
                        FindPIDIV(pk, method, hpType, set.Shiny, encounter);
                        ValidateGender(pk);
                        break;
                }
            }
        }

        /// <summary>
        /// Method to find the PID and IV associated with a nest. Shinies are just allowed
        /// since there is no way GameFreak actually brute-forces top half of the PID to flag illegals.
        /// </summary>
        /// <param name="pk">Passed PKM</param>
        /// <param name="enc">Nest encounter object</param>
        /// <param name="shiny">Shiny boolean</param>
        private static void FindNestPIDIV<T>(PK8 pk, T enc, bool shiny) where T : EncounterStatic8Nest<T>
        {
            // Preserve Nature, Form (all abilities should be possible in gen 8, so no need to early return on a mismatch for enc HA bool vs set HA bool)
            // Nest encounter RNG generation
            var iterPKM = pk.Clone();
            if (!UseXOROSHIRO)
                return;

            if (shiny && !(enc is EncounterStatic8U))
                return;

            if (pk.Species == (int)Species.Toxtricity && pk.Form != EvolutionMethod.GetAmpLowKeyResult(pk.Nature))
            {
                enc.ApplyDetailsTo(pk, GetRandomULong());
                pk.RefreshAbility(iterPKM.AbilityNumber >> 1);
                pk.StatNature = iterPKM.StatNature;
                return;
            }

            var count = 0;
            do
            {
                ulong seed = GetRandomULong();
                enc.ApplyDetailsTo(pk, seed);
                if (IsMatchCriteria<T>(pk, iterPKM))
                    break;
            } while (++count < 10_000);

            if (shiny && enc is EncounterStatic8U)
            {
                // Dynamax Adventure shinies are always XOR 1
                pk.PID = (uint)(((pk.TID ^ pk.SID ^ (pk.PID & 0xFFFF) ^ 1) << 16) | (pk.PID & 0xFFFF));
            }

            pk.Species = iterPKM.Species; // possible evolution
            // can be ability capsuled
            if (FormInfo.IsFormChangeable(pk.Species, pk.Form, iterPKM.Form, pk.Format))
                pk.Form = iterPKM.Form; // set alt form if it can be freely changed!
            pk.RefreshAbility(iterPKM.AbilityNumber >> 1);
            pk.StatNature = iterPKM.StatNature;
        }

        private static bool IsMatchCriteria<T>(PK8 pk, PKM template) where T : EncounterStatic8Nest<T>
        {
            if (template.Nature != pk.Nature) // match nature
                return false;
            if (template.Gender != pk.Gender) // match gender
                return false;
            if (template.Form != pk.Form && !FormInfo.IsFormChangeable(pk.Species, pk.Form, template.Form, pk.Format)) // match form -- Toxtricity etc
                return false;
            return true;
        }

        /// <summary>
        /// Function to generate a random ulong
        /// </summary>
        /// <returns>A random ulong</returns>
        public static ulong GetRandomULong()
        {
            return ((ulong)Util.Rand.Next(1 << 30) << 34) | ((ulong)Util.Rand.Next(1 << 30) << 4) | (uint)Util.Rand.Next(1 << 4);
        }

        /// <summary>
        /// Method to set PID, IV while validating nature.
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="Method">Given Method</param>
        /// <param name="HPType">HPType INT for preserving Hidden powers</param>
        /// <param name="shiny">Only used for CHANNEL RNG type</param>
        /// <param name="enc"></param>
        private static void FindPIDIV(PKM pk, PIDType Method, int HPType, bool shiny, IEncounterable enc)
        {
            if (Method == PIDType.None)
            {
                if (enc is WC3 wc3)
                    Method = wc3.Method;
                else
                    Method = FindLikelyPIDType(pk);

                if (pk.Version == (int)GameVersion.CXD && Method != PIDType.PokeSpot)
                    Method = PIDType.CXD;
                if (Method == PIDType.None)
                    pk.SetPIDGender(pk.Gender);
            }
            if (Method == PIDType.Method_1_Roamer && pk.HPType != (int)MoveType.Fighting - 1) // M1 Roamers can only be HP fighting
                return;
            if (Method == PIDType.Pokewalker && (pk.Nature >= 24 || pk.AbilityNumber == 4)) // No possible pokewalker matches
                return;
            var iterPKM = pk.Clone();
            var count = 0;
            do
            {
                uint seed = Util.Rand32();
                if (PokeWalkerSeedFail(seed, Method, pk, iterPKM))
                    continue;
                PIDGenerator.SetValuesFromSeed(pk, Method, seed);
                if (!(pk.Ability == iterPKM.Ability && pk.AbilityNumber == iterPKM.AbilityNumber && pk.Nature == iterPKM.Nature))
                    continue;
                if (pk.PIDAbility != iterPKM.PIDAbility)
                    continue;
                if (HPType >= 0 && pk.HPType != HPType)
                    continue;
                if (pk.PID % 25 != iterPKM.Nature) // Util.Rand32 is the way to go
                    continue;
                if (pk.Version == (int)GameVersion.CXD && Method == PIDType.CXD) // verify locks
                {
                    pk.EncryptionConstant = pk.PID;
                    var la = new LegalityAnalysis(pk);
                    if (la.Info.PIDIV.Type != PIDType.CXD || !la.Info.PIDIVMatches)
                        continue;
                }

                if (Method == PIDType.Channel && shiny != pk.IsShiny)
                    continue;
                break;
            } while (++count < 10_000_000);
        }

        /// <summary>
        /// Checks if a pokewalker seed failed, and if it did, randomizes TID and SID (to retry in the future)
        /// </summary>
        /// <param name="seed">Seed</param>
        /// <param name="method">RNG method (every method except pokewalker is ignored)</param>
        /// <param name="pk">PKM object</param>
        /// <param name="original">original encounter pkm</param>
        /// <returns></returns>
        private static bool PokeWalkerSeedFail(uint seed, PIDType method, PKM pk, PKM original)
        {
            if (method != PIDType.Pokewalker)
                return false;
            if (seed % 24 != original.Nature)
                return true;
            pk.TID = Util.Rand.Next(65535);
            pk.SID = Util.Rand.Next(65535);
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

            return pk.Generation switch
            {
                3 => EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch switch
                {
                    WC3 g => g.Method,
                    EncounterStatic => pk.Version switch
                    {
                        (int)GameVersion.CXD => PIDType.CXD,
                        (int)GameVersion.E => PIDType.Method_1,
                        (int)GameVersion.FR or (int)GameVersion.LG => PIDType.Method_1, // roamer glitch
                        _ => PIDType.Method_1,
                    },
                    EncounterSlot when pk.Version == (int)GameVersion.CXD => PIDType.PokeSpot,
                    EncounterSlot => pk.Species == (int)Species.Unown ? PIDType.Method_1_Unown : PIDType.Method_1,
                    _ => PIDType.None,
                },

                4 => EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch switch
                {
                    EncounterStatic4Pokewalker => PIDType.Pokewalker,
                    EncounterStatic s => (s.Shiny == Shiny.Always ? PIDType.ChainShiny : PIDType.Method_1),
                    PGT => PIDType.Method_1,
                    _ => PIDType.None
                },

                _ => PIDType.None
            };
        }

        /// <summary>
        /// Method to fix specific fateful encounter flags
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <param name="enc">encounter</param>
        private static void FixFatefulFlag(this PKM pk, IEncounterable enc)
        {
            switch (enc)
            {
                case EncounterSlot { Version: GameVersion.XD }: // pokespot RNG is always fateful
                    pk.FatefulEncounter = true;
                    break;
            }
        }

        /// <summary>
        /// Method to get preferred ability number based on the encounter. Useful for when multiple ability numbers have the same ability
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <param name="enc">encounter</param>
        /// <returns></returns>
        private static int GetAbilityPreference(PKM pk, IEncounterable enc)
        {
            return enc switch
            {
                EncounterTrade et => et.Ability,
                EncounterStatic es when es.Ability is not 0 and not 1 => es.Ability,
                _ => pk.AbilityNumber,
            };
        }

        /// <summary>
        /// Method to get the correct met level for a pokemon. Move up the met level till all moves are legal
        /// </summary>
        /// <param name="pk">pokemon</param>
        public static void SetCorrectMetLevel(this PKM pk)
        {
            if (pk.Met_Location is not Locations.Transfer1 and not Locations.Transfer2 and not Locations.Transfer3 and not Locations.Transfer4)
                return;
            var level = pk.Met_Level;
            if (pk.CurrentLevel <= level)
                return;
            while (pk.CurrentLevel >= pk.Met_Level)
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
        /// <param name="pk"></param>
        private static void FixEdgeCases(this PKM pk)
        {
            // Shiny Manaphy Egg
            if (pk.Species == (int)Species.Manaphy && pk.IsShiny)
            {
                pk.Egg_Location = Locations.LinkTrade4;
                pk.Met_Location = pk.Format == 4 ? (pk.HGSS ? Locations.HatchLocationHGSS : Locations.HatchLocationDPPt) : pk.Met_Location;
            }
            if (pk.Species == (int)Species.Milotic && pk.Format < 5 && pk is IContestStatsMutable c) // Evolves via beauty
                c.CNT_Beauty = 170;
            if (pk.Version == (int)GameVersion.CXD && pk.OT_Gender == (int)Gender.Female) // Colosseum and XD are sexist games.
                pk.OT_Gender = (int)Gender.Male;
        }

        /// <summary>
        /// Wrapper function for GetLegalFromTemplate but with a Timeout
        /// </summary>
        public static PKM GetLegalFromTemplateTimeout(this ITrainerInfo dest, PKM template, IBattleTemplate set, out LegalizationResult satisfied)
        {
            AsyncLegalizationResult GetLegal()
            {
                var res = dest.GetLegalFromTemplate(template, set, out var s);
                return new AsyncLegalizationResult(res, s);
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
    }
}
