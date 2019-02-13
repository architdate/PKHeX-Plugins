using System;
using System.IO;
using PKHeX.Core;

namespace AutoModPlugins
{
    public static class PKSMUtil
    {
        /// <summary>
        /// Exports PKSM's bank.bin to individual PKX files
        /// </summary>
        /// <param name="bank">Byte Array containing concatenated PKX files</param>
        public static void ExportBank(byte[] bank)
        {
            // Computing earlier to save time while looping
            int max = GameInfo.Strings.Species.Count; 

            // Todo: Support for all generations
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "PKSM Bank");

            Directory.CreateDirectory(dir);
            for (int i = 12; i < bank.Length; i += 264)
            {
                var data = new byte[232];
                Array.Copy(bank, i + 4, data, 0, 232);
                var pk = new PK7(data);
                if (pk.Species != 0 && pk.Species <= max)
                    File.WriteAllBytes(Path.Combine(dir, Util.CleanFileName(pk.FileName)), data);
            }
        }

        /// <summary>
        /// Gets byte array from .bnk format file
        /// </summary>
        /// <returns>PKSM Bank byte array</returns>
        public static byte[] GetBankData()
        {
            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { ".bnk" }, out string path))
            {
                WinFormsUtil.Alert("No Bank Data Provided");
                return null;
            }

            return File.ReadAllBytes(path);
        }
    }
}
