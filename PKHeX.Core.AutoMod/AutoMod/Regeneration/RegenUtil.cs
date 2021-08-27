using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public static class RegenUtil
    {
        public static bool GetTrainerInfo(IEnumerable<string> lines, int format, out ITrainerInfo tr)
        {
            var sti = new SimpleTrainerInfo { Generation = format };

            var split = Split(lines);
            bool any = false;
            foreach (var s in split)
            {
                var key = s.Key;
                var value = s.Value;
                switch (key)
                {
                    case "OT":
                        sti.OT = value;
                        break;
                    case "TID" when int.TryParse(value, out int tid) && tid > 0:
                        sti.TID = tid;
                        break;
                    case "SID" when int.TryParse(value, out int sid) && sid > 0:
                        sti.SID = sid;
                        break;
                    case "OTGender":
                        sti.Gender = value is "Female" or "F" ? 1 : 0;
                        break;
                    default:
                        continue;
                }

                any = true;
            }
            tr = sti;
            if (!any || format < 7)
                return any;
            const int mil = 1_000_000;
            uint repack = ((uint)sti.SID * mil) + (uint)sti.TID;
            sti.TID = (int)(repack & 0xFFFF);
            sti.SID = (int)(repack >> 16);
            return true;
        }

        private const char Splitter = ':';
        public const char EncounterFilterPrefix = '~';

        public static IEnumerable<StringInstruction>? GetEncounterFilters(IEnumerable<string> lines)
        {
            var valid = lines.Where(z => z.StartsWith(EncounterFilterPrefix.ToString()));
            if (valid.Count() == 0)
                return null;
            var cleaned = valid.Select(z => z.TrimStart(EncounterFilterPrefix));
            var filters = StringInstruction.GetFilters(cleaned).ToArray();
            BatchEditing.ScreenStrings(filters);
            return filters;
        }

        public static IEnumerable<KeyValuePair<string, string>> Split(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var index = line.IndexOf(Splitter);
                if (index < 0)
                    continue;

                var key = line.Substring(0, index);
                var value = line.Substring(index + 1, line.Length - key.Length - 1).Trim();
                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        public static string GetSummary(RegenSetting extra) => extra.GetSummary();

        public static string GetSummary(ITrainerInfo trainer)
        {
            var tid = trainer.TID;
            var sid = trainer.SID;
            if (trainer.Generation >= 7)
            {
                const int mil = 1_000_000;
                uint repack = ((uint)sid << 16) + (uint)tid;
                tid = (int)(repack % mil);
                sid = (int)(repack / mil);
            }

            var result = new[]
            {
                $"OT: {trainer.OT}",
                $"OTGender: {(trainer.Gender == 1 ? "Female" : "Male")}",
                $"TID: {tid}",
                $"SID: {sid}",
            };
            return string.Join(Environment.NewLine, result);
        }

        public static string GetSummary(StringInstructionSet set)
        {
            var result = new List<string>();
            foreach (var s in set.Filters)
                result.Add($"{(s.Evaluator ? "=" : "!")}{s.PropertyName}={s.PropertyValue}");
            foreach (var s in set.Instructions)
                result.Add($".{s.PropertyName}={s.PropertyValue}");
            return string.Join(Environment.NewLine, result);
        }

        public static string GetSummary(IEnumerable<StringInstruction> filters, char prefix = EncounterFilterPrefix)
        {
            var result = new List<string>();
            foreach (var s in filters)
                result.Add($"{prefix}{(s.Evaluator ? "=" : "!")}{s.PropertyName}={s.PropertyValue}");
            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// Clone trainerdata and mutate the language and then return the clone
        /// </summary>
        /// <param name="tr">Trainerdata to clone</param>
        /// <param name="lang">language to mutate</param>
        /// <returns></returns>
        public static ITrainerInfo MutateLanguage(this ITrainerInfo tr, LanguageID? lang, GameVersion ver)
        {
            if (lang is LanguageID.UNUSED_6 or LanguageID.Hacked or null)
                return tr;
            if (tr is PokeTrainerDetails p)
            {
                var clone = PokeTrainerDetails.Clone(p);
                clone.Language = (int)lang;
                clone.OT = MutateOT(clone.OT, lang, ver);
                return clone;
            }
            if (tr is SimpleTrainerInfo s)
            {
                var version = GameUtil.GameVersions.FirstOrDefault(z => ver.Contains(z) && z != GameVersion.BU);
                return new SimpleTrainerInfo(version)
                {
                    OT = MutateOT(s.OT, lang, version),
                    TID = s.TID,
                    SID = s.SID,
                    Gender = s.Gender,
                    Language = (int)lang,
                    ConsoleRegion = s.ConsoleRegion != 0 ? s.ConsoleRegion : (byte)1,
                    Region = s.Region != 0 ? s.Region : (byte)7,
                    Country = s.Country != 0 ? s.Country : (byte)49,
                    Generation = s.Generation,
                };
            }
            return tr;
        }

        private static string MutateOT(string OT, LanguageID? lang, GameVersion game)
        {
            if (game.GetGeneration() >= 8 || lang == null)
                return OT;
            var full = lang == LanguageID.Japanese || lang == LanguageID.Korean || lang == LanguageID.ChineseS || lang == LanguageID.ChineseT;
            if (full && GlyphLegality.ContainsHalfWidth(OT))
            {
                var max = Legal.GetMaxLengthOT(game.GetGeneration(), (LanguageID)lang);
                var modified = GlyphLegality.StringConvert(OT, StringConversionType.FullWidth);
                return modified.Substring(0, Math.Min(modified.Length, max));
            }
            if (!full && GlyphLegality.ContainsFullWidth(OT))
            {
                var max = Legal.GetMaxLengthOT(game.GetGeneration(), (LanguageID)lang);
                var modified = GlyphLegality.StringConvert(OT, StringConversionType.HalfWidth);
                return modified.Substring(0, Math.Min(modified.Length, max));
            }
            return OT;
        }

        public static string MutateNickname(string nick, LanguageID? lang, GameVersion game)
        {
            // Length checks are handled later in SetSpeciesLevel
            if (game.GetGeneration() >= 8 || lang == null)
                return nick;
            var full = lang == LanguageID.Japanese || lang == LanguageID.Korean || lang == LanguageID.ChineseS || lang == LanguageID.ChineseT;
            if (full && GlyphLegality.ContainsHalfWidth(nick))
                return GlyphLegality.StringConvert(nick, StringConversionType.FullWidth);
            if (!full && GlyphLegality.ContainsFullWidth(nick))
                return GlyphLegality.StringConvert(nick, StringConversionType.HalfWidth);
            return nick;
        }

        public static int GetRegenAbility(int species, int gen, AbilityRequest ar)
        {
            var abils = GetInfo(gen)[species].Abilities;
            return ar switch
            {
                AbilityRequest.Any => -1,
                AbilityRequest.First => abils[0],
                AbilityRequest.Second => abils[1],
                AbilityRequest.NotHidden => abils[0],
                AbilityRequest.PossiblyHidden => abils[0],
                AbilityRequest.Hidden => abils.Count > 2 ? abils[2] : -1,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static PersonalTable GetInfo(int gen)
        {
            return gen switch
            {
                1 => PersonalTable.RB,
                2 => PersonalTable.C,
                3 => PersonalTable.E,
                4 => PersonalTable.Pt,
                5 => PersonalTable.B2W2,
                6 => PersonalTable.AO,
                7 => PersonalTable.USUM,
                8 => PersonalTable.SWSH,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
