using System.IO;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModTests
{
    public static class TestUtil
    {
        static TestUtil() => InitializePKHeXEnvironment();
        private static bool Initialized;

        private static readonly object _lock = new();

        public static void InitializePKHeXEnvironment()
        {
            lock (_lock)
            {
                if (Initialized)
                    return;
                if (EncounterEvent.MGDB_G3.Length == 0)
                    EncounterEvent.RefreshMGDB();
                RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
                Legalizer.EnableEasterEggs = false;
                APILegality.SetAllLegalRibbons = false;
                APILegality.Timeout = 99999;
                Initialized = true;
            }
        }

        public static string GetTestFolder(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
            {
                var dir = Directory.GetParent(folder) ?? throw new DirectoryNotFoundException($"Unable to find a directory named {nameof(AutoModTests)}.");
                folder = dir.FullName;
            }
            return Path.Combine(folder, name);
        }
    }
}
