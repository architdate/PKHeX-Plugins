using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    public static class PKSMUtil
    {
        /// <summary>
        /// Exports PKSM's bank.bin to individual <see cref="PKM"/> files
        /// </summary>
        /// <param name="bank">PKSM format bank storage</param>
        /// <param name="dir">Folder to export all dumped files to</param>
        public static int ExportBank(byte[] bank, string dir, out List<PKMPreview> previews)
        {
            Directory.CreateDirectory(dir);
            var ctr = 0;
            previews = new List<PKMPreview>();
            for (int i = 12; i < bank.Length; i += 264)
            {
                var pk = GetPKSMStoredPKM(bank, i);
                if (pk == null)
                    continue;
                if (pk.Species == 0 && pk.Species >= pk.MaxSpeciesID)
                    continue;
                var strings = GameInfo.Strings;
                previews.Add(new PKMPreview(pk, strings));
                File.WriteAllBytes(Path.Combine(dir, Util.CleanFileName(pk.FileName)), pk.DecryptedBoxData);
                ctr++;
            }
            return ctr;
        }

        public static void ExportCSV(List<PKMPreview> pklist, string path)
        {
            List<PKMPreview> sortedprev = pklist.OrderBy(p => p.Species).ToList();
            // Todo: Complete function. POC
            var sb = new StringBuilder();
            string headers = "Nickname, Species, Nature, Gender, ESV, Hidden Power, Ability, Move 1, Move 2, Move 3, Move 4, Held Item, HP, ATK, DEF, SPA, SPD, SPE, Met Location, Egg Location, Ball, OT, Version, OT Language, Legal, Country, Region, 3DS Region, PID, EC, HP IVs, ATK IVs, DEF IVs, SPA IVs, SPD IVs, SPE IVs, EXP, Level, Markings, Handling Trainer, Met Level, Shiny, TID, SID, Friendship, Met Year, Met Month, Met Day";
            sb.AppendLine(headers);
            foreach (PKMPreview p in sortedprev)
                sb.AppendLine(string.Join(",", new string[] { p.Nickname, p.Species, p.Nature, p.Gender, p.ESV, p.HP_Type, p.Ability, p.Move1, p.Move2, p.Move3, p.Move4, p.HeldItem, p.HP, p.ATK, p.DEF, p.SPA, p.SPD, p.SPE, p.MetLoc, p.EggLoc, p.Ball, p.OT, p.Version, p.OTLang, p.Legal, p.CountryID, p.RegionID, p.DSRegionID, p.PID, p.EC, p.HP_IV.ToString(), p.ATK_IV.ToString(), p.DEF_IV.ToString(), p.SPA_IV.ToString(), p.SPD_IV.ToString(), p.SPE_IV.ToString(), p.EXP.ToString(), p.Level.ToString(), p.Markings.ToString(), p.NotOT, p.MetLevel.ToString(), p.IsShiny.ToString(), p.TID.ToString(), p.SID.ToString(), p.Friendship.ToString(), p.Met_Year.ToString(), p.Met_Month.ToString(), p.Met_Day.ToString() }));
            File.WriteAllText(Path.Combine(path, "boxdump.csv"), sb.ToString(), Encoding.UTF8);
        }

        private static PKM GetPKSMStoredPKM(byte[] data, int ofs)
        {
            // get format
            var metadata = BitConverter.ToUInt32(data, ofs);
            var format = (PKSMStorageFormat)(metadata & 0xFF);
            if (format >= PKSMStorageFormat.MAX_COUNT)
                return null;

            // gen4+ presence check; won't work for prior gens
            if (!IsPKMPresent(data, ofs + 4))
                return null;

            switch (format)
            {
                case PKSMStorageFormat.FOUR: return new PK4(Slice(data, ofs + 4, 136));
                case PKSMStorageFormat.FIVE: return new PK5(Slice(data, ofs + 4, 136));
                case PKSMStorageFormat.SIX: return new PK6(Slice(data, ofs + 4, 232));
                case PKSMStorageFormat.SEVEN: return new PK7(Slice(data, ofs + 4, 232));
                case PKSMStorageFormat.LGPE: return new PB7(Slice(data, ofs + 4, 232));
                default:
                    return null;
            }
        }

        private static byte[] Slice(byte[] data, int ofs, int len)
        {
            var arr = new byte[len];
            Array.Copy(data, ofs, arr, 0, len);
            return arr;
        }

        // copied from PKHeX.Core
        private static bool IsPKMPresent(byte[] data, int offset)
        {
            if (BitConverter.ToUInt32(data, offset) != 0) // PID
                return true;
            return 0 != BitConverter.ToUInt16(data, offset + 8);
        }

        private enum PKSMStorageFormat
        {
            FOUR,
            FIVE,
            SIX,
            SEVEN,
            LGPE,
            MAX_COUNT,
            UNUSED = 0xFF
        }
    }
}
