using System;
using System.Collections.Generic;
using System.Linq;
using static PKHeX.Core.Species;

namespace PKHeX.Core.AutoMod
{
    public static class SimpleEdits
    {
        static SimpleEdits()
        {
            // Make PKHeX use our own marking method
            MarkingApplicator.MarkingMethod = FlagIVsAutoMod;
        }

        internal static readonly int[] Roaming_MetLocation_BDSP =
        {
            197, 201, 354, 355, 356, 357, 358, 359, 361, 362, 364, 365, 367, 373, 375, 377,
            378, 379, 383, 385, 392, 394, 395, 397, 400, 403, 404, 407, 411, 412, 414, 416,
            420,
        };

        internal static readonly HashSet<int> AlolanOriginForms = new()
        {
            019, // Rattata
            020, // Raticate
            027, // Sandshrew
            028, // Sandslash
            037, // Vulpix
            038, // Ninetales
            050, // Diglett
            051, // Dugtrio
            052, // Meowth
            053, // Persian
            074, // Geodude
            075, // Graveler
            076, // Golem
            088, // Grimer
            089, // Muk
        };

        public static bool IsShinyLockedSpeciesForm(int species, int form)
        {
            var tuple = ((Species)species, form);
            return ShinyLockedSpeciesForm.Contains(tuple);
        }

        private static readonly HashSet<(Species, int)> ShinyLockedSpeciesForm = new()
        {
            // Cap Pikachus
            ( Pikachu, 1 ),
            ( Pikachu, 2 ),
            ( Pikachu, 3 ),
            ( Pikachu, 4 ),
            ( Pikachu, 5 ),
            ( Pikachu, 6 ),
            ( Pikachu, 7 ),
            ( Pikachu, 9 ),

            ( Pichu, 1 ),

            ( Victini, 0 ),
            ( Keldeo, 0 ),
            ( Keldeo, 1 ),
            ( Meloetta, 0 ),

            // Vivillons
            ( Scatterbug, 18 ),
            ( Scatterbug, 19 ),
            ( Spewpa, 18 ),
            ( Spewpa, 19 ),
            ( Vivillon, 18 ),
            ( Vivillon, 19 ),

            // Hoopa
            ( Hoopa, 0 ),
            ( Hoopa, 1 ),

            ( Volcanion, 0 ),
            ( Cosmog, 0 ),
            ( Cosmoem, 0 ),
            ( Magearna, 0 ),
            ( Magearna, 1 ),
            ( Marshadow, 0 ),

            ( Eternatus, 0 ),

            ( Kubfu, 0 ),
            ( Urshifu, 0 ),
            ( Urshifu, 1 ),
            ( Zarude, 0 ),
            ( Zarude, 1 ),
            ( Glastrier, 0 ),
            ( Spectrier, 0 ),
            ( Calyrex, 0 ),
            ( Calyrex, 1 ),
            ( Calyrex, 2 ),

            ( Enamorus, 0 ),
            ( Enamorus, 1 ),
        };

        public static HashSet<int> Gen1TradeEvos = new () { (int)Kadabra, (int)Machoke, (int)Graveler, (int)Haunter };

        private static Func<int, int, int> FlagIVsAutoMod(PKM pk)
        {
            if (pk.Format < 7)
                return GetSimpleMarking;
            return GetComplexMarking;

            // value, index
            static int GetSimpleMarking(int val, int _) => val == 31 ? 1 : 0;
            static int GetComplexMarking(int val, int _)
            {
                return val switch
                {
                    31 => 1,
                    1 => 2,
                    0 => 2,
                    _ => 0,
                };
            }
        }

