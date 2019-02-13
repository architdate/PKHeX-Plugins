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
        public ITrainerInfo GetTrainer(int version) => GetTrainer((GameVersion) version);

        /// <summary>
        /// Fetches an appropriate trainer based on the requested <see cref="ver"/>.
        /// </summary>
        /// <param name="ver">Version the trainer should originate from</param>
        /// <returns>Null if no trainer found for this version.</returns>
        public ITrainerInfo GetTrainer(GameVersion ver)
        {
            if (Database.TryGetValue(ver, out var list))
            {
                if (list.Count == 1)
                    return list[0];
                return list[Util.Rand.Next(list.Count)];
            }

            return null;
        }

        /// <summary>
        /// Fetches an appropriate trainer based on the requested <see cref="generation"/>.
        /// </summary>
        /// <param name="generation">Generation the trainer should inhabit</param>
        /// <returns>Null if no trainer found for this version.</returns>
        public ITrainerInfo GetTrainerFromGen(int generation)
        {
            var possible = Database.Where(z => z.Key.GetGeneration() == generation).ToList();
            var group = Util.Rand.Next(possible.Count);
            var list = possible[group].Value;
            return list[Util.Rand.Next(list.Count)];
        }

        /// <summary>
        /// Adds the <see cref="trainer"/> to the <see cref="Database"/>.
        /// </summary>
        /// <param name="trainer">Trainer details to add.</param>
        public void Register(ITrainerInfo trainer)
        {
            var ver = (GameVersion) trainer.Game;
            if (Database.TryGetValue(ver, out var list))
                list.Add(trainer);
            else
                Database.Add(ver, new List<ITrainerInfo> {trainer});
        }

        /// <summary>
        /// Adds the trainer details of the <see cref="pkm"/> to the <see cref="Database"/>.
        /// </summary>
        /// <param name="pkm">Pokémon with Trainer details to add.</param>
        /// <remarks>A copy of the object will be made to prevent modifications, just in case.</remarks>
        public void Register(PKM pkm) => Register(new PokeTrainerDetails(pkm.Clone()));
    }
}