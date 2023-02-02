using System;
using System.Reflection;

namespace PKHeX.Core.AutoMod
{
    public static class ALMVersion

    {
        public const string CurrentVersion = "23.01.30";

        /*
         * TODO: Add other versioning code, maybe compatability lists here?
         */

        public static Version? CurrentPKHeXVersion
        {
            get
            {
                Version? version = Assembly.GetEntryAssembly()?.GetName().Version;
                if (version == null)
                    return null;
                return new Version(version.Major, version.Minor, version.Build);
            }
        }
        public static Version? CurrentALMVersion => Version.TryParse(CurrentVersion, out var current_alm) ? current_alm : null;

    }
}