        /// <summary>
        /// Set Encryption Constant based on PKM Generation
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="enc">Encounter details</param>
        public static void SetEncryptionConstant(this PKM pk, IEncounterable enc)
        {
            if (pk.Format < 6)
                return;

            if ((pk.Species == 658 && pk.Form == 1) || APILegality.IsPIDIVSet(pk, enc)) // Ash-Greninja or raids
                return;
            int gen = pk.Generation;
            if (gen is 3 or 4 or 5)
            {
                var ec = pk.PID;
                pk.EncryptionConstant = ec;
                var pidxor = ((pk.TID ^ pk.SID ^ (int)(ec & 0xFFFF) ^ (int)(ec >> 16)) & ~0x7) == 8;
                pk.PID = pidxor ? ec ^ 0x80000000 : ec;
                return;
            }
            int wIndex = WurmpleUtil.GetWurmpleEvoGroup(pk.Species);
            if (wIndex != -1)
            {
                pk.EncryptionConstant = WurmpleUtil.GetWurmpleEncryptionConstant(wIndex);
                return;
            }

            if (pk.EncryptionConstant != 0)
                return;

            pk.EncryptionConstant = enc is WC8 { PIDType: ShinyType8.FixedValue, EncryptionConstant: 0 } ? 0 : Util.Rand32();
        }

        /// <summary>
        /// Sets shiny value to whatever boolean is specified. Takes in specific shiny as a boolean. Ignores it for stuff that is gen 5 or lower. Cant be asked to find out all legality quirks
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="isShiny">Shiny value that needs to be set</param>
        /// <param name="enc">Encounter details</param>
        /// <param name="shiny">Set is shiny</param>
        public static void SetShinyBoolean(this PKM pk, bool isShiny, IEncounterable enc, Shiny shiny = Shiny.Random)
        {
            if (pk.IsShiny == isShiny)
                return; // don't mess with stuff if pk is already shiny. Also do not modify for specific shinies (Most likely event shinies)

            if (!isShiny)
            {
                pk.SetUnshiny();
                return;
            }

            if (enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U)
            {
                pk.SetRaidShiny(shiny, enc);
                return;
            }

            if (enc is WC8 w8)
            {
                var isHOMEGift = w8.Location == 30018 || w8.GetOT(2) == "HOME";
                if (isHOMEGift)
                {
                    // Set XOR as 0 so SID comes out as 8 or less, Set TID based on that (kinda like a setshinytid)
                    pk.TID = (int)(0 ^ (pk.PID & 0xFFFF) ^ (pk.PID >> 16));
                    pk.SID = Util.Rand.Next(8);
                    return;
                }
            }

            if (pk.Generation > 5 || pk.VC)
            {
                if (shiny is Shiny.FixedValue or Shiny.Never)
                    return;

                while (true)
                {
                    pk.SetShiny();
                    switch (shiny)
                    {
                        case Shiny.AlwaysSquare when pk.ShinyXor != 0:
                            continue;
                        case Shiny.AlwaysStar when pk.ShinyXor == 0:
                            continue;
                    }
                    return;
                }
            }

            if (enc is MysteryGift mg)
            {
                if (mg.IsEgg || mg is PGT { IsManaphyEgg: true })
                {
                    pk.SetShinySID(); // not SID locked
                    return;
                }

                pk.SetShiny();
                if (pk.Format < 6)
                    return;

                do { pk.SetShiny(); }
                while (IsBit3Set());

                bool IsBit3Set() =>
                    ((pk.TID ^ pk.SID ^ (int)(pk.PID & 0xFFFF) ^ (int)(pk.PID >> 16)) & ~0x7) == 8;

                return;
            }

            pk.SetShinySID(); // no mg = no lock

            if (pk.Generation != 5)
                return;

            while (true)
            {
                pk.PID = EntityPID.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, pk.PID);
                if (shiny == Shiny.AlwaysSquare && pk.ShinyXor != 0)
                    continue;
                if (shiny == Shiny.AlwaysStar && pk.ShinyXor == 0)
                    continue;
                var validg5sid = pk.SID & 1;
                pk.SetShinySID();
                pk.EncryptionConstant = pk.PID;
                var result = (pk.PID & 1) ^ (pk.PID >> 31) ^ (pk.TID & 1) ^ (pk.SID & 1);
                if ((validg5sid == (pk.SID & 1)) && result == 0)
                    break;
            }
        }

