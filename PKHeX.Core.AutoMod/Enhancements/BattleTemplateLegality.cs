using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class BattleTemplateLegality
    {
        public const string ANALYSIS_INVALID = "No possible encounter could be found. Specific analysis for this set is unavailable.";
        private static string SPECIES_UNAVAILABLE_FORM => "{0} with form {1} is unavailable in this game.";
        private static string SPECIES_UNAVAILABLE => "{0} is unavailable in the game.";
        private static string INVALID_MOVES => "{0} cannot learn the following move(s) in this game: {1}.";
        private static string ALL_MOVES_INVALID => "All the requested moves for this Pokémon are invalid.";
        private static string LEVEL_INVALID => "Requested level is lower than the minimum possible level for {0}. Minimum required level is {1}.";
        private static string SHINY_INVALID => "Requested shiny value (ShinyType.{0}) is not possible for the given set.";
        private static string BALL_INVALID => "{0} Ball is not possible for the given set.";
        private static string ONLY_HIDDEN_ABILITY_AVAILABLE => "You can only obtain {0} with hidden ability in this game.";
        private static string HIDDEN_ABILITY_UNAVAILABLE => "You cannot obtain {0} with hidden ability in this game.";

        public static string SetAnalysis(this IBattleTemplate set, ITrainerInfo sav, PKM blank)
        {
            var species_name = SpeciesName.GetSpeciesNameGeneration(set.Species, (int)LanguageID.English, sav.Generation);
            var analysis = set.Form == 0 ? string.Format(SPECIES_UNAVAILABLE, species_name)
                                     : string.Format(SPECIES_UNAVAILABLE_FORM, species_name, set.FormName);

            // Species checks
            var gv = (GameVersion)sav.Game;
            if (!gv.ExistsInGame(set.Species, set.Form))
                return analysis; // Species does not exist in the game

            // Species exists -- check if it has at least one move.
            // If it has no moves and it didn't generate, that makes the mon still illegal in game (moves are set to legal ones)
            var moves = set.Moves.Where(z => z != 0).ToArray();
            var count = set.Moves.Count(z => z != 0);

            // Reusable data
            var batchedit = false;
            IReadOnlyList<StringInstruction>? filters = null;
            if (set is RegenTemplate r)
            {
                filters = r.Regen.Batch.Filters;
                batchedit = APILegality.AllowBatchCommands && r.Regen.HasBatchSettings;
            }
            var destVer = (GameVersion)sav.Game;
            if (destVer <= 0 && sav is SaveFile s)
                destVer = s.Version;
            var gamelist = APILegality.FilteredGameList(blank, destVer, batchedit ? filters : null);

            // Move checks
            List<IEnumerable<int>> move_combinations = new();
            for (int i = count; i >= 1; i--)
                move_combinations.AddRange(GetKCombs(moves, i));

            int[] original_moves = new int[4];
            set.Moves.CopyTo(original_moves, 0);
            int[] successful_combination = GetValidMoves(set, sav, move_combinations, blank, gamelist);
            if (!new HashSet<int>(original_moves.Where(z => z != 0)).SetEquals(successful_combination))
            {
                var invalid_moves = string.Join(", ", original_moves.Where(z => !successful_combination.Contains(z) && z != 0).Select(z => $"{(Move)z}"));
                return successful_combination.Length > 0 ? string.Format(INVALID_MOVES, species_name, invalid_moves) : ALL_MOVES_INVALID;
            }

            // All moves possible, get encounters
            blank.ApplySetDetails(set);
            blank.SetMoves(original_moves, true);
            blank.SetRecordFlags();
            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: blank, moves: original_moves, gamelist).ToList();
            if (set is RegenTemplate rt && rt.Regen.EncounterFilters is { } x)
                encounters.RemoveAll(enc => !BatchEditing.IsFilterMatch(x, enc));

            // Level checks, check if level is impossible to achieve
            if (encounters.All(z => !APILegality.IsRequestedLevelValid(set, z)))
                return string.Format(LEVEL_INVALID, species_name, encounters.Min(z => z.LevelMin));
            encounters.RemoveAll(enc => !APILegality.IsRequestedLevelValid(set, enc));

            // Shiny checks, check if shiny is impossible to achieve
            Shiny shinytype = set.Shiny ? Shiny.Always : Shiny.Never;
            if (set is RegenTemplate ret && ret.Regen.HasExtraSettings)
                shinytype = ret.Regen.Extra.ShinyType;
            if (encounters.All(z => !APILegality.IsRequestedShinyValid(set, z)))
                return string.Format(SHINY_INVALID, shinytype);
            encounters.RemoveAll(enc => !APILegality.IsRequestedShinyValid(set, enc));

            // Ability checks
            var abilityreq = APILegality.GetRequestedAbility(blank, set);
            if (abilityreq == AbilityRequest.NotHidden && encounters.All(z => z is EncounterStatic { Ability: 4 }))
                return string.Format(ONLY_HIDDEN_ABILITY_AVAILABLE, species_name);
            if (abilityreq == AbilityRequest.Hidden && encounters.All(z => z.Generation is 3 or 4) && destVer.GetGeneration() < 8)
                return string.Format(HIDDEN_ABILITY_UNAVAILABLE, species_name);

            // Ball checks
            if (set is RegenTemplate regt && regt.Regen.HasExtraSettings)
            {
                var ball = regt.Regen.Extra.Ball;
                if (encounters.All(z => !APILegality.IsRequestedBallValid(set, z)))
                    return string.Format(BALL_INVALID, ball);
                encounters.RemoveAll(enc => !APILegality.IsRequestedBallValid(set, enc));
            }

            return ANALYSIS_INVALID;
        }

        private static int[] GetValidMoves(IBattleTemplate set, ITrainerInfo sav, List<IEnumerable<int>> move_combinations, PKM blank, GameVersion[] gamelist)
        {
            int[] successful_combination = Array.Empty<int>();
            foreach (var c in move_combinations)
            {
                var combination = c.ToArray();
                if (combination.Length <= successful_combination.Length)
                    continue;
                var new_moves = combination.Concat(Enumerable.Repeat(0, 4 - combination.Length)).ToArray();
                blank.ApplySetDetails(set);
                blank.SetMoves(new_moves, true);
                blank.SetRecordFlags();

                if (sav.Generation <= 2)
                    blank.EXP = 0; // no relearn moves in gen 1/2 so pass level 1 to generator

                var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: blank, moves: new_moves, gamelist);
                if (set is RegenTemplate r && r.Regen.EncounterFilters is { } x)
                    encounters = encounters.Where(enc => BatchEditing.IsFilterMatch(x, enc));
                if (encounters.Any())
                    successful_combination = combination.ToArray();
            }
            return successful_combination;
        }

        private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1)
                return list.Select(t => new[] { t });

            var temp = list.ToArray();
            return GetKCombs(temp, length - 1).SelectMany(
                collectionSelector: t => temp.Where(o => o.CompareTo(t.Last()) > 0),
                resultSelector: (t1, t2) => t1.Concat(new[] { t2 }));
        }
    }
}
