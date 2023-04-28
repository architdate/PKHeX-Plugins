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
        public static bool IsSpecialBlock(this string block, PokeSysBotMini psb, out string? value)
        {
            return psb.Injector.SpecialBlocks.TryGetValue(block, out value);
        }
    }
}