        public static void SetRaidShiny(this PKM pk, Shiny shiny, IEncounterable enc)
        {
            if (pk.IsShiny)
                return;

            while (true)
            {
                pk.SetShiny();
                if (pk.Format <= 7)
                    return;
                var xor = pk.ShinyXor;
                if (enc is EncounterStatic8U && xor != 1 && shiny != Shiny.AlwaysSquare)
                    continue;
                if ((shiny == Shiny.AlwaysStar && xor == 1) || (shiny == Shiny.AlwaysSquare && xor == 0) || ((shiny is Shiny.Always or Shiny.Random) && xor < 2)) // allow xor1 and xor0 for den shinies
                    return;
            }
        }

        public static void ClearRelearnMoves(this PKM pk)
        {
            pk.RelearnMove1 = 0;
            pk.RelearnMove2 = 0;
            pk.RelearnMove3 = 0;
            pk.RelearnMove4 = 0;
        }

        public static uint GetShinyPID(int tid, int sid, uint pid, int type)
        {
            return (uint)(((tid ^ sid ^ (pid & 0xFFFF) ^ type) << 16) | (pid & 0xFFFF));
        }

        public static void ApplyHeightWeight(this PKM pk, IEncounterable enc, bool signed = true)
        {
            if (pk.Generation < 8 && pk.Format >= 8 && !pk.GG) // height and weight don't apply prior to GG
                return;
            if (pk is IScaledSizeValue obj) // Deal with this later -- restrictions on starters/statics/alphas, for now roll with whatever encounter DB provides
            {
                obj.HeightAbsolute = obj.CalcHeightAbsolute;
                obj.WeightAbsolute = obj.CalcWeightAbsolute;
                if (pk is PB7 pb1)
                    pb1.ResetCP();
                return;
            }
            if (pk is not IScaledSize size)
                return;

            // fixed height and weight
            if (enc is EncounterTrade8b)
                return;
            if (enc is EncounterStatic8a { HasFixedHeight: true } || enc is EncounterStatic8a { HasFixedWeight: true })
                return;

            if (enc is WC8 w8)
            {
                var isHOMEGift = w8.Location == 30018 || w8.GetOT(2) == "HOME";
                if (isHOMEGift) return;
            }

            if (APILegality.IsPIDIVSet(pk, enc) && !(enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND) && !(enc is EncounterEgg && GameVersion.BDSP.Contains(enc.Version)))
                return;

            if (enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND)
            {
                if (APILegality.UseXOROSHIRO && !pk.IsShiny)
                    return;
            }

            var height = 0x12;
            var weight = 0x97;
            if (signed)
            {
                if (GameVersion.SWSH.Contains(pk.Version) || GameVersion.BDSP.Contains(pk.Version))
                {
                    var top = (int)(pk.PID >> 16);
                    var bottom = (int)(pk.PID & 0xFFFF);
                    height = (top % 0x80) + (bottom % 0x81);
                    weight = ((int)(pk.EncryptionConstant >> 16) % 0x80) + ((int)(pk.EncryptionConstant & 0xFFFF) % 0x81);
                }
                else if (pk.GG)
                {
                    height = (int)(pk.PID >> 16) % 0xFF;
                    weight = (int)(pk.PID & 0xFFFF) % 0xFF;
                }
            }
            else
            {
                height = Util.Rand.Next(255);
                weight = Util.Rand.Next(255);
            }
            size.HeightScalar = (byte)height;
            size.WeightScalar = (byte)weight;
        }

        public static void ClearHyperTraining(this PKM pk)
        {
            if (pk is IHyperTrain h)
                h.HyperTrainClear();
        }

        public static string? GetBatchValue(this IBattleTemplate set, string key)
        {
            var batchexists = set is RegenTemplate rt && rt.Regen.HasBatchSettings;
            if (!batchexists)
                return null;
            rt = (RegenTemplate)set;
            foreach (var instruction in rt.Regen.Batch.Instructions)
            {
                if (instruction.PropertyName != key)
                    continue;
                return instruction.PropertyValue;
            }
            return null;
        }

