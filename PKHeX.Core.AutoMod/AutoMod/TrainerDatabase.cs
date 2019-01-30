using System;
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

        public void Register(PKM pkm) => Register(new PokeTrainerDetails(pkm));

        private class PokeTrainerDetails : ITrainerInfo
        {
            private readonly PKM pkm;

            public PokeTrainerDetails(PKM pk) => pkm = pk;

            public int TID { get => pkm.TID; set => throw new ArgumentException(); }
            public int SID { get => pkm.SID; set => throw new ArgumentException(); }

            public string OT => pkm.OT_Name;
            public int Gender => pkm.OT_Gender;
            public int Game => pkm.Version;
            public int Language => pkm.Language;
            public int Country => pkm.Country;
            public int SubRegion => pkm.Region;
            public int ConsoleRegion => pkm.ConsoleRegion;
            public int Generation => pkm.GenNumber;
        }
    }
}