using System.IO;
using PKHeX.Core;

namespace ExportTrainerData
{
    public static class TrainerDataExporter
    {
        public static bool ExportTextFile(PKM pk)
        {
            string TID = "23456";
            string SID = "34567";
            string OT = "Archit";
            string Gender = "M";
            string Country = "Canada";
            string SubRegion = "Alberta";
            string ConsoleRegion = "Americas (NA/SA)";
            try
            {
                TID = pk.TID.ToString();
                SID = pk.SID.ToString();
                OT = pk.OT_Name;
                Gender = pk.OT_Gender == 1 ? "F" : "M";
                Country = pk.Country.ToString();
                SubRegion = pk.Region.ToString();
                ConsoleRegion = pk.ConsoleRegion.ToString();
                WriteTxtFile(TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion);
                return true;
            }
            catch
            {
                WriteTxtFile(TID, SID, OT, Gender, Country, SubRegion, ConsoleRegion);
                return false;
            }
        }

        private static void WriteTxtFile(string TID, string SID, string OT, string Gender, string Country, string SubRegion, string ConsoleRegion)
        {
            string[] lines =
            {
                $"TID:{TID}",
                $"SID:{SID}",
                $"OT:{OT}",
                $"Gender:{Gender}",
                $"Country:{Country}",
                $"SubRegion:{SubRegion}",
                $"3DSRegion:{ConsoleRegion}"
            };
            File.WriteAllLines("trainerdata.txt", lines);
        }
    }
}