/// PKMN-NTR - On-the-air memory editor for 3DS Pokémon games
/// Copyright(C) 2016-2018  PKMN-NTR Dev Team
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU General Public License as published by
/// the Free Software Foundation, either version 3 of the License, or
/// (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
/// GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program.If not, see<http://www.gnu.org/licenses/>.
///

using PKHeX.Core;

namespace pkmn_ntr.Helpers
{
    /// <summary>
    /// This class contains RAM memory address, some formulas, button padding and screen
    /// positions, they're used used by the program.
    /// </summary>
    public class LookupTable
    {
        #region RAM Address

        public static ISaveFileProvider SaveFileEditor2 = WonderTradeBot.WonderTradeBot.SaveFileEditor2;

        /// <summary>
        /// Location of the NFC disable data in RAM.
        /// </summary>
        public static uint NFCOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3E14C0; // 1.0 offset was 0x3DFFD0
                    case GameVersion.US:
                        return 0x3F3424; // 1.1 offset was 0x3F341C; // 1.0 offset was 0x3F1F00
                    case GameVersion.UM:
                        return 0x3F3428; // 1.1 offset was 0x3F3420; // 1.0 offset was 0x3F1F04
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Value to write to disabe NFC.
        /// </summary>
        public static readonly uint NFCValue = 0xE3A01000;

        /// <summary>
        /// Location of Trainer Card data in RAM.
        /// </summary>
        public static uint TrainerCardOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C79C3C;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C81340;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D67D0;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33012818;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Size of Trainer Card data in RAM.
        /// </summary>
        public static uint TrainerCardSize
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x200;
                    case GameVersion.SN:
                    case GameVersion.MN:
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0xC0;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Trainer Card data in SAV.
        /// </summary>
        public static uint TrainerCardLocation
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x14000;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x01200;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x01400;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Bag Items data in RAM.
        /// </summary>
        public static uint ItemsOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C67564;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C6EC70;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D5934;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33011934;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Size of Bag Items data in RAM.
        /// </summary>
        public static uint ItemsSize
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0xC00;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0xDE0;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x1000;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Bag Items data in SAV.
        /// </summary>
        public static uint ItemsLocation
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x0400;
                    case GameVersion.SN:
                    case GameVersion.MN:
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x00;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's name data in RAM.
        /// </summary>
        public static uint TrainerNameOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C79C84;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C81388;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D6808;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33012850;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's TID data in RAM.
        /// </summary>
        public static uint TrainerTIDOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C79C3C;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C81340;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D67D0;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33012818;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's SID data in RAM.
        /// </summary>
        public static uint TrainerSIDOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C79C3E;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C81342;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D67D2;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3301281A;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's Money data in RAM.
        /// </summary>
        public static uint TrainerMoneyOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C6A6AC;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C71DC0;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D8FC0;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33015210;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's current festival coins data in RAM.
        /// </summary>
        public static uint TrainerCurrentFCOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x33124D58;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33061168;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's total festival coins data in RAM.
        /// </summary>
        public static uint TrainerTotalFCOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x33124D5C;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3306116C;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's PokéMiles data in RAM.
        /// </summary>
        public static uint TrainerPokeMilesOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C82BA0;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C8B36C;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Player's Battle Points data in RAM.
        /// </summary>
        public static uint TrainerBPOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C6A6E0;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C71DE8;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D90D8;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33015328;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Game's Language data in RAM.
        /// </summary>
        public static uint GameLanguageOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C79C69;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C8136D;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D6805;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3301284D;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Game's Played Time data in RAM.
        /// </summary>
        public static uint GamePlayTimeOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8CE2814;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8CFBD88;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x34197648;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33F8127C;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Game's Egg Seed data in RAM.
        /// </summary>
        public static uint SeedEggOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3313EDDC;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3307B1EC;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Game's Legendary Pokémon Seed data in RAM.
        /// </summary>
        public static uint SeedLegendaryOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x325A3878;      //1.1: 0x325A3838;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x326601C4;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Search pattern for opponent data in RAM.
        /// </summary>
        public static byte[] OpponentPatern
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return new byte[] { 0x60, 0x75, 0xC6, 0x08, 0xDC, 0xA8, 0xC7,
                            0x08, 0xD0, 0xB6, 0xC7, 0x08 };
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return new byte[] { 0x60, 0xE7, 0xC6, 0x08, 0x6C, 0xEC, 0xC6,
                            0x08, 0xE0, 0x1F, 0xC8, 0x08, 0x00, 0x39, 0xC8, 0x08 };
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Offset to find opponent data in RAM.
        /// </summary>
        public static uint OpponentOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 637;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 673;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Pokémon Box data in RAM.
        /// </summary>
        public static uint BoxOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C861C8;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C9E134;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D9838;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33015AB0;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of the Current Active Box data in RAM 
        /// </summary>
        public static uint CurrentboxOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x330D982F; // 1.0: 0x520000;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33015AA7;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Day Care/Nursery Pokémon data in RAM (Slot 1).
        /// </summary>
        public static uint DayCare1Offset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C7FF4C;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C88180;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3313EC01;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3307B011;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Day Care/Nursery Pokémon data in RAM (Slot 2).
        /// </summary>
        public static uint DayCare2Offset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C8003C;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C88270;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3313ECEA;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3307B0FA;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Battle Resort Day Care Pokémon data in RAM (Slot 1).
        /// </summary>
        public static uint DayCare3Offset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C88370;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Battle Resort Day Care Pokémon data in RAM (Slot 2).
        /// </summary>
        public static uint DayCare4Offset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C88460;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Battle Box Pokémon data in RAM.
        /// </summary>
        public static uint BattleBoxOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8C6AC2C;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8C72330;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Trade Pokémon data in RAM.
        /// </summary>
        public static uint TradeOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8500000;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8520000;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x32A870C8;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x30000660; // Needs testing
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Wild Pokémon data in RAM.
        /// </summary>
        public static uint WildOffset1
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8800000;
                    case GameVersion.SN:
                    case GameVersion.MN:
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3254F4AC;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Wild Pokémon data in RAM (Slot 2 in Double Battle).
        /// </summary>
        public static uint WildOffset2
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x32663BF0;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Wild Pokémon data in RAM (Last called helper in SOS battle).
        /// </summary>
        public static uint WildOffset3
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3003969C;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x30039888;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Wild Pokémon data in RAM (Previous helpers in SOS battle).
        /// </summary>
        public static uint WildOffset4
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x3002F7B8;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x3002F9A0; // Can also be 0x30030544
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Location of Party Trade data in RAM.
        /// </summary>
        public static uint PartyOffset
        {
            get
            {
                switch (SaveFileEditor2.SAV.Version)
                {
                    case GameVersion.X:
                    case GameVersion.Y:
                        return 0x8CE1CF8;
                    case GameVersion.OR:
                    case GameVersion.AS:
                        return 0x8CFB26C;
                    case GameVersion.SN:
                    case GameVersion.MN:
                        return 0x34195E10;
                    case GameVersion.US:
                    case GameVersion.UM:
                        return 0x33F7FA44; // Could also be 0x330128E4
                    default:
                        return 0;
                }
            }
        }

