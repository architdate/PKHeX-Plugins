using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="ShowdownSet"/>.
    /// </summary>
    public static class APILegality
    {
        /// <summary>
        /// Settings
        /// </summary>
        public static bool UseTrainerData { get; set; } = true;
        public static bool SetMatchingBalls { get; set; } = true;
        public static bool SetAllLegalRibbons { get; set; } = true;
        public static bool UseCompetitiveMarkings { get; set; } = false;
        public static bool UseMarkings { get; set; } = true;
        public static bool UseXOROSHIRO { get; set; } = true;
        public static bool SetRandomTracker { get; set; } = false;

        /// <summary>
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <remarks>Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="ShowdownSet"/>.</remarks>
        /// <param name="dest">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is satisfactory, otherwise use deprecated bruteforce auto legality functionality</param>
        public static PKM GetLegalFromTemplate(this ITrainerInfo dest, PKM template, ShowdownSet set, out bool satisfied)
        {
            set = set.PreProcessShowdownSet(template.PersonalInfo);
            var Form = SanityCheckForm(template, ref set);
            template.ApplySetDetails(set);
            template.SetRecordFlags(); // Validate TR moves for the encounter
            var isHidden = template.AbilityNumber == 4;
            var destType = template.GetType();
            var destVer = (GameVersion)dest.Game;
            if (destVer <= 0 && dest is SaveFile s)
                destVer = s.Version;

            var gamelist = GameUtil.GetVersionsWithinRange(template, template.Format).OrderByDescending(c => c.GetGeneration()).ToArray();
            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves, gamelist);
            encounters = encounters.Concat(GetFriendSafariEncounters(template));
            foreach (var enc in encounters)
            {
                if (enc.LevelMin > set.Level)
                    continue;
                var gen = enc is IGeneration g ? g.Generation : dest.Generation;
                if (isHidden && (uint)(gen - 3) < 2) // Gen 3 and Gen 4
                    continue;
                var ver = enc is IVersion v ? v.Version : destVer;
                if (set.CanGigantamax && !GameVersion.SWSH.Contains(ver))
                    continue;
                var tr = UseTrainerData ? TrainerSettings.GetSavedTrainerData(ver, gen) : TrainerSettings.DefaultFallback(gen);
                var raw = SanityCheckEncounters(enc).ConvertToPKM(tr);
                if (raw.IsEgg) // PGF events are sometimes eggs. Force hatch them before proceeding
                    raw.HandleEggEncounters(enc, tr);
                var pk = PKMConverter.ConvertToType(raw, destType, out _);
                if (pk == null)
                    continue;

                ApplySetDetails(pk, set, Form, raw, dest, enc);
                if (pk is IGigantamax gmax && gmax.CanGigantamax != set.CanGigantamax)
                {
                    continue;
                }

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
        /// Function to fix impossible forms
        /// </summary>
        /// <param name="template">PKM template with uncorrected set data imported</param>
        /// <param name="set">Showdown set</param>
        /// <returns></returns>
        private static int SanityCheckForm(PKM template, ref ShowdownSet set)
        {
            int Form = template.AltForm;
            if (set.Form != null && FixFormes(set, out set))
                Form = set.FormIndex;
            return Form;
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
        /// <param name="Form">Alternate form required</param>
        /// <param name="unconverted">Original pkm data</param>
        /// <param name="handler">Trainer to handle the Pokémon</param>
        /// <param name="enc">Encounter details matched to the Pokémon</param>
        private static void ApplySetDetails(PKM pk, ShowdownSet set, int Form, PKM unconverted, ITrainerInfo handler, IEncounterable enc)
        {
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
            pk.SetShinyBoolean(set.Shiny, enc);
            pk.FixGender(set);
            pk.SetSuggestedRibbons(SetAllLegalRibbons);
            pk.SetSuggestedMemories();
            pk.SetHTLanguage();
            pk.SetDynamaxLevel();
            pk.SetHappiness(enc);
            pk.SetBelugaValues();
            pk.FixEdgeCases();
            pk.SetSuggestedBall(SetMatchingBalls);
            pk.ApplyMarkings(UseMarkings, UseCompetitiveMarkings);
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
        private static void SetHyperTrainingFlags(this PKM pk, ShowdownSet set)
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
        /// Set matching colored pokeballs based on the color API in personal table
        /// </summary>
        /// <param name="pk">Return PKM</param>
        public static void SetMatchingBall(this PKM pk) => BallApplicator.ApplyBallLegalByColor(pk);

        /// <summary>
        /// Fix Formes that are illegal outside of battle
        /// </summary>
        /// <param name="set">Original Showdown Set</param>
        /// <param name="changedSet">Edited Showdown Set</param>
        /// <returns>boolen that checks if a form is fixed or not</returns>
        private static bool FixFormes(ShowdownSet set, out ShowdownSet changedSet)
        {
            changedSet = set;
            var badForm = ShowdownUtil.IsInvalidForm(set.Form);
            if (!badForm)
                return false;

            var invalidform = set.Form == "Galar-Zen" ? "Zen" : set.Form;
            changedSet = new ShowdownSet(set.Text.Replace($"-{invalidform}", string.Empty));

            // Changed set handling for forme changes that affect battle-only moves
            ReplaceBattleOnlyMoves(changedSet);
            return true;
        }

        /// <summary>
        /// General method to preprocess sets excluding invalid formes. (handled in a future method)
        /// </summary>
        /// <param name="set">Showdown set passed to the function</param>
        /// <param name="personal">Personal data for the desired form</param>
        private static ShowdownSet PreProcessShowdownSet(this ShowdownSet set, PersonalInfo personal)
        {
            if ((set.Species == (int)Species.Indeedee || set.Species == (int)Species.Meowstic) && set.Form == "F")
                set = new ShowdownSet(set.Text.Replace("(M)", "(F)"));

            // Validate Gender
            if (personal.Genderless && set.Gender.Length == 0)
                return new ShowdownSet(set.Text.Replace("(M)", "").Replace("(F)", ""));
            if (personal.OnlyFemale && set.Gender != "F")
                return new ShowdownSet(set.Text.Replace("(M)", "(F)"));
            if (personal.OnlyMale && set.Gender != "M")
                return new ShowdownSet(set.Text.Replace("(F)", "(M)"));

            return set;
        }

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
        /// Showdown quirks lets you have battle only moves in battle only formes. Transform back to base move.
        /// </summary>
        /// <param name="changedSet"></param>
        private static void ReplaceBattleOnlyMoves(ShowdownSet changedSet)
        {
            switch (changedSet.Species)
            {
                case (int)Species.Zacian:
                case (int)Species.Zamazenta:
                    {
                        // Behemoth Blade and Behemoth Bash -> Iron Head
                        if (!changedSet.Moves.Contains(781) && !changedSet.Moves.Contains(782))
                            return;

                        for (int i = 0; i < changedSet.Moves.Length; i++)
                        {
                            if (changedSet.Moves[i] == 781 || changedSet.Moves[i] == 782)
                                changedSet.Moves[i] = 442;
                        }
                        break;
                    }
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
        /// Add friend safari encounters to encounter generator
        /// </summary>
        /// <param name="pk">mock pkm to get friend safari encounters</param>
        /// <returns>IEncounterable enumaration of friend safari encounters in the evo chain</returns>
        private static IEnumerable<IEncounterable> GetFriendSafariEncounters(PKM pk)
        {
            // Set values to get a mock pk6
            pk.HT_Name = "A";
            return EncounterArea6XY.GetValidFriendSafari(pk);
        }

        /// <summary>
        /// Set IV Values for the pokemon
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="set"></param>
        /// <param name="method"></param>
        /// <param name="hpType"></param>
        /// <param name="original"></param>
        private static void SetIVsPID(this PKM pk, ShowdownSet set, PIDType method, int hpType, PKM original)
        {
            // Useful Values for computation
            int Species = pk.Species;
            int Nature = pk.Nature;
            int Gender = pk.Gender;
            int AbilityNumber = pk.AbilityNumber; // 1,2,4 (HA)

            // Find the encounter
            var li = EncounterFinder.FindVerifiedEncounter(original);
            pk.IVs = set.IVs;
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
                if (method != PIDType.G5MGShiny)
                {
                    pk.PID = PKX.GetRandomPID(Util.Rand, Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
                    if (li.Generation != 5)
                        return;

                    while (true)
                    {
                        if (li.EncounterMatch is EncounterStatic s && (s.Gift || s.Roaming || s.Ability != 4 || s.Location == 75))
                            break;
                        if (pk is PK5 p && p.NPokémon)
                            break;
                        var result = (pk.PID & 1) ^ (pk.PID >> 31) ^ (pk.TID & 1) ^ (pk.SID & 1);
                        if (result == 0)
                            break;
                        pk.PID = PKX.GetRandomPID(Util.Rand, Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
                    }
                }
            }
            else
            {
                if (li.EncounterMatch is PCD d)
                {
                    if (d.Gift.PK.PID != 1)
                        pk.PID = d.Gift.PK.PID;
                    else if (pk.Nature != pk.PID % 25)
                        pk.SetPIDNature(Nature);
                    return;
                }
                if (li.EncounterMatch is EncounterEgg)
                {
                    pk.SetPIDNature(Nature);
                    return;
                }
                if (li.EncounterMatch is EncounterTradePID t)
                {
                    t.SetEncounterTradeIVs(pk);
                    return; // Fixed PID, no need to mutate
                }
                FindPIDIV(pk, method, hpType, set.Shiny);
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
            } while (++ count < 10_000);

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
            // if (template.AltForm != pk.AltForm) // match form -- no variable forms
                // return false;
            return true;
        }

        /// <summary>
        /// Function to generate a random ulong
        /// </summary>
        /// <returns>A random ulong</returns>
        private static ulong GetRandomULong()
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
        private static void FindPIDIV(PKM pk, PIDType Method, int HPType, bool shiny)
        {
            if (Method == PIDType.None)
            {
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
            if (BruteForce.UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.BACD_R))
                return PIDType.BACD_R;
            if (BruteForce.UsesEventBasedMethod(pk.Species, pk.Moves, PIDType.Method_2))
                return PIDType.Method_2;
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
            var la = new LegalityAnalysis(pk);
            while (pk.CurrentLevel >= pk.Met_Level)
            {
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
