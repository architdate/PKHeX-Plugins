using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Contains many <see cref="ITrainerInfo"/> instances to match against a <see cref="GameVersion"/>.
    /// </summary>
    public class TrainerDatabase
    {
        private readonly Dictionary<GameVersion, List<ITrainerInfo>> Database = new Dictionary<GameVersion, List<ITrainerInfo>>();

        /// <summary>
        /// Fetches an appropriate trainer based on the <see cref="ver"/>.
        /// </summary>
        /// <param name="ver">Version the trainer should originate from</param>
        /// <returns>Null if no trainer found for this version.</returns>
        public ITrainerInfo GetTrainer(int ver) => GetTrainer((GameVersion) ver);

        private ITrainerInfo GetTrainer(GameVersion ver)
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