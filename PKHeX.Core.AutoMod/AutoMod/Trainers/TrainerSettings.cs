using System;
using System.IO;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Logic to load <see cref="ITrainerInfo"/> from a saved text file.
    /// </summary>
    public static class TrainerSettings
    {
        private static readonly TrainerDatabase Database = new();
        private static readonly string TrainerPath = Path.Combine(
            Path.GetDirectoryName(Environment.ProcessPath)!,
            "trainers"
        );
        private static readonly SimpleTrainerInfo DefaultFallback8 = new(GameVersion.SW);
        private static readonly SimpleTrainerInfo DefaultFallback7 = new(GameVersion.UM);
        private static readonly GameVersion[] FringeVersions =
        [
            GameVersion.GG,
            GameVersion.BDSP,
            GameVersion.PLA
        ];

        public static string DefaultOT { get; set; } = "ALM";
        public static ushort DefaultTID16 { get; set; } = 54321; // reverse of PKHeX defaults
        public static ushort DefaultSID16 { get; set; } = 12345; // reverse of PKHeX defaults

        public static ITrainerInfo DefaultFallback(int gen = 8, LanguageID? lang = null)
        {
            var fallback = gen > 7 ? DefaultFallback8 : DefaultFallback7;
            if (lang == null)
                return fallback;
            return new SimpleTrainerInfo(fallback.Version) { Language = (int)lang };
        }

        public static ITrainerInfo DefaultFallback(GameVersion ver, LanguageID? lang = null)
        {
            if (!ver.IsValidSavedVersion())
                ver = GameUtil.GameVersions.First(z => ver.Contains(z));
            var ctx = ver.GetContext();
            var fallback =
                lang == null
                    ? new SimpleTrainerInfo(ver) { Context = ctx }
                    : new SimpleTrainerInfo(ver) { Language = (int)lang, Context = ctx };
            fallback.OT = DefaultOT;
            fallback.TID16 = DefaultTID16;
            fallback.SID16 = DefaultSID16;
            return fallback;
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
                if (!EntityDetection.IsSizePlausible(len))
                    return;
                var data = File.ReadAllBytes(f);
                var prefer = EntityFileExtension.GetContextFromExtension(f);
                var pk = EntityFormat.GetFromBytes(data, prefer);
                if (pk != null)
                    Database.Register(new PokeTrainerDetails(pk.Clone()));
            }
        }

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="generation"/>.
        /// </summary>
        /// <param name="generation">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(
            byte generation,
            GameVersion ver = GameVersion.Any,
            ITrainerInfo? fallback = null,
            LanguageID? lang = null
        )
        {
            ITrainerInfo? trainer = null;
            var special_version = FringeVersions.Any(z => z.Contains(ver));
            if (!special_version)
                trainer = Database.GetTrainerFromGen(generation, lang);
            if (trainer != null)
                return trainer;

            if (fallback == null)
            {
                return special_version
                    ? DefaultFallback(ver, lang)
                    : DefaultFallback(generation, lang);
            }
            if (lang == null)
                return fallback;
            if (lang == (LanguageID)fallback.Language)
                return fallback;
            return special_version ? DefaultFallback(ver, lang) : DefaultFallback(generation, lang);
        }

        /// <summary>
        /// Gets a possible Trainer Data for the requested <see cref="version"/>.
        /// </summary>
        /// <param name="version">Version of origin requested.</param>
        /// <param name="gen">Generation of origin requested.</param>
        /// <param name="fallback">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(
            GameVersion version,
            byte gen,
            ITrainerInfo? fallback = null,
            LanguageID? lang = null
        )
        {
            var byVer = Database.GetTrainer(version, lang);
            return byVer ?? GetSavedTrainerData(gen, version, fallback, lang);
        }

        /// <summary>
        /// Gets a possible Trainer Data for the provided <see cref="pk"/>.
        /// </summary>
        /// <param name="pk">Pok�mon that will receive the trainer details.</param>
        /// <param name="template_save">Fallback trainer data if no new parent is found.</param>
        /// <param name="lang">Language to request for</param>
        /// <returns>Parent trainer data that originates from the <see cref="PKM.Version"/>. If none found, will return the <see cref="fallback"/>.</returns>
        public static ITrainerInfo GetSavedTrainerData(
            PKM pk,
            ITrainerInfo template_save,
            LanguageID? lang = null
        )
        {
            byte origin = pk.Generation;
            byte format = pk.Format;
            if (format != origin)
                return GetSavedTrainerData(format, template_save.Version, fallback: template_save, lang: lang);
            return GetSavedTrainerData(pk.Version, origin, template_save, lang);
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
