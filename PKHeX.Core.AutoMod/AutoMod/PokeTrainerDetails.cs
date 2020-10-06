using System;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Wrapper for a <see cref="PKM"/> to provide details as if it were a <see cref="ITrainerInfo"/>
    /// </summary>
    public class PokeTrainerDetails : ITrainerInfo, IRegionOrigin
    {
        private readonly PKM pkm;

        public PokeTrainerDetails(PKM pk) => pkm = pk;

        public int TID { get => pkm.TID; set => throw new ArgumentException("Setter for this object should never be called."); }
        public int SID { get => pkm.SID; set => throw new ArgumentException("Setter for this object should never be called."); }

        public string OT => pkm.OT_Name;
        public int Gender => pkm.OT_Gender;
        public int Game => pkm.Version;
        public int Language { get => pkm.Language; set => pkm.Language = value; }

        public int Country { get => pkm is IGeoTrack gt ? gt.Country : 0; set { if (pkm is IGeoTrack gt) gt.Country = value; } }
        public int Region { get => pkm is IGeoTrack gt ? gt.Region : 0; set { if (pkm is IGeoTrack gt) gt.Region = value; } }
        public int ConsoleRegion { get => pkm is IGeoTrack gt ? gt.ConsoleRegion : 0; set { if (pkm is IGeoTrack gt) gt.ConsoleRegion = value; } }
        public int Generation => pkm.GenNumber;
    }
}
