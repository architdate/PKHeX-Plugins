using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKHeX.Core.AutoMod
{
    public class LivingDexConfig
    {
        public bool IncludeForms { get; init; }
        public bool SetShiny { get; init; }
        public bool SetAlpha { get; init; }
        public bool NativeOnly { get; init; }

        public override string ToString()
        {
            return $"IncludeForms: {IncludeForms}\nSetShiny: {SetShiny}\nSetAlpha: {SetAlpha}\nNativeOnly: {NativeOnly}";
        }
    }
}