        public static void SetFriendship(this PKM pk, IEncounterable enc)
        {
            bool neverOT = !HistoryVerifier.GetCanOTHandle(enc, pk, enc.Generation);
            if (enc.Generation <= 2)
                pk.OT_Friendship = GetBaseFriendship(7, pk.Species, pk.Form);  // VC transfers use SM personal info
            else if (neverOT)
                pk.OT_Friendship = GetBaseFriendship(enc, enc.Generation);
            else pk.CurrentFriendship = pk.HasMove(218) ? 0 : 255;
        }

        public static void SetBelugaValues(this PKM pk)
        {
            if (pk is PB7 pb7)
                pb7.ResetCalculatedValues();
        }

        public static void SetAwakenedValues(this PKM pk, IBattleTemplate set, bool isGO)
        {
            if (pk is not IAwakened pb7)
                return;
            Span<byte> result = stackalloc byte[6];
            AwakeningUtil.GetExpectedMinimumAVs(result, (PB7)pb7);
            var EVs = set.EVs;
            if (isGO)
                EVs = set.EVs.Select(z => z < 2 ? 2 : z).ToArray();
            pb7.AV_HP  = Math.Max(result[0], (byte)EVs[0]);
            pb7.AV_ATK = Math.Max(result[1], (byte)EVs[1]);
            pb7.AV_DEF = Math.Max(result[2], (byte)EVs[2]);
            pb7.AV_SPA = Math.Max(result[4], (byte)EVs[4]);
            pb7.AV_SPD = Math.Max(result[5], (byte)EVs[5]);
            pb7.AV_SPE = Math.Max(result[3], (byte)EVs[3]);
        }

        public static void SetHTLanguage(this PKM pk)
        {
            if (pk is IHandlerLanguage pkm)
                pkm.HT_Language = 1;
        }

        public static void SetGigantamaxFactor(this PKM pk, IBattleTemplate set, IEncounterable enc)
        {
            if (pk is not IGigantamax gmax || gmax.CanGigantamax == set.CanGigantamax)
                return;

            if (gmax.CanToggleGigantamax(pk.Species, pk.Form, enc.Species, enc.Form))
                gmax.CanGigantamax = set.CanGigantamax; // soup hax
        }

        public static void RestoreIVs(this PKM pk, int[] IVs)
        {
            pk.IVs = IVs;
            pk.ClearHyperTraining();
        }

        public static void HyperTrain(this PKM pk, int[]? IVs = null)
        {
            if (pk is not IHyperTrain t || pk.CurrentLevel != 100)
                return;

            IVs ??= pk.IVs;
            t.HT_HP  = pk.IV_HP  != 31;
            t.HT_ATK = pk.IV_ATK != 31 && IVs[1] > 2;
            t.HT_DEF = pk.IV_DEF != 31;
            t.HT_SPA = pk.IV_SPA != 31 && IVs[4] > 2;
            t.HT_SPD = pk.IV_SPD != 31;
            t.HT_SPE = pk.IV_SPE != 31 && IVs[3] > 2;

            if (pk is PB7 pb)
                pb.ResetCP();
        }

        public static void SetSuggestedMemories(this PKM pk)
        {
            switch (pk)
            {
                case PK8 pk8 when !pk.IsUntraded:
                    pk8.SetTradeMemoryHT8();
                    break;
                case PK7 pk7 when !pk.IsUntraded:
                    pk7.SetTradeMemoryHT6(true);
                    break;
                case PK6 pk6 when !pk.IsUntraded:
                    pk6.SetTradeMemoryHT6(true);
                    break;
            }
        }

        private static int GetBaseFriendship(IEncounterTemplate enc, int generation) => enc switch
        {
            IFixedOTFriendship f => f.OT_Friendship,
            { Version: GameVersion.BD or GameVersion.SP } => PersonalTable.SWSH.GetFormEntry(enc.Species, enc.Form).BaseFriendship,
            _ => GetBaseFriendship(generation, enc.Species, enc.Form),
        };

