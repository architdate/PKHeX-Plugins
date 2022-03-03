using System;
using System.IO;
using PKHeX.Core;

namespace AutoModTests
{
    public static class TestUtil
    {
        static TestUtil() => InitializePKHeXEnvironment();
        private static bool Initialized;

        public static void InitializePKHeXEnvironment()
        {
            if (Initialized)
                return;
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB();
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
            Initialized = true;
        }

        public static string GetTestFolder(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
            {
                var dir = Directory.GetParent(folder);
                if (dir == null)
                    throw new DirectoryNotFoundException($"Unable to find a directory named {nameof(AutoModTests)}.");
                folder = dir.FullName;
            }
            return Path.Combine(folder, name);
        }
    }
}
