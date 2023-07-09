using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Suggestion edits that rely on a <see cref="LegalityAnalysis"/> being done.
    /// </summary>
    public static class LegalEdits
    {
        private static readonly Dictionary<Ball, Ball> LABallMapping = new()
        {
            { Ball.Poke,  Ball.LAPoke },
            { Ball.Great, Ball.LAGreat },
            { Ball.Ultra, Ball.LAUltra },
            { Ball.Heavy, Ball.LAHeavy },
        };

        public static bool ReplaceBallPrefixLA { get; set; }

        /// <summary>
        /// Set a valid Pokeball based on a legality check's suggestions.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        /// <param name="matching">Set matching ball</param>
        /// <param name="force"></param>
        /// <param name="ball"></param>
        public static void SetSuggestedBall(this PKM pk, bool matching = true, bool force = false, Ball ball = Ball.None, IEncounterable? enc = null)
        {
            var orig = pk.Ball;
            if (ball == Ball.None)
                force = false; // accept anything if no ball is specified

            if (enc is MysteryGift)
                return;

            var legal = new LegalityAnalysis(pk).Valid;

            if (ball != Ball.None)
            {
                if (pk.LA && ReplaceBallPrefixLA && LABallMapping.TryGetValue(ball, out var modified))
                    ball = modified;
                pk.Ball = (int)ball;
                if (!force && !pk.ValidBall())
                    pk.Ball = orig;
            }
            else if (matching)
            {
                if (!pk.IsShiny)
                    pk.SetMatchingBall();
                else
                    Aesthetics.ApplyShinyBall(pk);
            }

            var la = new LegalityAnalysis(pk);
            if (force || la.Valid)
                return;

            if (pk.Generation == 5 && pk.Met_Location == 75)
                pk.Ball = (int)Ball.Dream;
            else
                pk.Ball = orig;

            if (legal && !la.Valid)
                pk.Ball = orig;
        }

        public static bool ValidBall(this PKM pk)
        {
            var rep = new LegalityAnalysis(pk).Report(true);
            return rep.Contains(LegalityCheckStrings.LBallEnc) || rep.Contains(LegalityCheckStrings.LBallSpeciesPass);
        }

        /// <summary>
        /// Sets all ribbon flags according to a legality report.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        /// <param name="enc">Encounter matched to</param>
        /// <param name="allValid">Set all valid ribbons only</param>
        public static void SetSuggestedRibbons(this PKM pk, IBattleTemplate set, IEncounterable enc, bool allValid = true)
        {
            if (allValid)
            {
                RibbonApplicator.SetAllValidRibbons(pk);
                if (pk is PK8 pk8 && pk8.Species != (int)Species.Shedinja && pk8.GetRandomValidMark(set, enc, out var mark))
                    pk8.SetRibbonIndex(mark);
            }
            else RibbonApplicator.RemoveAllValidRibbons(pk);
        }
    }
}