        private static int GetBaseFriendship(int gen, int species, int form)
        {
            return gen switch
            {
                1 => PersonalTable.USUM[species].BaseFriendship,
                2 => PersonalTable.USUM[species].BaseFriendship,

                6 => PersonalTable.AO[species].BaseFriendship,
                7 => PersonalTable.USUM[species].BaseFriendship,
                8 => PersonalTable.SWSH.GetFormEntry(species, form).BaseFriendship,
                _ => throw new IndexOutOfRangeException(),
            };
        }

        /// <summary>
        /// Set TID, SID and OT
        /// </summary>
        /// <param name="pk">PKM to set trainer data to</param>
        /// <param name="trainer">Trainer data</param>
        public static void SetTrainerData(this PKM pk, ITrainerInfo trainer)
        {
            pk.TID = trainer.TID;
            pk.SID = pk.Generation >= 3 ? trainer.SID : 0;
            pk.OT_Name = trainer.OT;
        }

        /// <summary>
        /// Set Handling Trainer data for a given PKM
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="trainer">Trainer to handle the <see cref="pk"/></param>
        public static void SetHandlerandMemory(this PKM pk, ITrainerInfo trainer, IEncounterable enc)
        {
            if (IsUntradeableEncounter(enc))
                return;
            pk.CurrentHandler = 1;
            pk.HT_Name = trainer.OT;
            pk.HT_Gender = trainer.Gender;
            pk.SetHTLanguage();
            pk.SetSuggestedMemories();
        }

        /// <summary>
        /// Set trainer data for a legal PKM
        /// </summary>
        /// <param name="pk">Legal PKM for setting the data</param>
        /// <param name="trainer"></param>
        /// <returns>PKM with the necessary values modified to reflect trainerdata changes</returns>
        public static void SetAllTrainerData(this PKM pk, ITrainerInfo trainer)
        {
            pk.SetBelugaValues(); // trainer details changed?

            if (pk is not IGeoTrack gt)
                return;

            if (trainer is IRegionOrigin o)
            {
                gt.ConsoleRegion = o.ConsoleRegion;
                gt.Country = o.Country;
                gt.Region = o.Region;
                if (pk is PK7 pk7 && pk.Generation <= 2)
                    pk7.FixVCRegion();
                if (pk.Species is (int)Vivillon or (int)Spewpa or (int)Scatterbug)
                    pk.FixVivillonRegion();
                return;
            }

            gt.ConsoleRegion = 1; // North America
            gt.Country = 49; // USA
            gt.Region = 7; // California
        }

        /// <summary>
        /// Sets a moveset which is suggested based on calculated legality.
        /// </summary>
        /// <param name="pk">Legal PKM for setting the data</param>
        /// <param name="random">True for Random assortment of legal moves, false if current moves only.</param>
        public static void SetSuggestedMoves(this PKM pk, bool random = false)
        {
            int[] m = pk.GetMoveSet(random);
            if (m.All(z => z == 0))
                return;

            if (pk.Moves.SequenceEqual(m))
                return;

            pk.SetMoves(m);
        }

        /// <summary>
        /// Set Dates for datelocked pokemon
        /// </summary>
        /// <param name="pk">pokemon file to modify</param>
        /// <param name="enc">encounter used to generate pokemon file</param>
        public static void SetDateLocks(this PKM pk, IEncounterable enc)
        {
            if (enc is WC8 { IsHOMEGift: true } wc8)
                SetDateLocksWC8(pk, wc8);
        }

        private static void SetDateLocksWC8(PKM pk, WC8 w)
        {
            var locked = EncounterServerDate.WC8Gifts.TryGetValue(w.CardID, out var time);
            if (locked)
                pk.MetDate = time;
        }

