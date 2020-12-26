using System;
using System.Collections.Generic;

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
                        sti.Gender = value == "Female" || value == "F" ? 1 : 0;
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
            return any;
        }

        private const char Splitter = ':';

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
                $"SID: {sid}"
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

        /// <summary>
        /// Clone trainerdata and mutate the language and then return the clone
        /// </summary>
        /// <param name="tr">Trainerdata to clone</param>
        /// <param name="lang">language to mutate</param>
        /// <returns></returns>
        public static ITrainerInfo MutateLanguage(this ITrainerInfo tr, LanguageID? lang)
        {
            if (lang == LanguageID.UNUSED_6 || lang == LanguageID.Hacked || lang == null)
                return tr;
            if (tr is PokeTrainerDetails p)
            {
                var clone = PokeTrainerDetails.Clone(p);
                clone.Language = (int)lang;
                return clone;
            }
            if (tr is SimpleTrainerInfo s)
            {
                return new SimpleTrainerInfo((GameVersion)s.Game)
                {
                    OT = s.OT,
                    TID = s.TID,
                    SID = s.SID,
                    Gender = s.Gender,
                    Language = (int)lang,
                    ConsoleRegion = s.ConsoleRegion,
                    Region = s.Region,
                    Country = s.Country,
                    Generation = s.Generation
                };
            }
            return tr;
        }
    }
}
