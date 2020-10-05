using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <remarks>Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="IBattleTemplate"/>.</remarks>
        /// <param name="dest">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is legal or not</param>
        public static PKM GetLegalFromTemplate(this ITrainerInfo dest, PKM template, IBattleTemplate set, out bool satisfied)
        {
            if (set is RegenTemplate t)
                t.FixGender(template.PersonalInfo);
            template.ApplySetDetails(set);
            template.SetRecordFlags(); // Validate TR moves for the encounter
            var isHidden = template.AbilityNumber == 4;
            var destType = template.GetType();
            var destVer = (GameVersion)dest.Game;
            if (destVer <= 0 && dest is SaveFile s)
                destVer = s.Version;

            var gamelist = GameUtil.GetVersionsWithinRange(template, template.Format).OrderByDescending(c => c.GetGeneration()).ToArray();
            if (PrioritizeGame)
                gamelist = PrioritizeGameVersion == GameVersion.Any ? PrioritizeVersion(gamelist, destVer) : PrioritizeVersion(gamelist, PrioritizeGameVersion);
            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves, gamelist);
            foreach (var enc in encounters)
            {
                if (!IsEncounterValid(set, enc, isHidden, destVer, out var ver))
                    continue;
                var tr = UseTrainerData ? TrainerSettings.GetSavedTrainerData(ver, enc.Generation) : TrainerSettings.DefaultFallback(enc.Generation);
                var raw = SanityCheckEncounters(enc).ConvertToPKM(tr);
                if (raw.IsEgg) // PGF events are sometimes eggs. Force hatch them before proceeding
                    raw.HandleEggEncounters(enc, tr);
                var pk = PKMConverter.ConvertToType(raw, destType, out _);
                if (pk == null)
                    continue;

                ApplySetDetails(pk, set, raw, dest, enc);
                if (pk is IGigantamax gmax && gmax.CanGigantamax != set.CanGigantamax)
                {
                    if (gmax.CanToggleGigantamax(pk.Species, enc.Species))
                        gmax.CanGigantamax = set.CanGigantamax; // soup hax
                    else continue;
                }

                if (pk is PK1 gen1Pk && ParseSettings.AllowGen1Tradeback)
                    gen1Pk.Catch_Rate = gen1Pk.Gen2Item; // Simulate a gen 2 trade/tradeback to allow tradeback moves

                var la = new LegalityAnalysis(pk);
                if (la.Valid)
                {
                    satisfied = true;
                    return pk;
                }
                Debug.WriteLine($"{la.Report()}\n");
            }
            satisfied = false;
            return template;
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
                if (!(enc is EncounterStatic8N))
                    return false;
            }

            // Don't process if Hidden Ability is requested and the PKM is from Gen 3 or Gen 4
            var gen = enc.Generation;
            if (isHidden && (uint) (gen - 3) < 2) // Gen 3 and Gen 4
                return false;

            // Don't process if requested PKM is Gigantamax but the Game is not SW/SH
            ver = enc is IVersion v ? v.Version : destVer;
            if (set.CanGigantamax && !GameVersion.SWSH.Contains(ver))
                return false;

            // Don't process if Game is LGPE and requested PKM is not Kanto / Meltan / Melmetal
            // Don't process if Game is SWSH and requested PKM is not from the Galar Dex (Zukan8.DexLookup)
            if (GameVersion.GG.Contains(destVer))
                return set.Species <= 151 || set.Species == 808 || set.Species == 809;
            if (GameVersion.SWSH.Contains(destVer))
                return ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormeEntry(set.Species, set.FormIndex)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(set.Species);
            if (set.Species > destVer.GetMaxSpeciesID())
                return false;

            // Encounter should hopefully be possible
            return true;
        }

        /// <summary>
        /// Sanity checking encounters before passing them into ApplySetDetails.
        /// Some encounters may have an empty met location leading to an encounter mismatch. Use this function for all encounter pre-processing!
        /// </summary>
        /// <param name="enc">IEncounterable variable that is a product of the Encounter Generator</param>
        /// <returns></returns>
        private static IEncounterable SanityCheckEncounters(IEncounterable enc)
        {
            const int SharedNest = 162; // Shared Nest for online encounter
            if (enc is EncounterStatic8N e && e.Location == 0)
                e.Location = SharedNest;
            if (enc is EncounterStatic8ND ed && ed.Location == 0)
                ed.Location = SharedNest;
            return enc;
        }

        /// <summary>
        /// Modifies the provided <see cref="pk"/> to the specifications required by <see cref="set"/>.
        /// </summary>
        /// <param name="pk">Converted final pkm to apply details to</param>
        /// <param name="set">Set details required</param>
        /// <param name="unconverted">Original pkm data</param>
        /// <param name="handler">Trainer to handle the Pokémon</param>
        /// <param name="enc">Encounter details matched to the Pokémon</param>
        private static void ApplySetDetails(PKM pk, IBattleTemplate set, PKM unconverted, ITrainerInfo handler, IEncounterable enc)
        {
            int Form = set.FormIndex;
            var pidiv = MethodFinder.Analyze(pk);
            var abilitypref = GetAbilityPreference(pk, enc);

            pk.SetVersion(unconverted); // Preemptive Version setting
            pk.SetSpeciesLevel(set, Form, enc);
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
            pk.SetShinyBoolean(set.Shiny, enc, set is RegenTemplate s ? s.ShinyType : Shiny.Random);
            pk.FixGender(set);
            pk.SetSuggestedRibbons(SetAllLegalRibbons);
            pk.SetSuggestedMemories();
            pk.SetHTLanguage();
            pk.SetDynamaxLevel();
            pk.SetFriendship(enc);
            pk.SetBelugaValues();
            pk.FixEdgeCases();
            pk.SetSuggestedBall(SetMatchingBalls, ForceSpecifiedBall, set is RegenTemplate b ? b.Ball : Ball.None);
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
                else if (pk.Format > 5 && (pk.Species == 183 || pk.Species == 184))
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
                var markings = new[] { 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < pk.IVs.Length; i++)
                    if (pk.IVs[i] == 31 || pk.IVs[i] == 30) markings[i] = pk.IVs[i] == 31 ? 1 : 2;
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
            if (!(pk is IHyperTrain t))
                return;
            pk.SetSuggestedHyperTrainingData(); // Set IV data based on showdownset

            // Fix HT flags as necessary
            t.HT_ATK = (set.IVs[1] >= 3 || !t.HT_ATK) && ((set.IVs[1] >= 3 && pk.IVs[1] < 3 && pk.CurrentLevel == 100) || t.HT_ATK);
            t.HT_SPE = (set.IVs[3] >= 3 || !t.HT_SPE) && ((set.IVs[3] >= 3 && pk.IVs[3] < 3 && pk.CurrentLevel == 100) || t.HT_SPE);

            // Handle special cases here for ultrabeasts
            switch (pk.Species)
            {
                case (int)Species.Kartana when pk.Nature == (int)Nature.Timid && ((set.IVs[1] <= 19 && pk.CurrentLevel == 100) || (set.IVs[1] <= 21 && pk.CurrentLevel == 50)): // Speed boosting Timid Kartana ATK IVs <= 19
                    t.HT_ATK = false;
                    break;
                case (int)Species.Stakataka when pk.Nature == (int)Nature.Lonely && ((set.IVs[2] <= 15 && pk.CurrentLevel == 100) || (set.IVs[2] <= 17 && pk.CurrentLevel == 50)): // Atk boosting Lonely Stakataka DEF IVs <= 15
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
                case (int)GameVersion.UM when original.Species == (int)Species.Greninja && original.AltForm == 1:
                case (int)GameVersion.US when original.Species == (int)Species.Greninja && original.AltForm == 1:
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
            if (pk.IsNative)
                return;
            if (!(pk is IBattleVersion bvPk))
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
        /// Set formes of specific species to altform 0 since they cannot have a form while boxed
        /// </summary>
        /// <param name="pk">pokemon passed to the method</param>
        public static void SetBoxForm(this PKM pk)
        {
            switch (pk.Species)
            {
                case (int)Species.Shaymin when pk.AltForm != 0:
                case (int)Species.Hoopa when pk.AltForm != 0:
                case (int)Species.Furfrou when pk.AltForm != 0:
                    pk.AltForm = 0;
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
            if (enc is MysteryGift mg && mg.IsEgg)
            {
                pk.Language = (int)LanguageID.English;
                pk.SetTrainerData(tr);
            }
            pk.Egg_Location = Locations.TradedEggLocation(pk.GenNumber);
        }

        private static void GetSuggestedTracker(this PKM pk)
        {
            var origin_gen = pk.GenNumber;
            if (pk is IHomeTrack home)
            {
                // Check setting
                if (SetRandomTracker && origin_gen < 8 && home.Tracker == 0)
                    home.Tracker = GetRandomULong();
                else
                    home.Tracker = 0;
            }
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
            if (li.EncounterMatch is EncounterStatic8N || li.EncounterMatch is EncounterStatic8NC || li.EncounterMatch is EncounterStatic8ND)
            {
                var e = (EncounterStatic)li.EncounterMatch;
                if (AbilityNumber == 4 && (e.Ability == 0 || e.Ability == 1 || e.Ability == 2))
                    return;

                var pk8 = (PK8)pk;
                switch (e)
                {
                    case EncounterStatic8NC c: FindNestPIDIV(pk8, c, set.Shiny); break;
                    case EncounterStatic8ND c: FindNestPIDIV(pk8, c, set.Shiny); break;
                    case EncounterStatic8N c: FindNestPIDIV(pk8, c, set.Shiny); break;
                }
            }
            else if (pk.GenNumber > 4 || pk.VC)
            {
                if (Species == 658 && pk.AltForm == 1)
                    pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
                if (li.EncounterMatch is WC6 w6 && w6.PIDType == Shiny.FixedValue) return;
                if (li.EncounterMatch is WC7 w7 && w7.PIDType == Shiny.FixedValue) return;
                if (li.EncounterMatch is WC8 w8 && w8.PIDType == Shiny.FixedValue) return;
                if (pk.Version >= 24) return; // Don't even bother changing IVs for Gen 6+ because why bother
                if (method != PIDType.G5MGShiny)
                {
                    var origpid = pk.PID;
                    pk.PID = PKX.GetRandomPID(Util.Rand, Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
                    if (!li.EncounterMatch.Equals(EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch))
                        pk.PID = origpid; // Bad things happen when you change the PID!
                    if (li.Generation != 5)
                        return;
                    if (pk is PK5 p && p.NPokémon)
                        return;
                    if (li.EncounterMatch is EncounterStatic5 s && (s.Gift || s.Roaming || s.Ability != 4 || s.Location == 75))
                        return;

                    while (true)
                    {
                        var result = (pk.PID & 1) ^ (pk.PID >> 31) ^ (pk.TID & 1) ^ (pk.SID & 1);
                        if (result == 0)
                            break;
                        pk.PID = PKX.GetRandomPID(Util.Rand, Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
                    }
                }
            }
            else // Generation 3 and 4
            {
                var encounter = li.EncounterMatch;
                if (encounter is PCD d)
                {
                    if (d.Gift.PK.PID != 1)
                        pk.PID = d.Gift.PK.PID;
                    else if (pk.Nature != pk.PID % 25)
                        pk.SetPIDNature(Nature);
                    return;
                }
                if (encounter is EncounterEgg)
                {
                    pk.SetPIDNature(Nature);
                    return;
                }
                if (encounter is EncounterTrade t)
                {
                    // EncounterTrade4 doesn't have fixed PIDs, so don't early return
                    if (encounter is EncounterTrade3 || encounter is EncounterTrade4PID)
                    {
                        t.SetEncounterTradeIVs(pk);
                        return; // Fixed PID, no need to mutate
                    }
                }
                FindPIDIV(pk, method, hpType, set.Shiny, encounter);
                ValidateGender(pk);
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
            // Preserve Nature, Altform, Ability (only if HA)
            // Nest encounter RNG generation
            var iterPKM = pk.Clone();
            if (enc.Ability != -1 && (pk.AbilityNumber == 4) != (enc.Ability == 4))
                return;
            if (pk.Species == (int) Species.Toxtricity && pk.AltForm != EvolutionMethod.GetAmpLowKeyResult(pk.Nature))
            {
                enc.ApplyDetailsTo(pk, GetRandomULong());
                pk.RefreshAbility(iterPKM.AbilityNumber >> 1);
                pk.StatNature = iterPKM.StatNature;
                return;
            }

            if (shiny || !UseXOROSHIRO)
                return;

            var count = 0;
            do
            {
                ulong seed = GetRandomULong();
                enc.ApplyDetailsTo(pk, seed);
                if (IsMatchCriteria<T>(pk, iterPKM))
                    break;
            } while (++count < 10_000);

            pk.Species = iterPKM.Species; // possible evolution
            // can be ability capsuled
            pk.RefreshAbility(iterPKM.AbilityNumber >> 1);
            pk.StatNature = iterPKM.StatNature;
        }

        private static bool IsMatchCriteria<T>(PK8 pk, PKM template) where T : EncounterStatic8Nest<T>
        {
            if (template.Nature != pk.Nature) // match nature
                return false;
            if (template.Gender != pk.Gender) // match gender
                return false;
            if (template.AbilityNumber == 4 && !(template.Ability == pk.Ability && template.AbilityNumber == pk.AbilityNumber))
                return false;
            if (template.AbilityNumber != 4 && pk.AbilityNumber == 4) // cannot ability capsule HA to non HA
                return false;
            if (template.AltForm != pk.AltForm) // match form -- Toxtricity etc
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
            switch (pk.GenNumber)
            {
                case 3:
                    switch (EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch)
                    {
                        case WC3 g:
                            return g.Method;
                        case EncounterStatic _:
                            switch (pk.Version)
                            {
                                case (int)GameVersion.CXD: return PIDType.CXD;
                                case (int)GameVersion.E: return PIDType.Method_1;
                                case (int)GameVersion.FR:
                                case (int)GameVersion.LG:
                                    return PIDType.Method_1; // roamer glitch
                                default:
                                    return PIDType.Method_1;
                            }
                        case EncounterSlot _ when pk.Version == (int)GameVersion.CXD:
                            return PIDType.PokeSpot;
                        case EncounterSlot _:
                            return pk.Species == (int)Species.Unown ? PIDType.Method_1_Unown : PIDType.Method_1;
                        default:
                            return PIDType.None;
                    }
                case 4:
                    return EncounterFinder.FindVerifiedEncounter(pk).EncounterMatch switch
                    {
                        EncounterStatic s when s.Location == Locations.PokeWalker4 && s.Gift => PIDType.Pokewalker,
                        EncounterStatic s => (s.Shiny == Shiny.Always ? PIDType.ChainShiny : PIDType.Method_1),
                        PGT _ => PIDType.Method_1,
                        _ => PIDType.None
                    };
                default:
                    return PIDType.None;
            }
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
                case EncounterSlot x when x.Version == GameVersion.XD: // pokespot RNG is always fateful
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
                EncounterStatic es when es.Ability != 0 && es.Ability != 1 => es.Ability,
                _ => pk.AbilityNumber,
            };
        }

        /// <summary>
        /// Method to get the correct met level for a pokemon. Move up the met level till all moves are legal
        /// </summary>
        /// <param name="pk">pokemon</param>
        public static void SetCorrectMetLevel(this PKM pk)
        {
            if (pk.Met_Location != Locations.Transfer4 && pk.Met_Location != Locations.Transfer3)
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
            if (pk.Species == (int)Species.Milotic && pk.Format < 5 && pk is IContestStats c) // Evolves via beauty
                c.CNT_Beauty = 170;
            if (pk.Version == (int)GameVersion.CXD && pk.OT_Gender == (int)Gender.Female) // Colosseum and XD are sexist games.
                pk.OT_Gender = (int)Gender.Male;
        }
    }
}
