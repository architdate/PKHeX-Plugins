using System.Collections;
using System.Collections.Generic;

namespace PKHeX.Core.AutoMod
{
    public static class RegenUtil
    {
        public static SimpleTrainerInfo? GetTrainerInfo(IEnumerable<string> lines)
        {

        }
    }

    public class RegenSetting
    {
        public Ball Ball { get; set; } = Ball.None;
        public Shiny ShinyType { get; set; } = Core.Shiny.Random;
        public LanguageID? Language { get; set; }

        public bool SetRegenSettings(IEnumerable<string> lines)
        {

        }
    }

    public class RegenExtras
    {

    }

    public class RegenSet
    {
        public RegenSetting Extra { get; set; }
        public SimpleTrainerInfo? Trainer { get; set; }
        public StringInstructionSet Batch { get; set; }

        public readonly bool HasExtraSettings;
        public bool HasTrainerSettings => Trainer != null;
        public bool HasBatchSettings => Batch.Filters.Count != 0 || Batch.Instructions.Count != 0;

        public RegenSet(IList<string> lines)
        {
            Extra = new RegenSetting();
            HasSettings = Extra.SetRegenSettings(lines);
            Trainer = RegenUtil.GetSettings(lines);
            Batch = new StringInstructionSet(lines);
        }
    }
}
