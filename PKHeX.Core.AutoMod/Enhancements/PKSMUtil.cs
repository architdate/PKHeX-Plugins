using System;
using System.Collections.Generic;
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
        /// <param name="previews">Preview data</param>
        public static int ExportBank(byte[] bank, string dir, out List<PKMPreview> previews)
        {
            Directory.CreateDirectory(dir);
            var ctr = 0;
            previews = new List<PKMPreview>();
            for (int i = 12; i < bank.Length; i += 4 + 260)
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

        private static PKM? GetPKSMStoredPKM(byte[] data, int ofs)
        {
            // get format
            var metadata = BitConverter.ToUInt32(data, ofs);
            var format = (PKSMStorageFormat)(metadata & 0xFF);
            if (format >= PKSMStorageFormat.MAX_COUNT)
                return null;

            // gen4+ presence check; won't work for prior gens
            if (!IsPKMPresent(data, ofs + 4))
                return null;

            return format switch
            {
                PKSMStorageFormat.FOUR => (PKM)new PK4(Slice(data, ofs + 4, 136)),
                PKSMStorageFormat.FIVE => new PK5(Slice(data, ofs + 4, 136)),
                PKSMStorageFormat.SIX => new PK6(Slice(data, ofs + 4, 232)),
                PKSMStorageFormat.SEVEN => new PK7(Slice(data, ofs + 4, 232)),
                PKSMStorageFormat.LGPE => new PB7(Slice(data, ofs + 4, 232)),
                _ => null
            };
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
