using System;
using System.Linq;
using System.Reflection;

namespace PKHeX.Core.AutoMod
{
    public static class ALMVersion
    {
        private static readonly Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        public static Version? GetCurrentVersion(string assemblyName)
        {
            var assembly = assemblies.FirstOrDefault(x => x.GetName().Name == assemblyName);
            return assembly?.GetName().Version;
        }
    }
}
