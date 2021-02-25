using System;
using System.Collections.Generic;

namespace PKHeX.Core.Injection
{
    public static class LiveHeXBlocks
    {
        // WinForms function (<value>) to invoke for editing saveblocks of type <key>
        public static readonly Dictionary<Type, string> BlockFormMapping = new()
        {
            { typeof(MyItem), "B_OpenItemPouch_Click" },
        };

        /// <summary>
        /// Check if a special form needs to be open to handle the block
        /// </summary>
        /// <param name="sb">saveblock</param>
        /// <param name="keytype">type of the saveblock</param>
        /// <returns></returns>
        public static bool IsSpecialBlock(this SaveBlock sb, out string value)
        {
            value = string.Empty;
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
