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

        public string OT { get => pkm.OT_Name; set => pkm.OT_Name = value; }
        public int Gender => pkm.OT_Gender;
        public int Game => pkm.Version;
        public int Language { get => pkm.Language; set => pkm.Language = value; }

        public byte Country { get => pkm is IGeoTrack gt ? gt.Country : (byte)49; set { if (pkm is IGeoTrack gt) gt.Country = value; } }
        public byte Region { get => pkm is IGeoTrack gt ? gt.Region : (byte)7; set { if (pkm is IGeoTrack gt) gt.Region = value; } }
        public byte ConsoleRegion { get => pkm is IGeoTrack gt ? gt.ConsoleRegion : (byte)1; set { if (pkm is IGeoTrack gt) gt.ConsoleRegion = value; } }
        public int Generation => pkm.Generation;
        public EntityContext Context => pkm.Context;

        public static PokeTrainerDetails Clone(PokeTrainerDetails p) => new(p.pkm.Clone());
    }
}
