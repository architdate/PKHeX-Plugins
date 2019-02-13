namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Indication for how a <see cref="PKM"/> is/was legalized.
    /// </summary>
    public enum LegalizationResult
    {
        /// <summary>
        /// Successfully regenerated from <see cref="IEncounterable"/> data.
        /// </summary>
        Regenerated,

        /// <summary>
        /// Passed through an attempt of Brute Forcing certain legality properties.
        /// </summary>
        BruteForce,
    }
}