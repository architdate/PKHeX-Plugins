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
        static TrainerSettings() => LoadTrainerDatabaseFromPath(TrainerPath);

        private static readonly ITrainerInfo DefaultFallback = new SimpleTrainerInfo();

        /// <summary>
        /// Loads possible <see cref="PKM"/> data from the path, and registers them to the <see cref="Database"/>.
        /// </summary>
        /// <param name="path"></param>
        public static void LoadTrainerDatabaseFromPath(string path)
        {
            if (!Directory.Exists(path))
                return;
            var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
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

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="generation"/>.
        /// </summary>
        /// <param name="generation">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(int generation, ITrainerInfo fallback = null) => Database.GetTrainerFromGen(generation) ?? fallback ?? DefaultFallback;


        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="version"/>.
        /// </summary>
        /// <param name="version">Version of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(GameVersion version, ITrainerInfo fallback = null) => Database.GetTrainer(version) ?? fallback ?? DefaultFallback;

        /// <summary>
        /// Gets a possible Trainer Data for the provided <see cref="pk"/>.
        /// </summary>
        /// <param name="pk">Pokémon that will receive the trainer details.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(PKM pk, ITrainerInfo fallback = null) => GetSavedTrainerData((GameVersion)pk.Version, fallback);
    }
}
