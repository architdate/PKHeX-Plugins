using System.IO;
using PKHeX.Core;

namespace AutoModTests
{
    public static class TestUtil
    {
        static TestUtil() => InitializePKHeXEnvironment();

        private static void InitializePKHeXEnvironment()
        {
            if (!EncounterEvent.Initialized)
                EncounterEvent.RefreshMGDB();
            RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
        }

        public static string GetTestFolder(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
                folder = Directory.GetParent(folder).FullName;
            return Path.Combine(folder, name);
        }
    }
}
