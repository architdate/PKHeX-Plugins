using System;
using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public static class LiveHeXBlocks
    {
        // WinForms function (<value>) to invoke for editing saveblocks of type <key>
        public static readonly Dictionary<Type, string> BlockFormMapping = new()
        {
            { typeof(MyItem),               "B_OpenItemPouch_Click" },
            { typeof(RaidSpawnList8),       "B_OpenRaids_Click"     },
        };

        public static readonly Dictionary<LiveHeXVersion, Dictionary<uint, string>> SCBlockFormMapping = new()
        {
            {
                LiveHeXVersion.SWSH_Rigel2,
                new()
                {
                    { 0x4716c404 , "B_OpenPokedex_Click" }, // KZukan
                    { 0x3F936BA9 , "B_OpenPokedex_Click" }, // KZukanR1
                    { 0x3C9366F0 , "B_OpenPokedex_Click" }, // KZukanR2
                }
            }
        };

        /// <summary>
        /// Check if a special form needs to be open to handle the block
        /// </summary>
        /// <param name="sb">saveblock</param>
        /// <param name="lv">LiveHeX version being edited</param>
        /// <param name="value">string value of the form to open</param>
        /// <returns>Boolean indicating if a special form needs to be opened</returns>
        public static bool IsSpecialBlock(this object sb, LiveHeXVersion lv, out string value)
        {
            value = string.Empty;
            if (sb is SCBlock scb)
            {
                // only keys exist here
                if (!SCBlockFormMapping.ContainsKey(lv)) 
                    return false;
                var forms = SCBlockFormMapping[lv];
                if (!forms.ContainsKey(scb.Key))
                    return false;
                value = forms[scb.Key];
                return true;
            }
            foreach (Type k in BlockFormMapping.Keys)
            {
                if (!k.IsAssignableFrom(sb.GetType()))
                    continue;
                value = BlockFormMapping[k];
                return true;
            }
            return false;
        }
    }
}
