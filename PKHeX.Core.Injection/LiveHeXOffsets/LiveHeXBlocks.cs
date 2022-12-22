using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.Injection
{
    public static class LiveHeXBlocks
    {
        /// <summary>
        /// Check if a special form needs to be open to handle the block
        /// </summary>
        /// <param name="sb">saveblock</param>
        /// <param name="lv">LiveHeX version being edited</param>
        /// <param name="value">string value of the form to open</param>
        /// <returns>Boolean indicating if a special form needs to be opened</returns>
        public static bool IsSpecialBlock(this string block, LiveHeXVersion lv, out string? value)
        {
            value = string.Empty;
            if (LPBasic.SupportedVersions.Contains(lv))
                return LPBasic.SpecialBlocks.TryGetValue(block, out value);
            if (LPBDSP.SupportedVersions.Contains(lv))
                return LPBDSP.SpecialBlocks.TryGetValue(block, out value);
            if (LPPointer.SupportedVersions.Contains(lv))
                return LPPointer.SpecialBlocks.TryGetValue(block, out value);
            return false;
        }
    }
}
