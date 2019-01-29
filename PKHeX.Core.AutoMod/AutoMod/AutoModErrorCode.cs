namespace PKHeX.Core.AutoMod
{
    public enum AutoModErrorCode
    {
        None,
        NoSingleImport,

        CODE_SILENT,

        NotEnoughSpace,
        InvalidLines,
    }

    public static class AutoModErrorCodeExtensions
    {
        public static string GetMessage(this AutoModErrorCode code)
        {
            if (code <= AutoModErrorCode.CODE_SILENT)
                return string.Empty;
            switch (code)
            {
                case AutoModErrorCode.NotEnoughSpace:
                    return "Not enough space in the box.";
                case AutoModErrorCode.InvalidLines:
                    return "Invalid lines detected.";
            }
            return string.Empty;
        }
    }
}