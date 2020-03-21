using System;
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
            079
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
        /// Set Encryption Constant based on PKM GenNumber
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="enc">Encounter details</param>
        public static void SetEncryptionConstant(this PKM pk, IEncounterable enc)
        {
            if ((pk.Species == 658 && pk.AltForm == 1) || enc is EncounterStatic8N) // Ash-Greninja
                return;
            int gen = pk.GenNumber;
            if (2 < gen && gen < 6)
            {
                var ec = pk.PID;
                pk.EncryptionConstant = ec;
                if (pk.Format >= 6)
                {
                    var pidxor = ((pk.TID ^ pk.SID ^ (int) (ec & 0xFFFF) ^ (int) (ec >> 16)) & ~0x7) == 8;
                    pk.PID = pidxor ? ec ^ 0x80000000 : ec;
                }
                return;
            }
            int wIndex = WurmpleUtil.GetWurmpleEvoGroup(pk.Species);
            if (wIndex != -1)
            {
                pk.EncryptionConstant = WurmpleUtil.GetWurmpleEC(wIndex);
                return;
            }
            pk.EncryptionConstant = Util.Rand32();
        }

        /// <summary>
        /// Sets shiny value to whatever boolean is specified
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="isShiny">Shiny value that needs to be set</param>
        /// <param name="enc">Encounter details</param>
        public static void SetShinyBoolean(this PKM pk, bool isShiny, IEncounterable enc)
        {
            if (pk.IsShiny == isShiny)
                return; // don't mess with stuff if pk is already shiny
            if (!isShiny)
            {
                pk.SetUnshiny();
            }
            else
            {
                if (enc is EncounterStatic8N || enc is EncounterStatic8NC || enc is EncounterStatic8ND)
                {
                    // have to verify all since 8NC and 8ND do not inherit 8N
                    pk.SetRaidShiny();
                }
                else if (8 > pk.GenNumber && pk.GenNumber > 5)
                {
                    // Set Shiny SID for gen 8 until raid shiny locks are documented
                    pk.SetShiny();
                }
                else if (pk.VC)
                {
                    pk.SetIsShiny(true);
                }
                else
                {
                    if (enc is MysteryGift mg)
                    {
                        if (mg.IsEgg || (mg is PGT g && g.IsManaphyEgg))
                            pk.SetShinySID(); // not SID locked
                        else
                        {
                            pk.SetShiny();
                            if (pk.Format >= 6)
                            {
                                do
                                {
                                    pk.SetShiny();
                                } while (GetPIDXOR());
                                bool GetPIDXOR() => ((pk.TID ^ pk.SID ^ (int)(pk.PID & 0xFFFF) ^ (int)(pk.PID >> 16)) & ~0x7) == 8;
                            }
                        } // if SID cant be changed, PID has to be able to change if shiny is possible.
                    }
                    else
                    {
                        pk.SetShinySID(); // no mg = no lock
                    }
                }
            }
        }

        public static void SetRaidShiny(this PKM pk)
        {
            if (pk.IsShiny)
                return;

            while (true)
            {
                pk.SetShiny();
                if (pk.Format <= 7)
                    return;
                var xor = pk.ShinyXor;
                if (xor < 2) // allow xor1 and xor0 for den shinies
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

        public static void ClearHyperTraining(this PKM pk)
        {
            if (pk is IHyperTrain h)
                h.HyperTrainClear();
        }

        public static void SetHappiness(this PKM pk, IEncounterable enc)
        {
            var gen = pk.GenNumber <= 2 ? 7 : pk.GenNumber;
            if (!HistoryVerifier.GetCanOTHandle(enc, pk, gen) || pk.GenNumber <= 2)
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
                if (pk8.Species >= (int)Species.Zacian) // Zacian, Zamazenta and Eternatus cannot dynamax
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
                if (iv == 31 || iv <= 1) // ignore IV value = 0/1 for intentional IV values (1 for hidden power cases)
                    continue;
                return true; // flawed IV present
            }
            return false;
        }

        public static void HyperTrain(this PKM pk)
        {
            if (!(pk is IHyperTrain) || !NeedsHyperTraining(pk))
                return;

            pk.CurrentLevel = 100; // Set level for HT before doing HT
            pk.SetSuggestedHyperTrainingData();
        }

        public static void ClearHyperTrainedPerfectIVs(this PKM pk)
        {
            if (!(pk is IHyperTrain h))
                return;
            if (pk.IV_HP == 31)
                h.HT_HP = false;
            if (pk.IV_ATK == 31)
                h.HT_ATK = false;
            if (pk.IV_DEF == 31)
                h.HT_DEF = false;
            if (pk.IV_SPA == 31)
                h.HT_SPA = false;
            if (pk.IV_SPD == 31)
                h.HT_SPD = false;
            if (pk.IV_SPE == 31)
                h.HT_SPE = false;
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
                    pk7.TradeMemory(true);
                    break;
                case PK6 pk6 when !pk.IsUntraded:
                    pk6.TradeMemory(true);
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

        public static void ClearOTMemory(this PKM pk)
        {
            pk.OT_Memory = 0;
            pk.OT_TextVar = 0;
            pk.OT_Intensity = 0;
            pk.OT_Feeling = 0;
        }

        /// <summary>
        /// Set TID, SID and OT
        /// </summary>
        /// <param name="pk">PKM to set trainer data to</param>
        /// <param name="trainer">Trainer data</param>
        public static void SetTrainerData(this PKM pk, ITrainerInfo trainer)
        {
            pk.TID = trainer.TID;
            pk.SID = pk.GenNumber >= 3 ? trainer.SID : 0;
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

            int gen = trainer.Generation;
            if (gen == 6 || (gen == 7 && !GameVersion.GG.Contains((GameVersion)trainer.Game)))
            {
                pk.ConsoleRegion = trainer.ConsoleRegion;
                pk.Country = trainer.Country;
                pk.Region = trainer.SubRegion;
            }
        }

        /// <summary>
        /// Sets a moveset which is suggested based on calculated legality.
        /// </summary>
        /// <param name="pk">Legal PKM for setting the data</param>
        /// <param name="random">True for Random assortment of legal moves, false if current moves only.</param>
        public static void SetSuggestedMoves(this PKM pk, bool random = false)
        {
            int[] m = pk.GetMoveSet(random);
            if (m?.Any(z => z != 0) != true)
                return;

            if (pk.Moves.SequenceEqual(m))
                return;

            pk.SetMoves(m);
        }
    }
}