using System.IO;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Logic to load <see cref="ITrainerInfo"/> from a saved text file.
    /// </summary>
    public static class TrainerSettings
    {
        private static readonly TrainerDatabase Database = new TrainerDatabase();
        private static readonly string TrainerPath = Path.Combine(Directory.GetCurrentDirectory(), "trainers");

        static TrainerSettings()
        {
            if (!Directory.Exists(TrainerPath))
                return;
            var files = Directory.EnumerateFiles(TrainerPath, "*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var len = new FileInfo(f).Length;
                if (!PKX.IsPKM(len))
                    return;
                var data = File.ReadAllBytes(f);
                var pk = PKMConverter.GetPKMfromBytes(data, f);
                if (pk != null)
                    Database.Register(pk);
            }
        }

        private static ITrainerInfo SAV => API.SAV;

        public static ITrainerInfo GetSavedTrainerData(PKM legal = null)
        {
            if (legal == null)
                return SAV;
            var trainer = Database.GetTrainer(legal.Version);
            return trainer ?? SAV;
        }

        public static ITrainerInfo GetRoughTrainerData(this PKM illegalPK)
        {
            return new SimpleTrainerInfo
            {
                TID = illegalPK.TID,
                SID = illegalPK.SID,
                OT = illegalPK.OT_Name,
                Gender = illegalPK.OT_Gender,
            };
        }
    }
}
