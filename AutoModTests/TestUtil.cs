using System.IO;

namespace AutoModTests
{
    public static class TestUtil
    {
        public static string GetTestFolder(string name)
        {
            var folder = Directory.GetCurrentDirectory();
            while (!folder.EndsWith(nameof(AutoModTests)))
                folder = Directory.GetParent(folder).FullName;
            return Path.Combine(folder, name);
        }
    }
}
