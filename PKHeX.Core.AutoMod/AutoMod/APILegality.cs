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
        /// Main function that auto legalizes based on the legality
        /// </summary>
        /// <remarks>Leverages <see cref="Core"/>'s <see cref="EncounterMovesetGenerator"/> to create a <see cref="PKM"/> from a <see cref="ShowdownSet"/>.</remarks>
        /// <param name="dest">Destination for the generated pkm</param>
        /// <param name="template">rough pkm that has all the <see cref="set"/> values entered</param>
        /// <param name="set">Showdown set object</param>
        /// <param name="satisfied">If the final result is satisfactory, otherwise use current auto legality functionality</param>
        public static PKM GetLegalFromTemplate(this ITrainerInfo dest, PKM template, ShowdownSet set, out bool satisfied)
        {
            var Form = SanityCheckForm(template, ref set);
            template.ApplySetDetails(set);
            template.SetRecordFlags(); // Validate TR moves for the encounter
            var destType = template.GetType();
            var destVer = (GameVersion)dest.Game;
            if (destVer <= 0 && dest is SaveFile s)
                destVer = s.Version;

            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: template, moves: set.Moves);
            foreach (var enc in encounters)
            {
                var ver = enc is IVersion v ? v.Version : destVer;
                var gen = enc is IGeneration g ? g.Generation : dest.Generation;
                var tr = TrainerSettings.GetSavedTrainerData(ver, gen, new SimpleTrainerInfo(ver));
                var raw = SanityCheckEncounters(enc).ConvertToPKM(tr);
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
            var SharedNest = 162; // Shared Nest for online encounter
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
        private static void ApplySetDetails(PKM pk, ShowdownSet set, int Form, PKM unconverted, ITrainerInfo handler, IEncounterable enc)
        {
            var pidiv = MethodFinder.Analyze(pk);

            pk.SetVersion(unconverted); // Preemptive Version setting
            pk.SetSpeciesLevel(set, Form);
            pk.SetRecordFlags(set.Moves);
            pk.SetMovesEVsItems(set);
            pk.SetHandlerandMemory(handler);
            pk.SetNatureAbility(set);
            pk.SetIVsPID(set, pidiv.Type, set.HiddenPowerType, unconverted);
            pk.SetSuggestedHyperTrainingData(pk.IVs); // Hypertrain
            pk.SetEncryptionConstant(enc);
            pk.SetShinyBoolean(set.Shiny, enc);
            pk.FixGender(set);
            pk.SetSuggestedRibbons();
            pk.SetSuggestedMemories();
            pk.SetHTLanguage();
            pk.SetDynamaxLevel();
            pk.SetSuggestedBall();
            pk.SetMarkings();
            pk.SetHappiness();
            pk.SetBelugaValues();
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

            changedSet = new ShowdownSet(set.Text.Replace($"-{set.Form}", string.Empty));

            // Changed set handling for forme changes that affect battle-only moves
            ReplaceBattleOnlyMoves(changedSet);
            return true;
        }

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
                if (AbilityNumber == 4 && (e.Ability == 0 || e.Ability == 1 || e.Ability == 2)) { } // encounter impossible 
                else
                {
                    FindNestPIDIV(pk, e, set.Shiny);
                    ValidateGender(pk);
                }
            }
            else if (pk.GenNumber > 4 || pk.VC)
            {
                pk.IVs = set.IVs;
                if (Species == 658 && pk.AltForm == 1)
                    pk.IVs = new[] { 20, 31, 20, 31, 31, 20 };
                if (method != PIDType.G5MGShiny)
                    pk.PID = PKX.GetRandomPID(Species, Gender, pk.Version, Nature, pk.Format, (uint)(AbilityNumber * 0x10001));
            }
            else
            {
                pk.IVs = set.IVs;
                if (li.EncounterMatch is PCD)
                    return;
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
            int altform_count = pk.PersonalInfo.FormeCount;
            int gender_ratio = pk.PersonalInfo.Gender;
            int nature_param = 255; // random nature in raids

            // TODO: Ability param for A2 raids
            if (enc.Ability == 0) ability_param = 255;
            else if (enc.Ability == -1) ability_param = 254;
            else ability_param = enc.Ability >> 1;

            var iterPKM = pk.Clone();
            while (true)
            {
                ulong seed = GetRandomULong();
                var RNG = new XOROSHIRO(seed);
                if (!shiny)
                    SetValuesFromSeed8Unshiny(pk, RNG, iv_count, ability_param, altform_count, gender_ratio, nature_param);
                if (!(pk.Nature == iterPKM.Nature && pk.AltForm == iterPKM.AltForm))
                    continue;
                if (iterPKM.AbilityNumber == 4 && !(pk.Ability == iterPKM.Ability && pk.AbilityNumber == iterPKM.AbilityNumber))
                    continue;
                else
                    // can be ability capsuled
                    pk.RefreshAbility(pk.AbilityNumber >> 1);
                break;
            }

        }

        private static void SetValuesFromSeed8Unshiny(PKM pk, XOROSHIRO rng, int iv_count, int ability_param, int altform_count, int gender_ratio, int nature_param)
        {
            pk.EncryptionConstant = rng.nextInt();
            var ftidsid = rng.nextInt(); // pass
            pk.PID = rng.nextInt();
            if (pk.PSV == ((ftidsid >> 16) ^ (ftidsid & 0xFFFF)) >> 4) // force unshiny!
                pk.PID = pk.PID ^ 0x10000000; // the rare case where you actually roll a full odds shiny in pkhex. Apply for a lottery!
            int[] ivs = new int[] {-1, -1, -1, -1, -1, -1};
            for (int i = 0; i < iv_count; i++)
            {
                int idx = (int)rng.nextInt(6);
                while (ivs[idx] != -1)
                    idx = (int)rng.nextInt(6);
                ivs[idx] = 31;
            }
            for (int i = 0; i < 6; i++)
            {
                if (ivs[i] == -1)
                    ivs[i] = (int)rng.nextInt(32);
            }
            pk.IVs = ivs;
            int abil;
            if (ability_param == 254)
                abil = (int)rng.nextInt(3);
            else if (ability_param == 255)
                abil = (int)rng.nextInt(2);
            else
                abil = ability_param;
            pk.RefreshAbility(abil);
            if (altform_count != 1)
                pk.AltForm = (int)rng.nextInt((ulong)altform_count);
            else
                pk.AltForm = 0;
            if (gender_ratio == 255)
                pk.SetGender(2);
            else if (gender_ratio == 254)
                pk.SetGender(1);
            else if (gender_ratio == 0)
                pk.SetGender(0);
            else if ((int)rng.nextInt(252) + 1 < gender_ratio)
                pk.SetGender(1);
            else
                pk.SetGender(0);
            if (nature_param == 255)
                pk.Nature = ((int)rng.nextInt(25));
            else
                pk.Nature = (nature_param);
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
                if (pk.Version == (int)GameVersion.CXD)
                    Method = PIDType.CXD;
                if (Method == PIDType.None)
                    pk.SetPIDGender(pk.Gender);
            }
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

        // Implemention from Leannys XOROSHIRO method and Admiral Fish's XOROSHIRO method
        public class XOROSHIRO
        {
            static ulong[] s = { 0, 0 };
            static ulong rotl(ulong x, int k)
            {
                return (x << k) | (x >> (64 - k));
            }
            public XOROSHIRO(ulong seed)
            {
                s[0] = seed;
                s[1] = 0x82A2B175229D6A5B;
            }
            public ulong next()
            {
                ulong s0 = s[0];
                ulong s1 = s[1];
                ulong result = s0 + s1;

                s1 ^= s0;
                s[0] = rotl(s0, 24) ^ s1 ^ (s1 << 16);
                s[1] = rotl(s1, 37);

                return result;
            }
            private ulong nextP2(ulong n)
            {
                n--;
                for (int i = 0; i < 6; i++)
                {
                    n |= n >> (1 << i);
                }
                return n;
            }
            public uint nextInt(ulong MOD = 0xFFFFFFFF)
            {
                ulong res = 0;
                ulong p2mod = nextP2(MOD);
                do
                {
                    res = next() & p2mod;
                } while (res >= MOD);
                return (uint) res;
            }
        }
    }
}
