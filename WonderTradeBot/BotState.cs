namespace WonderTradeBot
{
    /// <summary>
    /// Secuency of steps done by the bot.
    /// </summary>
    public enum BotState
    {
        StartBot,
        BackupBoxes,
        CheckMode,
        InitializeFC1,
        InitializeFC2,
        ReadPoke,
        ReadFolder,
        WriteFromFolder,
        WriteLastBox,
        PressTradeButton,
        TestTradeMenu,
        PressWTButton,
        TestWTScreen,
        PressWTstart,
        TestBoxes,
        TouchPoke,
        CancelTouch,
        TestPoke,
        StartTrade,
        ConfirmTrade,
        TestBoxesOut,
        WaitForTrade,
        TestTradeFinish,
        TryFinish,
        FinishTrade,
        CollectFC1,
        CollectFC2,
        CollectFC3,
        CollectFC4,
        CollectFC5,
        DumpAfter,
        ActionAfter,
        RestoreBackup,
        DeletePoke,
        ExitBot
    };
}