        #endregion RAM Address

        #region Formulas

        /// <summary>
        /// Returns the Trainer Shiny Value based on the player's TID and SID numbers.
        /// </summary>
        /// <param name="TID">Trainer ID number.</param>
        /// <param name="SID">Trainer Secret ID number.</param>
        /// <returns></returns>
        public static int GetTSV(ushort TID, ushort SID)
        {
            return (TID ^ SID) >> 4;
        }

        /// <summary>
        /// Returns the Trainer ID number used in Generation 7 games based on the
        /// player's TID and SID numbers.
        /// </summary>
        /// <param name="TID">Trainer ID number.</param>
        /// <param name="SID">Trainer Secret ID number.</param>
        /// <returns></returns>
        public static int GetG7ID(ushort TID, ushort SID)
        {
            return (int)((uint)(TID | (SID << 16)) % 1000000);
        }

        /// <summary>
        /// Returns the number of box spaces starting on a specific slot until the
        /// last box.
        /// </summary>
        /// <param name="box">Box number.</param>
        /// <param name="slot">Slot number.</param>
        /// <returns></returns>
        public static int GetRemainingSpaces(int box, int slot)
        {
            int result = 0;
            result += (31 - slot);
            switch (SaveFileEditor2.SAV.Generation)
            {
                case 6:
                    result += (31 - box) * 30;
                    break;
                case 7:
                    result += (32 - box) * 30;
                    break;
            }
            return result;
        }

