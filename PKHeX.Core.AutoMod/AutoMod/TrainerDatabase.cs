using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Contains many <see cref="ITrainerInfo"/> instances to match against a <see cref="GameVersion"/>.
    /// </summary>
    public class TrainerDatabase
    {
        private readonly Dictionary<GameVersion, List<ITrainerInfo>> Database = new Dictionary<GameVersion, List<ITrainerInfo>>();

        /// <summary>
        /// Fetches an appropriate trainer based on the requested <see cref="version"/>.
        /// </summary>
        /// <param name="version">Version the trainer should originate from</param>
        /// <returns>Null if no trainer found for this version.</returns>
        public ITrainerInfo? GetTrainer(int version) => GetTrainer((GameVersion) version);

        /// <summary>
        /// Fetches an appropriate trainer based on the requested <see cref="ver"/>.
        /// </summary>
        /// <param name="ver">Version the trainer should originate from</param>
        /// <returns>Null if no trainer found for this version.</returns>
        public ITrainerInfo? GetTrainer(GameVersion ver)
        {
            if (ver <= 0)
                return null;

            if (ver >= GameVersion.RB)
                return GetTrainerFromGroup(ver);

            if (Database.TryGetValue(ver, out var list))
                return GetRandomChoice(list);

            return null;
        }

        private static T GetRandomChoice<T>(IReadOnlyList<T> list)
        {
            if (list.Count == 1)
                return list[0];
            return list[Util.Rand.Next(list.Count)];
        }

        /// <summary>
        /// Fetches an appropriate trainer based on the requested <see cref="ver"/> group.
        /// </summary>
        /// <param name="ver">Version the trainer should originate from</param>
        /// <returns>Null if no trainer found for this version.</returns>
        private ITrainerInfo? GetTrainerFromGroup(GameVersion ver)
        {
            var possible = Database.Where(z => ver.Contains(z.Key)).ToList();
            return GetRandomTrainer(possible);
        }

        /// <summary>
        /// Fetches an appropriate trainer based on the requested <see cref="generation"/>.
        /// </summary>
        /// <param name="generation">Generation the trainer should inhabit</param>
        /// <returns>Null if no trainer found for this version.</returns>
        public ITrainerInfo? GetTrainerFromGen(int generation)
        {
            var possible = Database.Where(z => z.Key.GetGeneration() == generation).ToList();
            return GetRandomTrainer(possible);
        }

        private static ITrainerInfo? GetRandomTrainer(IReadOnlyList<KeyValuePair<GameVersion, List<ITrainerInfo>>> possible)
        {
            if (possible.Count == 0)
                return null;
            var group = GetRandomChoice(possible);
            return GetRandomChoice(group.Value);
        }

        /// <summary>
        /// Adds the <see cref="trainer"/> to the <see cref="Database"/>.
        /// </summary>
        /// <param name="trainer">Trainer details to add.</param>
        public void Register(ITrainerInfo trainer)
        {
            var ver = (GameVersion) trainer.Game;
            if (ver <= 0 && trainer is SaveFile s)
                ver = s.Version;
            if (!Database.TryGetValue(ver, out var list))
            {
                Database.Add(ver, new List<ITrainerInfo> {trainer});
                return;
            }

            if (list.Contains(trainer))
                return;
            list.Add(trainer);
        }

        /// <summary>
        /// Adds the trainer details of the <see cref="pkm"/> to the <see cref="Database"/>.
        /// </summary>
        /// <param name="pkm">Pokémon with Trainer details to add.</param>
        /// <remarks>A copy of the object will be made to prevent modifications, just in case.</remarks>
        public void Register(PKM pkm) => Register(new PokeTrainerDetails(pkm.Clone()));
    }
}