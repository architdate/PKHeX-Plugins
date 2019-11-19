using System;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Modifications for a <see cref="PKM"/> based on a <see cref="ShowdownSet"/>
    /// </summary>
    public static class ShowdownEdits
    {
        /// <summary>
        /// Quick Gender Toggle
        /// </summary>
        /// <param name="pk">PKM whose gender needs to be toggled</param>
        /// <param name="set">Showdown Set for Gender reference</param>
        public static void FixGender(this PKM pk, ShowdownSet set)
        {
            pk.SetGender(set.Gender);
            var la = new LegalityAnalysis(pk);
            string Report = la.Report();

            if (Report.Contains(LegalityCheckStrings.LPIDGenderMismatch))
                pk.Gender = pk.Gender == 0 ? 1 : 0;

            if (pk.Gender != 0 && pk.Gender != 1)
                pk.Gender = pk.GetSaneGender();
        }

        /// <summary>
        /// Set Nature and Ability of the pokemon
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        public static void SetNatureAbility(this PKM pk, ShowdownSet set)
        {
            // Values that are must for showdown set to work, IVs should be adjusted to account for this
            pk.Nature = Math.Max(0, set.Nature);
            if (pk is PK8 pkm)
                pkm.StatNature = Math.Max(0, set.Nature);
            pk.SetAbility(set.Ability);
        }

        /// <summary>
        /// Set Species and Level with nickname (Helps with PreEvos)
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Set to use as reference</param>
        /// <param name="Form">Form to apply</param>
        public static void SetSpeciesLevel(this PKM pk, ShowdownSet set, int Form)
        {
            pk.Species = set.Species;
            if (set.Gender != null)
                pk.Gender = set.Gender == "M" ? 0 : 1;
            else
                pk.Gender = pk.GetSaneGender();
            pk.SetAltForm(Form);
            pk.SetNickname(set.Nickname);
            pk.CurrentLevel = set.Level;
            if (pk.CurrentLevel == 50)
                pk.CurrentLevel = 100; // VGC Override
        }

        /// <summary>
        /// Set Moves, EVs and Items for a specific PKM. These should not affect legality after being vetted by GeneratePKMs
        /// </summary>
        /// <param name="pk">PKM to modify</param>
        /// <param name="set">Showdown Set to refer</param>
        public static void SetMovesEVsItems(this PKM pk, ShowdownSet set)
        {
            pk.SetMoves(set.Moves, true);
            pk.CurrentFriendship = set.Friendship;
            if (pk is IAwakened pb7)
            {
                pb7.SetSuggestedAwakenedValues(pk);
            }
            else
            {
                pk.EVs = set.EVs;
                pk.ApplyHeldItem(set.HeldItem, set.Format);
                var la = new LegalityAnalysis(pk);
                if (la.Parsed && !pk.WasEvent)
                    pk.SetRelearnMoves(la.GetSuggestedRelearnMoves());
            }
        }
    }
}