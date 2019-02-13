using System;
using System.IO;

namespace PKHeX.Core.AutoMod
{
    public static class PKSMUtil
    {
        /// <summary>
        /// Exports PKSM's bank.bin to individual <see cref="PKM"/> files
        /// </summary>
        /// <param name="bank">PKSM format bank storage</param>
        /// <param name="dir">Folder to export all dumped files to</param>
        public static int ExportBank(byte[] bank, string dir)
        {
            Directory.CreateDirectory(dir);
            var ctr = 0;
            for (int i = 12; i < bank.Length; i += 264)
            {
                var pk = GetPKSMStoredPKM(bank, i);
                if (pk == null)
                    continue;
                if (pk.Species == 0 && pk.Species >= pk.MaxSpeciesID)
                    continue;
                File.WriteAllBytes(Path.Combine(dir, Util.CleanFileName(pk.FileName)), pk.DecryptedBoxData);
                ctr++;
            }
            return ctr;
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
            Array.Copy(data, ofs, arr, len, 0);
            return arr;
        }


        // copied from pkhex.core
        internal static bool IsPKMPresent(byte[] data, int offset)
        {
            if (BitConverter.ToUInt32(data, offset) != 0) // PID
                return true;
            ushort species = BitConverter.ToUInt16(data, offset + 8);
            return species != 0;
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
