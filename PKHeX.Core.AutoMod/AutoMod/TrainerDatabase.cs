using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    public class TrainerDatabase
    {
        private readonly Dictionary<GameVersion, List<ITrainerInfo>> Database = new Dictionary<GameVersion, List<ITrainerInfo>>();

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

        public void Register(ITrainerInfo trainer)
        {
            var ver = (GameVersion) trainer.Game;
            if (Database.TryGetValue(ver, out var list))
                list.Add(trainer);
            else
                Database.Add(ver, new List<ITrainerInfo> {trainer});
        }
    }
}