        #endregion Formulas

        #region Buttons

        // Buton Padding values, to press several of them at once, you need to AND the
        // values.

        public const uint ButtonA = 0xFFE;
        public const uint ButtonB = 0xFFD;
        public const uint ButtonX = 0xBFF;
        public const uint ButtonY = 0x7FF;
        public const uint ButtonR = 0xEFF;
        public const uint ButtonL = 0xDFF;
        public const uint ButtonStart = 0xFF7;
        public const uint ButtonSelect = 0xFFB;
        public const uint DPadUp = 0xFBF;
        public const uint DPadDown = 0xF7F;
        public const uint DPadLeft = 0xFDF;
        public const uint DPadRight = 0xFEF;

        /// <summary>
        /// Press B and D-pad Up buttons at the same time.
        /// </summary>
        public const uint RunUp = 0xFBD;

        /// <summary>
        /// Press B and D-pad Down buttons at the same time.
        /// </summary>
        public const uint RunDown = 0xF7D;

        /// <summary>
        /// Press B and D-pad Left buttons at the same time.
        /// </summary>
        public const uint RunLeft = 0xFDD;

        /// <summary>
        /// Press B and D-pad Right buttons at the same time.
        /// </summary>
        public const uint RunRight = 0xFED;

        /// <summary>
        /// Soft-reset button combination. Press L, R and Start buttons at the same time.
        /// </summary>
        public const uint softReset = 0xCF7;

        /// <summary>
        /// No buttons pressed value.
        /// </summary>
        public const uint NoButtons = 0xFFF;

        /// <summary>
        /// No touch screen pressed value.
        /// </summary>
        public const uint NoTouch = 0x02000000;

        /// <summary>
        /// Control Stick released value.
        /// </summary>
        public const uint NoStick = 0x00800800;

        #endregion Buttons

        #region Box Position

        // Gen 6 box and pokémon positions in the PC
        public static uint[] pokeposX6 = { 30, 60, 90, 120, 150, 180, 30, 60, 90, 120,
            150, 180, 30, 60, 90, 120, 150, 180, 30, 60, 90, 120, 150, 180, 30, 60, 90,
            120, 150, 180 };
        public static uint[] pokeposY6 = { 60, 60, 60, 60, 60, 60, 90, 90, 90, 90, 90,
            90, 120, 120, 120, 120, 120, 120, 150, 150, 150, 150, 150, 150, 180, 180,
            180, 180, 180, 180 };
        public static uint[] boxposX6 = { 20, 60, 100, 140, 180, 220, 260, 300, 20, 60,
            100, 140, 180, 220, 260, 300, 20, 60, 100, 140, 180, 220, 260, 300, 20, 60,
            100, 140, 180, 220, 260 };
        public static uint[] boxposY6 = { 24, 24, 24, 24, 24, 24, 24, 24, 72, 72, 72,
            72, 72, 72, 72, 72, 120, 120, 120, 120, 120, 120, 120, 120, 168, 168, 168,
            168, 168, 168, 168 };

        // Gen 7 box and pokémon positions in the PC
        public static uint[] pokeposX7 = { 30, 60, 90, 120, 150, 180, 30, 60, 90, 120,
            150, 180, 30, 60, 90, 120, 150, 180, 30, 60, 90, 120, 150, 180, 30, 60, 90,
            120, 150, 180 };
        public static uint[] pokeposY7 = { 70, 70, 70, 70, 70, 70, 100, 100, 100, 100,
            100, 100, 130, 130, 130, 130, 130, 130, 160, 160, 160, 160, 160, 160, 190,
            190, 190, 190, 190, 190 };
        public static uint[] boxposX7 = { 33, 69, 105, 141, 177, 213, 249, 285, 33, 69,
            105, 141, 177, 213, 249, 285, 33, 69, 105, 141, 177, 213, 249, 285, 33, 69,
            105, 141, 177, 213, 249, 285 };
        public static uint[] boxposY7 = { 36, 36, 36, 36, 36, 36, 36, 36, 84, 84, 84,
            84, 84, 84, 84, 84, 132, 132, 132, 132, 132, 132, 132, 132, 180, 180, 180,
            180, 180, 180, 180, 180 };

        #endregion Box Position
    }

}
