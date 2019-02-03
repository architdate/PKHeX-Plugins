namespace PKHeX.Core.AutoMod
{
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

            switch (code)
            {
                case AutoModErrorCode.NotEnoughSpace:
                    return "Not enough space in the box.";
                case AutoModErrorCode.InvalidLines:
                    return "Invalid lines detected.";
                default:
                    return string.Empty;
            }
        }

        public static bool IsSilent(this AutoModErrorCode code) => code <= AutoModErrorCode.CODE_SILENT;
    }
}