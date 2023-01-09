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
                    case "TID" when ushort.TryParse(value, out ushort tid) && tid >= 0:
                        sti.TID16 = tid;
                        break;
                    case "SID" when ushort.TryParse(value, out ushort sid) && sid >= 0:
                        sti.SID16 = sid;
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
            uint repack = ((uint)sti.SID16 * mil) + sti.TID16;
            sti.TID16 = (ushort)(repack & 0xFFFF);
            sti.SID16 = (ushort)(repack >> 16);
            return true;
        }

        private const char Splitter = ':';
        public const char EncounterFilterPrefix = '~';

        public static IEnumerable<StringInstruction>? GetEncounterFilters(IEnumerable<string> lines)
        {
            var valid = lines.Where(z => z.StartsWith(EncounterFilterPrefix.ToString())).ToList();
            if (valid.Count == 0)
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

                var key = line[..index];
                var value = line.Substring(index + 1, line.Length - key.Length - 1).Trim();
                yield return new KeyValuePair<string, string>(key, value);
            }
        }

        public static string GetSummary(RegenSetting extra) => extra.GetSummary();

        public static string GetSummary(ITrainerInfo trainer)
        {
            var tid = trainer.TID16;
            var sid = trainer.SID16;
            if (trainer.Generation >= 7)
            {
                const int mil = 1_000_000;
                uint repack = ((uint)sid << 16) + (uint)tid;
                tid = (ushort)(repack % mil);
                sid = (ushort)(repack / mil);
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
                result.Add($"{StringInstruction.Prefixes[(int)s.Comparer]}{s.PropertyName}={s.PropertyValue}");
            foreach (var s in set.Instructions)
                result.Add($".{s.PropertyName}={s.PropertyValue}");
            return string.Join(Environment.NewLine, result);
        }

        public static string GetSummary(IEnumerable<StringInstruction> filters, char prefix = EncounterFilterPrefix)
        {
            var result = new List<string>();
            foreach (var s in filters)
                result.Add($"{prefix}{StringInstruction.Prefixes[(int)s.Comparer]}{s.PropertyName}={s.PropertyValue}");
            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// Clone trainerdata and mutate the language and then return the clone
        /// </summary>
        /// <param name="tr">Trainerdata to clone</param>
        /// <param name="lang">language to mutate</param>
        /// <param name="ver"></param>
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
                var version = Array.Find(GameUtil.GameVersions, z => ver.Contains(z) && z != GameVersion.BU);
                return new SimpleTrainerInfo(version)
                {
                    OT = MutateOT(s.OT, lang, version),
                    TID16 = s.TID16,
                    SID16 = s.SID16,
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
            if (lang == null)
                return OT;
            var max = Legal.GetMaxLengthOT(game.GetGeneration(), (LanguageID)lang);
            OT = OT[..Math.Min(OT.Length, max)];
            if (GameVersion.GG.Contains(game) || game.GetGeneration() >= 8) // switch keyboard only has latin characters, --don't mutate
                return OT;
            var full = lang is LanguageID.Japanese or LanguageID.Korean or LanguageID.ChineseS or LanguageID.ChineseT;
            if (full && GlyphLegality.ContainsHalfWidth(OT))
                return GlyphLegality.StringConvert(OT, StringConversionType.FullWidth);
            if (!full && GlyphLegality.ContainsFullWidth(OT))
                return GlyphLegality.StringConvert(OT, StringConversionType.HalfWidth);
            return OT;
        }

        public static string MutateNickname(string nick, LanguageID? lang, GameVersion game)
        {
            // Length checks are handled later in SetSpeciesLevel
            if (game.GetGeneration() >= 8 || lang == null)
                return nick;
            var full = lang is LanguageID.Japanese or LanguageID.Korean or LanguageID.ChineseS or LanguageID.ChineseT;
            return full switch
            {
                true when GlyphLegality.ContainsHalfWidth(nick) => GlyphLegality.StringConvert(nick, StringConversionType.FullWidth),
                false when GlyphLegality.ContainsFullWidth(nick) => GlyphLegality.StringConvert(nick, StringConversionType.HalfWidth),
                _ => nick,
            };
        }

        public static int GetRegenAbility(int species, int gen, AbilityRequest ar)
        {
            var pi = GameData.GetPersonal(GetGameVersionFromGen(gen))[species];
            var abils_ct = pi.AbilityCount;
            if (pi is not IPersonalAbility12 a)
                return -1;
            return ar switch
            {
                AbilityRequest.Any => -1,
                AbilityRequest.First => a.Ability1,
                AbilityRequest.Second => a.Ability2,
                AbilityRequest.NotHidden => a.Ability1,
                AbilityRequest.PossiblyHidden => a.Ability1,
                AbilityRequest.Hidden => abils_ct > 2 && pi is IPersonalAbility12H h ? h.AbilityH : -1,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static GameVersion GetGameVersionFromGen(int gen) => gen switch
        {
            1 => GameVersion.RB,
            2 => GameVersion.C,
            3 => GameVersion.E,
            4 => GameVersion.Pt,
            5 => GameVersion.B2W2,
            6 => GameVersion.ORAS,
            7 => GameVersion.USUM,
            8 => GameVersion.SWSH,
            9 => GameVersion.SV,
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
