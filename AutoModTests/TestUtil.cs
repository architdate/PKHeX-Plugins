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

        private static readonly NicknameRestriction NicknameRestriction = new() { NicknamedTrade = Severity.Fishy, NicknamedMysteryGift = Severity.Fishy };

        public static void InitializePKHeXEnvironment()
        {
            lock (_lock)
            {
                if (Initialized)
                    return;
                EncounterEvent.RefreshMGDB();
                RibbonStrings.ResetDictionary(GameInfo.Strings.ribbons);
                Legalizer.EnableEasterEggs = false;
                APILegality.SetAllLegalRibbons = false;
                APILegality.Timeout = 99999;
                ParseSettings.Settings.Handler.CheckActiveHandler = false;
                ParseSettings.Settings.HOMETransfer.HOMETransferTrackerNotPresent = Severity.Fishy;
                ParseSettings.Settings.Nickname.Nickname12 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname3 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname4 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname5 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname6 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname7 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname7b = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname8 = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname8a = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname8b = NicknameRestriction;
                ParseSettings.Settings.Nickname.Nickname9 = NicknameRestriction;
                Initialized = true;
            }
        }

        public static string GetTestFolder(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
            {
                var dir =
                    Directory.GetParent(folder)
                    ?? throw new DirectoryNotFoundException(
                        $"Unable to find a directory named {nameof(AutoModTests)}."
                    );
                folder = dir.FullName;
            }
            return Path.Combine(folder, name);
        }
    }
}