        public static bool TryApplyHardcodedSeedWild8(PK8 pk, IEncounterable enc, int[] ivs, Shiny requestedShiny)
        {
            // Don't bother if there is no overworld correlation
            if (enc is not IOverworldCorrelation8 eo)
                return false;

            // Check if a seed exists
            var flawless = Overworld8Search.GetFlawlessIVCount(enc, ivs, out var seed);

            // Ensure requested criteria matches
            if (flawless == -1)
                return false;
            APILegality.FindWildPIDIV8(pk, requestedShiny, flawless, seed);
            if (!eo.IsOverworldCorrelationCorrect(pk))
                return false;

            return requestedShiny switch
            {
                Shiny.AlwaysStar when pk.ShinyXor is 0 or > 15 => false,
                Shiny.Never when pk.ShinyXor < 16 => false,
                _ => true,
            };
        }

        public static bool ExistsInGame(this GameVersion destVer, int species, int form)
        {
            // Don't process if Game is LGPE and requested PKM is not Kanto / Meltan / Melmetal
            // Don't process if Game is SWSH and requested PKM is not from the Galar Dex (Zukan8.DexLookup)
            if (GameVersion.GG.Contains(destVer))
                return species is <= 151 or 808 or 809;
            if (GameVersion.SWSH.Contains(destVer))
                return PersonalTable.SWSH.IsPresentInGame(species, form);
            if (GameVersion.PLA.Contains(destVer))
                return PersonalTable.LA.IsPresentInGame(species, form);
            return (uint)species <= destVer.GetMaxSpeciesID();
        }

        public static GameVersion GetIsland(this GameVersion ver)
        {
            return ver switch
            {
                GameVersion.BD or GameVersion.SP => GameVersion.BDSP,
                GameVersion.SW or GameVersion.SH => GameVersion.SWSH,
                GameVersion.GP or GameVersion.GE => GameVersion.GG,
                GameVersion.SN or GameVersion.MN => GameVersion.SM,
                GameVersion.US or GameVersion.UM => GameVersion.USUM,
                GameVersion.X  or GameVersion.Y  => GameVersion.XY,
                GameVersion.OR or GameVersion.AS => GameVersion.ORAS,
                GameVersion.B  or GameVersion.W  => GameVersion.BW,
                GameVersion.B2 or GameVersion.W2 => GameVersion.B2W2,
                GameVersion.HG or GameVersion.SS => GameVersion.HGSS,
                GameVersion.FR or GameVersion.LG => GameVersion.FRLG,
                GameVersion.D  or GameVersion.P  or GameVersion.Pt => GameVersion.DPPt,
                GameVersion.R  or GameVersion.S  or GameVersion.E  => GameVersion.RSE,
                GameVersion.GD or GameVersion.SI or GameVersion.C  => GameVersion.GSC,
                GameVersion.RD or GameVersion.BU or GameVersion.YW or GameVersion.GN => GameVersion.Gen1,
                _ => ver
            };
        }

        public static bool IsUntradeableEncounter(IEncounterTemplate enc) => enc switch
        {
            EncounterStatic7b { Location: 28 } => true, // LGP/E Starter
            _ => false,
        };

        public static void SetRecordFlags(this PKM pk, int[] moves)
        {
            if (pk is ITechRecord8 tr and not PA8)
            {
                if (moves.Length != 0)
                    tr.SetRecordFlags(moves);
                else
                    tr.SetRecordFlags();
                return;
            }

            if (pk is IMoveShop8Mastery master)
                MoveShopRecordApplicator.SetMoveShopFlags(master, pk);
        }

        public static void SetSuggestedContestStats(this PKM pk, IEncounterable enc)
        {
            var la = new LegalityAnalysis(pk);
            pk.SetSuggestedContestStats(enc, la.Info.EvoChainsAllGens);
        }

        private static readonly ushort[] Arceus_PlateIDs = { 303, 306, 304, 305, 309, 308, 310, 313, 298, 299, 301, 300, 307, 302, 311, 312, 644 };
        public static int? GetArceusHeldItemFromForm(int form) => form is >= 1 and <= 17 ? Arceus_PlateIDs[form - 1] : null;
        public static int? GetSilvallyHeldItemFromForm(int form) => form == 0 ? null : form + 903;
        public static int? GetGenesectHeldItemFromForm(int form) => form == 0 ? null : form + 115;
    }
}
