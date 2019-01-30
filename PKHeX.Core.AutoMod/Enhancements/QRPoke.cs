using System;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public class QRPoke
    {
        private readonly uint Key;
        private readonly byte HyperTrainingFlags;
        private readonly byte field_5;
        private readonly byte field_6;
        private readonly byte field_7;
        private readonly byte[] PPUps;
        private readonly uint IvFlags;
        private readonly uint field_10;
        private readonly ushort MonsNo;
        private readonly ushort HoldItem;
        private readonly ushort[] Moves;
        private readonly byte field_20;
        private readonly byte AbilityFlags;
        private readonly byte Nature;
        private readonly byte EncounterFlags;
        private readonly byte EffortHp;
        private readonly byte EffortAtk;
        private readonly byte EffortDef;
        private readonly byte EffortSpeed;
        private readonly byte EffortSpAtk;
        private readonly byte EffortSpDef;
        private readonly byte field_2A;
        private readonly byte Familiarity;
        private readonly byte Pokeball;
        private readonly byte Level;
        private readonly byte CassetteVersion;
        private readonly byte LangId;
        private readonly byte[] rawData;
        private readonly string gender;
        private readonly int formID;
        private readonly int[] IVs;

        public QRPoke(byte[] pkmData)
        {
            Console.WriteLine(pkmData.Length);
            try
            {
                //split the data down into the correct sections.
                rawData = pkmData.Skip(0).ToArray();
                Key = BitConverter.ToUInt32(pkmData, 0);
                HyperTrainingFlags = pkmData[4];
                field_5 = pkmData[5];
                field_6 = pkmData[6];
                field_7 = pkmData[7];
                PPUps = pkmData.Skip(8).Take(4).ToArray();
                IvFlags = BitConverter.ToUInt32(pkmData, 12);
                field_10 = BitConverter.ToUInt32(pkmData, 16);
                MonsNo = BitConverter.ToUInt16(pkmData, 20);
                //Console.WriteLine(MonsNo);

                HoldItem = BitConverter.ToUInt16(pkmData, 22);

                Moves = new ushort[4];
                Moves[0] = BitConverter.ToUInt16(pkmData, 24);
                Moves[1] = BitConverter.ToUInt16(pkmData, 26);
                Moves[2] = BitConverter.ToUInt16(pkmData, 28);
                Moves[3] = BitConverter.ToUInt16(pkmData, 30);

                field_20 = pkmData[32];
                AbilityFlags = pkmData[33];
                Nature = pkmData[34];

                EncounterFlags = pkmData[35];
                //From Project Pokemon.
                //Stored as: Bit 0: Fateful Encounter, Bit 1: Female, Bit 2: Genderless, Bits 3-7: Form Data.

                gender = ((EncounterFlags & 0x2) != 0) ? "(F) " : ((EncounterFlags & 0x4) != 0) ? "" : "(M) ";
                formID = EncounterFlags >> 3;

                EffortHp = pkmData[36];
                EffortAtk = pkmData[37];
                EffortDef = pkmData[38];
                EffortSpeed = pkmData[39];
                EffortSpAtk = pkmData[40];
                EffortSpDef = pkmData[41];
                field_2A = pkmData[42];
                Familiarity = pkmData[43];

                Pokeball = pkmData[44];
                Level = pkmData[45];
                CassetteVersion = pkmData[46];
                LangId = pkmData[47];

                Console.WriteLine("Familiarity: " + formID);

                int[] IVTemp = new int[6];

                for (int i = 0; i < 6; i++)
                {
                    IVTemp[i] = ((int)IvFlags >> (i * 5)) & 0x1F;
                }

                IVs = new int[6]{
                    IVTemp[0],
                    IVTemp[1],
                    IVTemp[2],
                    IVTemp[4],
                    IVTemp[5],
                    IVTemp[3]
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public string GetHiddenPowerTypeName()
        {
            switch (HiddenPower.GetType(IVs))
            {
                case 0: return "Fighting";
                case 1: return "Flying";
                case 2: return "Poison";
                case 3: return "Ground";
                case 4: return "Rock";
                case 5: return "Bug";
                case 6: return "Ghost";
                case 7: return "Steel";
                case 8: return "Fire";
                case 9: return "Water";
                case 10: return "Grass";
                case 11: return "Electric";
                case 12: return "Psychic";
                case 13: return "Ice";
                case 14: return "Dragon";
                case 15: return "Dark";
                default:
                    return string.Empty;
            }
        }

        public string ToShowdownFormat(bool HT)
        {
            /* TODO:
             *  - Add the ability to remove the IVs.
             *  - Add the Hidden Power type.
             * */

            string HTFlags = Convert.ToString(HyperTrainingFlags, 2);
            string[] IVString = new string[6];
            for (int i = 0; i < 30 - HTFlags.Length; i++)
            {
                HTFlags = "0" + HTFlags;
            }

            char[] chars = HTFlags.Reverse().ToArray();

            for (int i = 0; i < 6; i++)
            {
                if (chars[i] == '1')
                {
                    if (HT)
                        IVString[i] = IVs[i] + "(HT)";
                    else
                        IVString[i] = "31";
                }
                else
                {
                    IVString[i] = IVs[i].ToString();
                }
            }

            string[] result =
            {
                $"{DataFetch.GetSpecies(MonsNo, formID)} @ {DataFetch.GetItem(HoldItem)}",
                $"Ability: {DataFetch.GetAbility(MonsNo, formID, AbilityFlags)}",
                $"Level: {Level}",
                "Happiness: 0",
                $"EVs: {EffortHp} HP / {EffortAtk} Atk / {EffortDef} Def / {EffortSpAtk} SpA / {EffortSpDef} SpD / {EffortSpeed} Spe",
                DataFetch.GetNature(Nature) + " Nature",
                $"IVs: {IVString[0]} HP / {IVString[1]} Atk / {IVString[2]} Def / {IVString[3]} SpA / {IVString[4]} SpD / {IVString[5]} Spe ",
                $" - {DataFetch.GetMove(Moves[0], this)}",
                $" - {DataFetch.GetMove(Moves[1], this)}",
                $" - {DataFetch.GetMove(Moves[2], this)}",
                $" - {DataFetch.GetMove(Moves[3], this)}"
            };

            return string.Join("\n", result);
        }

        public string GetStatsData()
        {
            string HTFlags = Convert.ToString(HyperTrainingFlags, 2);
            for (int i = 0; i < 30 - HTFlags.Length; i++)
            {
                HTFlags = "0" + HTFlags;
            }

            char[] chars = HTFlags.Reverse().ToArray();
            string HT = "HT: ";

            for (int i = 0; i < 6; i++)
            {
                if (i != 0)
                {
                    HT += "/";
                }
                if (chars[i] == '1')
                {
                    HT += "HT";
                }
                else
                {
                    HT += "X";
                }
            }

            string[] format =
            {
                $"Item: {DataFetch.GetItem(HoldItem)}",
                HT,
                $"EVs: {EffortHp}H {EffortAtk}A {EffortDef}B {EffortSpAtk}C {EffortSpDef}D {EffortSpeed}S",
                $"IVs: {IVs[0]}/{IVs[1]}/{IVs[2]}/{IVs[3]}/{IVs[4]}/{IVs[5]}"
            };

            return string.Join("\n", format);
        }

        public override string ToString()
        {
            try
            {
                return "\n" + ToShowdownFormat(true) + "\n";
            }
            catch (Exception)
            {
                return "No Pokemon in this Slot";
            }
        }
    }
}