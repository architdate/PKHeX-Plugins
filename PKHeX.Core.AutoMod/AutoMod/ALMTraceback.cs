namespace PKHeX.Core.AutoMod
{
    [System.Diagnostics.DebuggerDisplay($"{{{nameof(Identifier)}}}: {{{nameof(Comment)}}}")]
    public readonly record struct ALMTraceback(TracebackType Identifier, string Comment);

    public enum TracebackType : byte
    {
        Encounter,
        Trainer,
        PID_IV,
        EC,
        Species,
        Level,
        Shiny,
        Gender,
        Form,
        Nature,
        Ability,
        Item,
        Moves,
        EVs,
        AVs,
        Size,
        HyperTrain,
        Friendship,
        Misc
    }
}
