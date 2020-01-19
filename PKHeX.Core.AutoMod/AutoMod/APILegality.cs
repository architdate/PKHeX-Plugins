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
        public static bool UseTrainerData { get; set; } = true;
        public static bool SetMatchingBalls { get; set; } = true;
        public static bool SetAllLegalRibbons { get; set; } = true;
        public static bool UseCompetitiveMarkings { get; set; } = false;
        public static bool UseMarkings { get; set; } = true;
        public static bool UseXOROSHIRO { get; set; } = true;

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
            var Form = SanityCheckForm(template, ref set);
            template.ApplySetDetails(set);
            template.SetRecordFlags(); // Validate TR moves for the encounter
            var destType = template.GetType();
            var destVer = (GameVersion)dest.Game;
            if (destVer <= 0 && dest is SaveFile s)
                destVer = s.Version;

            var gamelist = GameUtil.GetVersionsWithinRange(template, template.Format).OrderByDescending(c => c.GetGeneration()).ToArray();
            EncounterMovesetGenerator.PriorityList = new EncounterOrder[] { EncounterOrder.Egg, EncounterOrder.Static, EncounterOrder.Trade, EncounterOrder.Slot, EncounterOrder.Mystery };
            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves, gamelist);
            if (template.Species <= 721)
                encounters = encounters.Concat(GetFriendSafariEncounters(template));
            foreach (var enc in encounters)
            {
                var ver = enc is IVersion v ? v.Version : destVer;
                var gen = enc is IGeneration g ? g.Generation : dest.Generation;
                var tr = UseTrainerData ? TrainerSettings.GetSavedTrainerData(ver, gen) : TrainerSettings.DefaultFallback(gen);
                var raw = SanityCheckEncounters(enc).ConvertToPKM(tr);
                if (raw.IsEgg) // PGF events are sometimes eggs. Force hatch them before proceeding
                    raw.HandleEggEncounters(enc, tr);
                var pk = PKMConverter.ConvertToType(raw, destType, out _);
                if (pk == null)
                    continue;

                ApplySetDetails(pk, set, Form, raw, dest, enc);
                if (set.CanGigantamax && pk is IGigantamax gmax)
                {
                    if (!gmax.CanGigantamax)
                        continue;
                }

                var la = new LegalityAnalysis(pk);
                if (la.Valid)
                {
                    satisfied = true;
                    return pk;
                }
                Debug.WriteLine(la.Report());
            }
            satisfied = false;
            return template;
        }

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
            pk.SetMovesEVsItems(set);
            pk.SetHandlerandMemory(handler);
            pk.SetNatureAbility(set, abilitypref);
            pk.SetIVsPID(set, pidiv.Type, set.HiddenPowerType, unconverted);
            pk.SetHyperTrainingFlags(set); // Hypertrain
            pk.SetEncryptionConstant(enc);
            pk.FixFatefulFlag(enc);
            pk.SetShinyBoolean(set.Shiny, enc);
            pk.FixGender(set);
            pk.SetSuggestedRibbons(SetAllLegalRibbons);
            pk.SetSuggestedMemories();
            pk.SetHTLanguage();
            pk.SetDynamaxLevel();
            pk.SetSuggestedBall(SetMatchingBalls);
            pk.ApplyMarkings(UseMarkings, UseCompetitiveMarkings);
            pk.SetHappiness();
            pk.SetBelugaValues();
            pk.FixEdgeCases();
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
            if (!competitive || pk.Format < 7) // Simple markings dont apply with competitive atall
                // Blue for 31/1 IVs and Red for 30/0 IVs (PKHeX default behaviour)
                pk.SetMarkings();
            else
            {
                // Red for 30 denoting imperfect but close to perfect. Blue for 31. No marking for 0 IVs
                var markings = new int[] { 0, 0, 0, 0, 0, 0 };
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
            t.HT_ATK = set.IVs[1] < 3 && t.HT_ATK ? false : (set.IVs[1] >= 3 && pk.IVs[1] < 3 && pk.CurrentLevel == 100 ? true : t.HT_ATK);
            t.HT_SPE = set.IVs[3] < 3 && t.HT_SPE ? false : (set.IVs[3] >= 3 && pk.IVs[3] < 3 && pk.CurrentLevel == 100 ? true : t.HT_SPE);

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
                default:
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
        public static void SetMatchingBall(this PKM pk) => BallRandomizer.ApplyBallLegalByColor(pk);

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
                default:
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
                case (int) Species.Zacian:
                case (int) Species.Zamazenta:
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
        private static void HandleEggEncounters(this PKM pk, IEncounterable enc, ITrainerInfo SAV)
        {
            if (!pk.IsEgg)
                return; // should be checked before, but condition added for future usecases
            // Handle egg encounters. Set them as traded and force hatch them before proceeding
            pk.ForceHatchPKM();
            if (enc is MysteryGift mg && mg.IsEgg)
            {
                pk.Language = (int)LanguageID.English;
                pk.SetTrainerData(SAV);
            }
            pk.Egg_Location = Locations.TradedEggLocation(pk.GenNumber);
        }

        /// <summary>
        /// Add friend safari encounters to encounter generator
        /// </summary>
        /// <param name="pk">mock pkm to get friend safari encounters</param>
        /// <returns>IEncounterable enumaration of friend safari encounters in the evo chain</returns>
        private static IEnumerable<IEncounterable> GetFriendSafariEncounters(PKM pk)
        {
            // Set values to get a mock pk6
            pk.Version = (int)GameVersion.X;
            pk.Met_Location = 148;
            pk.Met_Level = 30;
            pk.Egg_Location = 0;

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
            // TODO: Something about the gen 5 events. Maybe check for nature and shiny val and not touch the PID in that case?
            // Also need to figure out hidden power handling in that case.. for PIDType 0 that may isn't even be possible.
            if (li.EncounterMatch is EncounterStatic8N e)
            {
                pk.IVs = set.IVs;
                if (AbilityNumber == 4 && (e.Ability == 0 || e.Ability == 1 || e.Ability == 2))
                    return;
                FindNestPIDIV(pk, e, set.Shiny);
                ValidateGender(pk);
            }
            else if (pk.GenNumber > 4 || pk.VC)
            {
                pk.IVs = set.IVs;
                if (Species == 658 && pk.AltForm == 1)
                    pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
                if (method != PIDType.G5MGShiny)
                    pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, pk.PID);
            }
            else
            {
                pk.IVs = set.IVs;
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
                FindPIDIV(pk, method, hpType);
                ValidateGender(pk);
            }
        }

        private static void FindNestPIDIV(PKM pk, EncounterStatic8N enc, bool shiny)
        {
            // Preserve Nature, Altform, Ability (only if HA)
            // Nest encounter RNG generation
            int iv_count = enc.FlawlessIVCount;
            int ability_param;
            int gender_ratio = pk.PersonalInfo.Gender;
            const int nature_param = 255; // random nature in raids

            // TODO: Ability param for A2 raids
            if (enc.Ability == 0)
                ability_param = 255;
            else if (enc.Ability == -1)
                ability_param = 254;
            else
                ability_param = enc.Ability >> 1;

            var iterPKM = pk.Clone();
            while (true)
            {
                ulong seed = GetRandomULong();
                var RNG = new XOROSHIRO(seed);
                if (!shiny && UseXOROSHIRO)
                    SetValuesFromSeed8Unshiny(pk, RNG, iv_count, ability_param, gender_ratio, nature_param);
                if (!(pk.Nature == iterPKM.Nature && pk.AltForm == iterPKM.AltForm))
                    continue;
                if (iterPKM.AbilityNumber == 4 && !(pk.Ability == iterPKM.Ability && pk.AbilityNumber == iterPKM.AbilityNumber))
                    continue;
                // can be ability capsuled
                pk.RefreshAbility(pk.AbilityNumber >> 1);
                break;
            }
        }

        private static void SetValuesFromSeed8Unshiny(PKM pk, XOROSHIRO rng, int iv_count, int ability_param, int gender_ratio, int nature_param)
        {
            pk.EncryptionConstant = (uint)rng.NextInt();
            var ftidsid = (uint)rng.NextInt(); // pass
            pk.PID = (uint)rng.NextInt();
            if (pk.PSV == ((ftidsid >> 16) ^ (ftidsid & 0xFFFF)) >> 4) // force unshiny!
                pk.PID ^= 0x10000000; // the rare case where you actually roll a full odds shiny in PKHeX. Apply for a lottery!
            int[] ivs = {-1, -1, -1, -1, -1, -1};
            for (int i = 0; i < iv_count; i++)
            {
                int idx = (int)rng.NextInt(6);
                while (ivs[idx] != -1)
                    idx = (int)rng.NextInt(6);
                ivs[idx] = 31;
            }
            for (int i = 0; i < 6; i++)
            {
                if (ivs[i] == -1)
                    ivs[i] = (int)rng.NextInt(32);
            }
            pk.IVs = new[] { ivs[0], ivs[1], ivs[2], ivs[5], ivs[3], ivs[4] };
            int abil;
            if (ability_param == 254)
                abil = (int)rng.NextInt(3);
            else if (ability_param == 255)
                abil = (int)rng.NextInt(2);
            else
                abil = ability_param;
            pk.RefreshAbility(abil);
            if (gender_ratio == 255)
                pk.SetGender(2);
            else if (gender_ratio == 254)
                pk.SetGender(1);
            else if (gender_ratio == 0)
                pk.SetGender(0);
            else if ((int)rng.NextInt(252) + 1 < gender_ratio)
                pk.SetGender(1);
            else
                pk.SetGender(0);
            if (nature_param == 255)
                pk.Nature = (int)rng.NextInt(25);
            else
                pk.Nature = nature_param;
        }

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
        private static void FindPIDIV(PKM pk, PIDType Method, int HPType)
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
            if (Method == PIDType.Pokewalker) // No possible pokewalker matches
                return;
            var iterPKM = pk.Clone();
            while (true)
            {
                uint seed = Util.Rand32();
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
                break;
            }
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

        private static void FixFatefulFlag(this PKM pk, IEncounterable enc)
        {
            switch (enc)
            {
                case EncounterSlot x when x.Version == GameVersion.XD: // pokespot RNG is always fateful
                    pk.FatefulEncounter = true; 
                    break;
            }
        }

        private static int GetAbilityPreference(PKM pk, IEncounterable enc)
        {
            var pref = pk.AbilityNumber;
            if (enc is EncounterTrade et)
                pref = et.Ability;
            else if (enc is EncounterStatic es)
            {
                if (es.Ability != 0 && es.Ability != 1)
                    pref = es.Ability;
            }
            return pref;
        }

        public static void SetCorrectMetLevel(this PKM pk)
        {
            if (pk.Met_Location != Locations.Transfer4 && pk.Met_Location != Locations.Transfer3)
                return;
            var level = pk.Met_Level;
            if (pk.CurrentLevel <= level)
                return;
            while (pk.CurrentLevel >= pk.Met_Level)
            {
                pk.Met_Level += 1;
                var la = new LegalityAnalysis(pk);
                if (la.Info.Moves.All(z => z.Valid))
                    return;
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
