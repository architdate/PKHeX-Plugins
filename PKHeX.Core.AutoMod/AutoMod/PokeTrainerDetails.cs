using System;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Wrapper for a <see cref="PKM"/> to provide details as if it were a <see cref="ITrainerInfo"/>
    /// </summary>
    public class PokeTrainerDetails : ITrainerInfo
    {
        private readonly PKM pkm;

        public PokeTrainerDetails(PKM pk) => pkm = pk;

        public int TID { get => pkm.TID; set => throw new ArgumentException("Setter for this object should never be called."); }
        public int SID { get => pkm.SID; set => throw new ArgumentException("Setter for this object should never be called."); }

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