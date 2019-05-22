using System;
using System.IO;
using PKHeX.Core;

namespace AutoModPlugins
{
    /// <summary>
    /// All PKSM based logic (for PKSM and PKHeX interaction)
    /// </summary>
    public static class PKSMUtil
    {

        /// <summary>
        /// Exports PKSM's bank.bin to individual PKX files
        /// </summary>
        /// <param name="bank">Byte Array containing concatenated PKX files</param>
        public static void ExportBank(byte[] bank)
        {
            // Todo: Support for all generations
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "PKSM Bank");

            Directory.CreateDirectory(dir);
            for (int i = 0; i < bank.Length; i += 232)
            {
                var data = new byte[232];
                Array.Copy(bank, i, data, 0, 232);
                var pk = new PK7(data);
                if (pk.Species != 0)
                    File.WriteAllBytes(Path.Combine(dir, Util.CleanFileName(pk.FileName)), data);
            }
        }

        /// <summary>
        /// User Input window for giving path to the bank bin.
        /// </summary>
        /// <returns>output byte array from PKSM Bank bin</returns>
        public static byte[] GetBankData()
        {
            if (!WinFormsUtil.OpenSAVPKMDialog(new[] { "bin" }, out string path))
            {
                WinFormsUtil.Alert("No data provided.");
                return null;
            }

            return File.ReadAllBytes(path);
        }
    }
}
