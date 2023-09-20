using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKHeX.Core.AutoMod
{
    public class RegenSet
    {
        public static readonly RegenSet Default = new(Array.Empty<string>(), PKX.Generation);

        public RegenSetting Extra { get; }
        public ITrainerInfo? Trainer { get; }
        public StringInstructionSet Batch { get; }
        public IEnumerable<StringInstruction> EncounterFilters { get; }
        public IReadOnlyList<StringInstruction> VersionFilters { get; }

        public readonly bool HasExtraSettings;
        public readonly bool HasTrainerSettings;
        public bool HasBatchSettings => Batch.Filters.Count != 0 || Batch.Instructions.Count != 0;

        public RegenSet(PKM pk)
            : this(Array.Empty<string>(), pk.Format)
        {
            Extra.Ball = (Ball)pk.Ball;
            Extra.ShinyType =
                pk.ShinyXor == 0
                    ? Shiny.AlwaysSquare
                    : pk.IsShiny
                        ? Shiny.AlwaysStar
                        : Shiny.Never;
            if (pk is IAlphaReadOnly { IsAlpha: true })
                Extra.Alpha = true;
        }

        public RegenSet(ICollection<string> lines, int format, Shiny shiny = Shiny.Never)
        {
            var modified = lines.Select(z => z.Replace(">=", "≥").Replace("<=", "≤"));
            Extra = new RegenSetting { ShinyType = shiny };
            HasExtraSettings = Extra.SetRegenSettings(modified);
            HasTrainerSettings = RegenUtil.GetTrainerInfo(modified, format, out var tr);
            Trainer = tr;
            Batch = new StringInstructionSet(modified.ToArray().AsSpan());
            EncounterFilters = RegenUtil.GetEncounterFilters(modified);
            VersionFilters = RegenUtil.GetVersionFilters(modified);
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            if (HasExtraSettings)
                sb.AppendLine(RegenUtil.GetSummary(Extra));
            if (HasTrainerSettings && Trainer != null)
                sb.AppendLine(RegenUtil.GetSummary(Trainer));
            if (HasBatchSettings)
                sb.AppendLine(RegenUtil.GetSummary(Batch));
            if (EncounterFilters.Count() > 0)
                sb.AppendLine(RegenUtil.GetSummary(EncounterFilters));
            if (VersionFilters.Count() > 0)
                sb.AppendLine(RegenUtil.GetSummary(VersionFilters));
            return sb.ToString();
        }
    }
}
