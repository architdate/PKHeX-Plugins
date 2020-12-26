using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Suggestion edits that rely on a <see cref="LegalityAnalysis"/> being done.
    /// </summary>
    public static class LegalEdits
    {
        /// <summary>
        /// Set a valid Pokeball based on a legality check's suggestions.
        /// </summary>
        /// <param name="pk">Pokémon to modify</param>
        /// <param name="matching">Set matching ball</param>
        /// <param name="force"></param>
        /// <param name="ball"></param>
        public static void SetSuggestedBall(this PKM pk, bool matching = true, bool force = false, Ball ball = Ball.None)
        {
            if (ball != Ball.None)
            {
                var orig = pk.Ball;
                pk.Ball = (int) ball;
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
            var report = la.Report();
            if (!report.Contains(LegalityCheckStrings.LBallEncMismatch) || force)
                return;
            if (pk.Generation == 5 && pk.Met_Location == 75)
                pk.Ball = (int)Ball.Dream;
            else
                pk.Ball = 4;
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
        /// <param name="allValid">Set all valid ribbons only</param>
        public static void SetSuggestedRibbons(this PKM pk, bool allValid = true)
        {
            if (allValid)
                RibbonApplicator.SetAllValidRibbons(pk);
            string report = new LegalityAnalysis(pk).Report();
            if (report.Contains(string.Format(LegalityCheckStrings.LRibbonFMissing_0, "")))
            {
                var val = string.Format(LegalityCheckStrings.LRibbonFMissing_0, "");
                var ribbonList = GetRequiredRibbons(report, val);
                var missingRibbons = GetRibbonsRequired(pk, ribbonList);
                SetRibbonValues(pk, missingRibbons, 0, true);
            }
            if (report.Contains(string.Format(LegalityCheckStrings.LRibbonFInvalid_0, "")))
            {
                var val = string.Format(LegalityCheckStrings.LRibbonFInvalid_0, "");
                string[] ribbonList = GetRequiredRibbons(report, val);
                var invalidRibbons = GetRibbonsRequired(pk, ribbonList);
                SetRibbonValues(pk, invalidRibbons, 0, false);
            }
        }

        /// <summary>
        /// Method to get ribbons from ribbon string array
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <param name="ribbonList">string array of ribbons</param>
        /// <returns>IEnumberable of all ribbons</returns>
        private static IEnumerable<string> GetRibbonsRequired(PKM pk, string[] ribbonList)
        {
            foreach (var RibbonName in GetRibbonNames(pk))
            {
                string v = RibbonStrings.GetName(RibbonName).Replace("Ribbon", "");
                if (ribbonList.Contains(v))
                    yield return RibbonName;
            }
        }

        /// <summary>
        /// Get required ribbons from the report
        /// </summary>
        /// <param name="Report">legality report</param>
        /// <param name="val">value passed</param>
        /// <returns></returns>
        private static string[] GetRequiredRibbons(string Report, string val)
        {
            return Report.Split(new[] { val }, StringSplitOptions.None)[1].Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(new[] { ", " }, StringSplitOptions.None);
        }

        /// <summary>
        /// Set ribbon values to the pkm file using reflectutil
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <param name="ribNames">string of ribbon names</param>
        /// <param name="vRib">ribbon value</param>
        /// <param name="bRib">ribbon boolean</param>
        private static void SetRibbonValues(this PKM pk, IEnumerable<string> ribNames, int vRib, bool bRib)
        {
            foreach (string rName in ribNames)
            {
                bool intRib = rName is nameof(PK6.RibbonCountMemoryBattle) or nameof(PK6.RibbonCountMemoryContest);
                ReflectUtil.SetValue(pk, rName, intRib ? (object)vRib : bRib);
            }
        }

        /// <summary>
        /// Get ribbon names of a pkm
        /// </summary>
        /// <param name="pk">pokemon</param>
        /// <returns></returns>
        private static IEnumerable<string> GetRibbonNames(PKM pk) => ReflectUtil.GetPropertiesStartWithPrefix(pk.GetType(), "Ribbon").Distinct();
    }
}
