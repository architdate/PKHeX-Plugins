namespace WonderTradeBot
{
    /// <summary>
    /// All different error or finisish messages the bot can return.
    /// </summary>
    public enum BotErrorMessage
    {
        Finished,
        UserStop,
        ReadError,
        WriteError,
        ButtonError,
        TouchError,
        StickError,
        NotInPSS,
        FestivalPlaza,
        SVMatch,
        FilterMatch,
        NoMatch,
        SRMatch,
        BattleMatch,
        Disconnect,
        NotWTMenu,
        GeneralError
    }

    public static partial class Extensions
    {
        public static string FormatString(this BotErrorMessage message, int[] info)
        {
            switch (message)
            {
                case BotErrorMessage.Finished:
                    return "Bot finished sucessfully.";
                case BotErrorMessage.UserStop:
                    return "Bot stopped by the user.";
                case BotErrorMessage.ReadError:
                    return "An error ocurred while reading data from the 3DS RAM.";
                case BotErrorMessage.WriteError:
                    return "An error ocurred while writting data to the 3DS RAM.";
                case BotErrorMessage.ButtonError:
                    return "An error ocurred while sending Button commands to the 3DS.";
                case BotErrorMessage.TouchError:
                    return "An error ocurred while sending Touch Screen commands to the 3DS.";
                case BotErrorMessage.StickError:
                    return "An error ocurred while sending Control Stick commands to the 3DS.";
                case BotErrorMessage.NotInPSS:
                    return "Please go to the PSS menu and try again.";
                case BotErrorMessage.FestivalPlaza:
                    return "Bot finished due level-up in Festival Plaza.";
                case BotErrorMessage.SVMatch:
                    return $"Finished. A match was found at box {info[0]}, slot{info[1]} with the ESV/TSV value: {info[2]}.";
                case BotErrorMessage.FilterMatch:
                    return $"Finished. A match was found at box {info[0]}, slot {info[1]} using filter #{info[2]}.";
                case BotErrorMessage.NoMatch:
                    return "Bot finished sucessfuly without finding a match for the current settings.";
                case BotErrorMessage.SRMatch:
                    return $"Finished. The current pokémon matched filter #{info[0]} after {info[1]} soft-resets.";
                case BotErrorMessage.BattleMatch:
                    return $"Finished. The current pokémon matched filter #{info[0]} after {info[1]} battles.";

                case BotErrorMessage.Disconnect:
                    return "Connection with the 3DS was lost.";
                case BotErrorMessage.NotWTMenu:
                    return "Please, go to the Wonder trade screen and try again.";
                case BotErrorMessage.GeneralError:
                    return "A error has ocurred, see log for detals.";
                default:
                    return "An unknown error has ocurred, please keep the log and report this error.";
            }
        }
    }
}