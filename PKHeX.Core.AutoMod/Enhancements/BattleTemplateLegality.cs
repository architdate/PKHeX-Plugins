using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class BattleTemplateLegality
    {
        public const string ANALYSIS_INVALID = "Analysis for this set is unavailable.";
        private static string SPECIES_UNAVAILABLE_FORM => "{0} with form {1} is unavailable in the game.";
        private static string SPECIES_UNAVAILABLE => "{0} is unavailable in the game.";
        private static string INVALID_MOVE => "{0} cannot learn {1} in the game.";
        private static string INVALID_MOVES => "{0} cannot learn the following moves in the game: {1}.";
        private static string ALL_MOVES_INVALID => "All the requested moves for this Pokémon are invalid.";

        public static SetLegality SetAnalysis(this RegenTemplate set, SaveFile sav, out string analysis)
        {
            var species_name = SpeciesName.GetSpeciesNameGeneration(set.Species, (int)LanguageID.English, sav.Generation);
            analysis = set.Form == 0 ? string.Format(SPECIES_UNAVAILABLE, species_name)
                                     : string.Format(SPECIES_UNAVAILABLE_FORM, species_name, set.FormName);

            // Species checks
            var gv = (GameVersion)sav.Game;
            if (!gv.ExistsInGame(set.Species, set.Form))
                return SetLegality.SpeciesUnavilable; // Species does not exist in the game

            // Species exists -- check if it has atleast one move. If it has no moves and it didn't generate, that makes the mon still illegal in game (moves are set to legal ones)
            var moves = set.Moves.Where(z => z != 0);
            var count = moves.Count();
            if (count == 0)
                return SetLegality.SpeciesUnavilable; // Species does not exist in the game

            analysis = string.Format(INVALID_MOVE, species_name, (Move)set.Moves[0]);
            if (count == 1)
                return SetLegality.InvalidMoves; // The only move specified cannot be learnt by the mon

            List<IEnumerable<int>> move_combinations = new();
            for (int i = moves.Count() - 1; i >= 1; i--)
                move_combinations.AddRange(GetKCombs(moves, i));

            var blank = sav.BlankPKM;
            int[] original_moves = new int[4];
            set.Moves.CopyTo(original_moves, 0);
            int[] successful_combination = new int[0];
            foreach (var combination in move_combinations)
            {
                if (combination.Count() <= successful_combination.Length)
                    continue;
                var new_moves = combination.Concat(Enumerable.Repeat(0, 4 - combination.Count()));
                set.Moves = new_moves.ToArray();
                blank.ApplySetDetails(set);
                blank.SetRecordFlags();
                var batchedit = APILegality.AllowBatchCommands && set.Regen.HasBatchSettings;

                var destVer = (GameVersion)sav.Game;
                if (destVer <= 0)
                    destVer = sav.Version;

                var gamelist = APILegality.FilteredGameList(blank, destVer, batchedit ? set.Regen.Batch.Filters : null);
                if (sav.Generation <= 2)
                    blank.EXP = 0; // no relearn moves in gen 1/2 so pass level 1 to generator

                var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: blank, moves: set.Moves, gamelist);
                var criteria = EncounterCriteria.GetCriteria(set, blank.PersonalInfo);
                if (set.Regen.EncounterFilters != null)
                    encounters = encounters.Where(enc => BatchEditing.IsFilterMatch(set.Regen.EncounterFilters, enc));
                if (encounters.Any())
                    successful_combination = combination.ToArray();
            }
            var invalid_moves = string.Join(", ", original_moves.Where(z => !successful_combination.Contains(z) && z != 0).Select(z => $"{(Move)z}"));
            analysis = successful_combination.Length > 0 ? string.Format(INVALID_MOVES, species_name, invalid_moves) : ALL_MOVES_INVALID;
            return SetLegality.InvalidMoves;
        }

        private static IEnumerable<IEnumerable<T>> GetKCombs<T>(IEnumerable<T> list, int length) where T : IComparable
        {
            if (length == 1) return list.Select(t => new T[] { t });
            return GetKCombs(list, length - 1)
                .SelectMany(t => list.Where(o => o.CompareTo(t.Last()) > 0),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }

    public enum SetLegality
    {
        Valid,
        SpeciesUnavilable,
        InvalidMoves,
        InvalidSet
    }
}
