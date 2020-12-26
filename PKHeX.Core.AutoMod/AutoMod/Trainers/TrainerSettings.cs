using System.IO;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Logic to load <see cref="ITrainerInfo"/> from a saved text file.
    /// </summary>
    public static class TrainerSettings
    {
        private static readonly TrainerDatabase Database = new();
        private static readonly string TrainerPath = Path.Combine(Directory.GetCurrentDirectory(), "trainers");
        private static readonly SimpleTrainerInfo DefaultFallback8 = new(GameVersion.SW);
        private static readonly SimpleTrainerInfo DefaultFallback7 = new(GameVersion.UM);

        public static ITrainerInfo DefaultFallback(int gen = 8, LanguageID? lang = null)
        {
            var fallback = gen > 7 ? DefaultFallback8 : DefaultFallback7;
            if (lang == null)
                return fallback;
            return new SimpleTrainerInfo((GameVersion)fallback.Game) { Language = (int)lang };
        }

        static TrainerSettings() => LoadTrainerDatabaseFromPath(TrainerPath);

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
                var pk = PKMConverter.GetPKMfromBytes(data);
                if (pk != null)
                    Database.Register(pk);
            }
        }

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="generation"/>.
        /// </summary>
        /// <param name="generation">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(int generation, ITrainerInfo? fallback = null, LanguageID? lang = null)
        {
            var trainer = Database.GetTrainerFromGen(generation, lang);
            if (trainer != null)
                return trainer;

            if (fallback == null)
                return DefaultFallback(generation, lang);
            if (lang == null)
                return fallback;
            if ((int)lang == fallback.Language)
                return fallback;
            return DefaultFallback(generation, lang);
        }

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="version"/>.
        /// </summary>
        /// <param name="version">Version of origin requested.</param>
        /// <param name="gen">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(GameVersion version, int gen, ITrainerInfo? fallback = null, LanguageID? lang = null)
        {
            var byVer = Database.GetTrainer(version, lang);
            return byVer ?? GetSavedTrainerData(gen, fallback, lang);
        }

        /// <summary>
        /// Gets a possible Trainer Data for the provided <see cref="pk"/>.
        /// </summary>
        /// <param name="pk">Pokémon that will receive the trainer details.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(PKM pk, ITrainerInfo? fallback = null, LanguageID? lang = null)
        {
            int origin = pk.Generation;
            int format = pk.Format;
            if (format != origin)
                return GetSavedTrainerData(format, fallback, lang);
            return GetSavedTrainerData((GameVersion)pk.Version, origin, fallback, lang);
        }

        /// <summary>
        /// Registers the Trainer Data to the <see cref="Database"/>.
        /// </summary>
        /// <param name="tr">Trainer Data</param>
        public static void Register(ITrainerInfo tr) => Database.Register(tr);

        /// <summary>
        /// Clears the Trainer Data in the <see cref="Database"/>.
        /// </summary>
        public static void Clear() => Database.Clear();
    }
}
