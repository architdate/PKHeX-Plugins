using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PKHeX.Core.AutoMod
{
    [DebuggerDisplay($"{{{nameof(Identifier)}}}: {{{nameof(Comment)}}}")]
    public readonly record struct ALMTraceback(TracebackType Identifier, string Comment);

    public interface ITracebackHandler
    {
        /// <summary>
        /// Handle traceback when it reaches the handler
        /// </summary>
        /// <param name="traceback">ALMTraceback object</param>
        void Handle(ALMTraceback traceback);

        /// <summary>
        /// Create an ALMTraceback object and handle
        /// </summary>
        /// <param name="ident">TracebackType</param>
        /// <param name="Comment">Comment</param>
        void Handle(TracebackType ident, string Comment);

        /// <summary>
        /// Any output produced by the traceback handler.
        /// Can be used post generation
        /// </summary>
        /// <returns>Output object</returns>
        IEnumerable<ALMTraceback>? Output();

        /// <summary>
        /// Get Handler type being used currently
        /// </summary>
        /// <returns>Handler type</returns>
        HandlerType GetType();
    }

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

    public enum HandlerType : byte
    {
        Disabled,
        Debug,
        Verbose
    }

    public static class HandlerTypeExtensions
    {
        public static ITracebackHandler GetTracebackHandler(this HandlerType ht) =>
            ht switch
            {
                HandlerType.Disabled => new DisabledTBHandler(),
                HandlerType.Debug => new DebugTBHandler(),
                HandlerType.Verbose => new VerboseTBHandler(),
                _ => throw new NotImplementedException("Traceback Handler is not implemented"),
            };
    }

    public class DisabledTBHandler : ITracebackHandler
    {
        public void Handle(ALMTraceback traceback) { }

        public void Handle(TracebackType ident, string Comment) { }

        public IEnumerable<ALMTraceback>? Output() => null;

        HandlerType ITracebackHandler.GetType() => HandlerType.Disabled;
    }

    public class DebugTBHandler : ITracebackHandler
    {
        public void Handle(ALMTraceback traceback)
        {
            Debug.WriteLine(traceback);
        }

        public void Handle(TracebackType ident, string Comment)
        {
            Debug.WriteLine($"{ident}: {Comment}");
        }

        public IEnumerable<ALMTraceback>? Output() => null;

        HandlerType ITracebackHandler.GetType() => HandlerType.Debug;
    }

    public class VerboseTBHandler : ITracebackHandler
    {
        private readonly List<ALMTraceback> tb = [];

        public void Handle(ALMTraceback traceback)
        {
            tb.Add(traceback);
        }

        public void Handle(TracebackType ident, string comment)
        {
            var almtb = new ALMTraceback(ident, comment);
            tb.Add(almtb);
        }

        public IEnumerable<ALMTraceback> Output() => tb;

        HandlerType ITracebackHandler.GetType() => HandlerType.Verbose;
    }
}
