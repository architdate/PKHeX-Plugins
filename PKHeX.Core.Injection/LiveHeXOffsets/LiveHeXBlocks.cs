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
        public static bool IsSpecialBlock(this SaveBlock sb, out Type? keytype)
        {
            keytype = null;
            foreach (Type k in BlockFormMapping.Keys)
            {
                if (!k.IsAssignableFrom(sb.GetType()))
                    continue;
                keytype = k;
                return true;
            }
            return false;
        }
    }
}
