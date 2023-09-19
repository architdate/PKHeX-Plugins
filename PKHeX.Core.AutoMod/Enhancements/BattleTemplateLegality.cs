using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace PKHeX.Core.AutoMod
{
    public static class BattleTemplateLegality
    {
        public static string ANALYSIS_INVALID { get; set; } = "Specific analysis for this set is unavailable.";
        public static string EXHAUSTED_ENCOUNTERS { get; set; } = "No valid matching encounter available: (Exhausted {0}/{1} possible encounters).";
        public static string SPECIES_UNAVAILABLE_FORM { get; set; } = "{0} with form {1} is unavailable in this game.";
        public static string SPECIES_UNAVAILABLE { get; set; } = "{0} is unavailable in the game.";
        public static string INVALID_MOVES { get; set; } = "{0} cannot learn the following move(s) in this game: {1}.";
        public static string ALL_MOVES_INVALID { get; set; } = "All the requested moves for this Pokémon are invalid.";
        public static string LEVEL_INVALID { get; set; } = "Requested level is lower than the minimum possible level for {0}. Minimum required level is {1}.";
        public static string SHINY_INVALID { get; set; } = "Requested shiny value (ShinyType.{0}) is not possible for the given set.";
        public static string ALPHA_INVALID { get; set; } = "Requested Pokémon cannot be an Alpha.";
        public static string BALL_INVALID { get; set; } = "{0} Ball is not possible for the given set.";
        public static string ONLY_HIDDEN_ABILITY_AVAILABLE { get; set; } = "You can only obtain {0} with hidden ability in this game.";
        public static string HIDDEN_ABILITY_UNAVAILABLE { get; set; } = "You cannot obtain {0} with hidden ability in this game.";

        public static string SetAnalysis(this IBattleTemplate set, ITrainerInfo sav, PKM failed)
        {
            if (failed.Version == 0)
                failed.Version = sav.Game;
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
            var gamelist = APILegality.FilteredGameList(failed, destVer, APILegality.AllowBatchCommands, set);

            // Move checks
            List<IEnumerable<ushort>> move_combinations = new();
            for (int i = count; i >= 1; i--)
                move_combinations.AddRange(GetKCombs(moves, i));

            ushort[] original_moves = new ushort[4];
            set.Moves.CopyTo(original_moves, 0);
            ushort[] successful_combination = GetValidMoves(set, sav, move_combinations, failed, gamelist);
            if (!new HashSet<ushort>(original_moves.Where(z => z != 0)).SetEquals(successful_combination))
            {
                var invalid_moves = string.Join(", ", original_moves.Where(z => !successful_combination.Contains(z) && z != 0).Select(z => $"{(Move)z}"));
                return successful_combination.Length > 0 ? string.Format(INVALID_MOVES, species_name, invalid_moves) : ALL_MOVES_INVALID;
            }

            // All moves possible, get encounters
            failed.ApplySetDetails(set);
            failed.SetMoves(original_moves);
            failed.SetRecordFlags(Array.Empty<ushort>());

            var encounters = EncounterMovesetGenerator.GenerateEncounters(pk: failed, moves: original_moves, gamelist).ToList();
            var initialcount = encounters.Count;
            if (set is RegenTemplate rt && rt.Regen.EncounterFilters is { } x)
                encounters.RemoveAll(enc => !BatchEditing.IsFilterMatch(x, enc));

            // No available encounters
            if (encounters.Count == 0)
                return string.Format(EXHAUSTED_ENCOUNTERS, initialcount, initialcount);

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

            // Alpha checks
            if (encounters.All(z => !APILegality.IsRequestedAlphaValid(set, z)))
                return ALPHA_INVALID;
            encounters.RemoveAll(enc => !APILegality.IsRequestedAlphaValid(set, enc));

            // Ability checks
            var abilityreq = APILegality.GetRequestedAbility(failed, set);
            if (abilityreq == AbilityRequest.NotHidden && encounters.All(z => z is IEncounterable { Ability: AbilityPermission.OnlyHidden }))
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

            return string.Format(EXHAUSTED_ENCOUNTERS, initialcount - encounters.Count, initialcount);
        }

        private static ushort[] GetValidMoves(IBattleTemplate set, ITrainerInfo sav, List<IEnumerable<ushort>> move_combinations, PKM blank, GameVersion[] gamelist)
        {
            ushort[] successful_combination = Array.Empty<ushort>();
            foreach (var c in move_combinations)
            {
                var combination = c.ToArray();
                if (combination.Length <= successful_combination.Length)
                    continue;
                var new_moves = combination.Concat(Enumerable.Repeat<ushort>(0, 4 - combination.Length)).ToArray();
                blank.ApplySetDetails(set);
                blank.SetMoves(new_moves);
                blank.SetRecordFlags(Array.Empty<ushort>());

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
