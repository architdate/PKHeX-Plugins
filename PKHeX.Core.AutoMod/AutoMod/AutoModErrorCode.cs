namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Result codes for legalization and import operations.
    /// </summary>
    public enum AutoModErrorCode
    {
        None,
        NoSingleImport,

        /// <summary>
        /// Don't use this!
        /// </summary>
        CODE_SILENT,

        NotEnoughSpace,
        InvalidLines,
    }

    public static class AutoModErrorCodeExtensions
    {
        public static string GetMessage(this AutoModErrorCode code)
        {
            if (code.IsSilent())
                return string.Empty;

            return code switch
            {
                AutoModErrorCode.NotEnoughSpace => "Not enough space in the box.",
                AutoModErrorCode.InvalidLines => "Invalid lines detected.",
                _ => string.Empty,
            };
        }

        public static bool IsSilent(this AutoModErrorCode code) => code <= AutoModErrorCode.CODE_SILENT;
    }
}