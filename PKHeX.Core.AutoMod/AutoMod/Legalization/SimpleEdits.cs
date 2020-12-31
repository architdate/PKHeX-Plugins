using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class SimpleEdits
    {
        static SimpleEdits()
        {
            // Make PKHeX use our own marking method
            MarkingApplicator.MarkingMethod = FlagIVsAutoMod;
        }

        public static readonly int[] Zukan8Additions =
        {
            // Original extra 35
            001, 002, 003, 007, 008, 009, 150, 151, 251, 385,
            638, 639, 640, 643, 644, 646, 647, 722, 723, 724,
            725, 726, 727, 728, 729, 730, 789, 790, 791, 792,
            800, 802, 807, 808, 809,

            // DLC (Isle of Armour)
            079, 080, 891, 892, 893,

            // DLC (Crown Tundra)
            199, 894, 895, 896, 897, 898
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
                    _ => 0
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
            var isRaid = enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND or EncounterStatic8U;
            if ((pk.Species == 658 && pk.Form == 1) || isRaid) // Ash-Greninja or raids
                return;
            int gen = pk.Generation;
            if (gen is > 2 and < 6)
            {
                var ec = pk.PID;
                pk.EncryptionConstant = ec;
                if (pk.Format >= 6)
                {
                    var pidxor = ((pk.TID ^ pk.SID ^ (int)(ec & 0xFFFF) ^ (int)(ec >> 16)) & ~0x7) == 8;
                    pk.PID = pidxor ? ec ^ 0x80000000 : ec;
                }
                return;
            }
            int wIndex = WurmpleUtil.GetWurmpleEvoGroup(pk.Species);
            if (wIndex != -1)
            {
                pk.EncryptionConstant = WurmpleUtil.GetWurmpleEncryptionConstant(wIndex);
                return;
            }

            pk.EncryptionConstant = enc is WC8 { PIDType: Shiny.FixedValue, EncryptionConstant: 0 } ? 0 : Util.Rand32();
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
                if (pk.Format >= 6)
                {
                    do
                    {
                        pk.SetShiny();
                    } while (GetPIDXOR());

                    bool GetPIDXOR() =>
                        ((pk.TID ^ pk.SID ^ (int)(pk.PID & 0xFFFF) ^ (int)(pk.PID >> 16)) & ~0x7) == 8;
                }

                return;
            }

            var validg5sid = pk.SID & 1;
            pk.SetShinySID(); // no mg = no lock

            if (pk.Generation != 5) return;

            while (true)
            {
                pk.PID = PKX.GetRandomPID(Util.Rand, pk.Species, pk.Gender, pk.Version, pk.Nature, pk.Format, pk.PID);
                validg5sid = pk.SID & 1;
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

        public static void ApplyHeightWeight(this PKM pk, IEncounterable enc, bool signed = true)
        {
            if (pk.Generation < 8 && pk.Format >= 8 && !pk.GG) // height and weight don't apply prior to GG
                return;
            if (pk is not IScaledSize size)
                return;
            if (enc is WC8 w8)
            {
                var isHOMEGift = w8.Location == 30018 || w8.GetOT(2) == "HOME";
                if (isHOMEGift) return;
            }

            if (enc is EncounterStatic8N or EncounterStatic8NC or EncounterStatic8ND)
            {
                if (APILegality.UseXOROSHIRO && !pk.IsShiny)
                    return;
            }

            if (enc is EncounterStatic8U)
                return; // height and weight already handled even for shiny cases

            var height = 0x12;
            var weight = 0x97;
            if (signed)
            {
                if (GameVersion.SWSH.Contains(pk.Version))
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
            size.HeightScalar = height;
            size.WeightScalar = weight;
            if (pk is PB7 pb1)
                pb1.ResetCalculatedValues();
        }

        public static void ClearHyperTraining(this PKM pk)
        {
            if (pk is IHyperTrain h)
                h.HyperTrainClear();
        }

        public static void SetFriendship(this PKM pk, IEncounterable enc)
        {
            var gen = pk.Generation <= 2 ? 7 : pk.Generation;
            if (!HistoryVerifier.GetCanOTHandle(enc, pk, gen) || pk.Generation <= 2)
                pk.OT_Friendship = GetBaseFriendship(gen, pk.Species);
            else pk.CurrentFriendship = pk.Moves.Contains(218) ? 0 : 255;
        }

        public static void SetBelugaValues(this PKM pk)
        {
            if (pk is PB7 pb7)
                pb7.ResetCalculatedValues();
        }

        public static void SetHTLanguage(this PKM pk)
        {
            if (pk is IHandlerLanguage pkm)
                pkm.HT_Language = 1;
        }

        public static void SetDynamaxLevel(this PKM pk, byte level = 10)
        {
            if (level > 10)
                level = 10;
            if (pk is IDynamaxLevel pkm)
                pkm.DynamaxLevel = level; // Set max dynamax level
            if (pk is PK8 pk8)
            {
                if (pk8.Species is (int)Species.Zacian or (int)Species.Zamazenta or (int)Species.Eternatus) // Zacian, Zamazenta and Eternatus cannot dynamax
                    pk8.DynamaxLevel = 0;
            }
        }

        public static void RestoreIVs(this PKM pk, int[] IVs)
        {
            pk.IVs = IVs;
            pk.ClearHyperTraining();
        }

        public static bool NeedsHyperTraining(this PKM pk)
        {
            for (int i = 0; i < 6; i++)
            {
                var iv = pk.GetIV(i);
                if (iv is 31 or <= 1) // ignore IV value = 0/1 for intentional IV values (1 for hidden power cases)
                    continue;
                return true; // flawed IV present
            }
            return false;
        }

        public static void HyperTrain(this PKM pk)
        {
            if (pk is not IHyperTrain || !NeedsHyperTraining(pk))
                return;

            pk.CurrentLevel = 100; // Set level for HT before doing HT
            pk.SetSuggestedHyperTrainingData();
        }

        public static void SetSuggestedMemories(this PKM pk)
        {
            switch (pk)
            {
                case PK8 pk8 when !pk.IsUntraded:
                    pk8.HT_Memory = 4;
                    pk8.HT_TextVar = 0;
                    pk8.HT_Intensity = 1;
                    pk8.HT_Feeling = Memories.GetRandomFeeling(pk8.HT_Memory, 10);
                    break;
                case PK7 pk7 when !pk.IsUntraded:
                    pk7.SetTradeMemoryHT(true);
                    break;
                case PK6 pk6 when !pk.IsUntraded:
                    pk6.SetTradeMemoryHT(true);
                    break;
            }
        }

        private static int GetBaseFriendship(int gen, int species)
        {
            return gen switch
            {
                1 => PersonalTable.USUM[species].BaseFriendship,
                2 => PersonalTable.USUM[species].BaseFriendship,

                6 => PersonalTable.AO[species].BaseFriendship,
                7 => PersonalTable.USUM[species].BaseFriendship,
                8 => PersonalTable.SWSH[species].BaseFriendship,
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
        public static void SetHandlerandMemory(this PKM pk, ITrainerInfo trainer)
        {
            pk.CurrentHandler = 1;
            pk.HT_Name = trainer.OT;
            pk.HT_Gender = trainer.Gender;
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
        public static void SetDatelocks(this PKM pk, IEncounterable enc)
        {
            pk.SetHOMEDates(enc);
        }

        /// <summary>
        /// Sets the met date for a Pokemon HOME event (dates are serverside)
        /// </summary>
        /// <param name="pk">pokemon file to modify</param>
        /// <param name="enc">encounter used to generate pokemon file</param>
        public static void SetHOMEDates(this PKM pk, IEncounterable enc)
        {
            if (enc is not WC8 w8)
                return;
            var isHOMEGift = w8.Location == 30018 || w8.GetOT(2) == "HOME";
            if (!isHOMEGift)
                return;
            switch (w8.Species)
            {
                case (int)Species.Zeraora:
                    pk.MetDate = new DateTime(2020, 06, 30);
                    break;
            }
        }
    }
}