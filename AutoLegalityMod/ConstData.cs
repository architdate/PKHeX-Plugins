namespace AutoLegalityMod
{
    public static class ConstData
    {
        public static string changelog = @"
- Added a JSON for trainerdata with additional features
- Added modes for trainerdata json (key = mode, value = game or save or auto. Defaults to save. game mode allows OT data to be applied to pokemon based on Origin Game. save applies data based only on save game. auto works like auto in trainerdata.txt)
- Another round of legality fixes - Thanks to many people
- Added region based defaults
- Allow MGDB Downloader behaviour to be determined by user
- Add G7TID and G7SID support for trainerdata (6 digits and 4 digits respectively)
- Add Smogon Genning addon to gen smogon sets from smogon.com (Refer Features.md)
- Fix some RNG stuff (Pokemon Box)
- [Disable the Training Wheels Protocol](https://i.imgur.com/qLisJiv.png) (Leverage the generators coded in base PKHeX for faster genning)
- Current base PKHeX commit [e0aa193](https://github.com/kwsch/PKHeX/commit/e0aa1934e7be00955dede723c75dfc555472dc7c)
";

        public static string keyboardshortcuts = @"
- `Ctrl + I` : Auto Legality import from clipboard
- `Shift + Click Import from Auto Legality Mod` : Auto Legality import from a `.txt` file
- `Alt + Q` : PGL QR code genning. Also saves the showdown import to your clipboard!
- `Ctrl + Mass Import` replaces the first Pokemon in the box. Otherwise it sets them in empty places!
";

        public const string discord = "https://discord.gg/9ptDkpV";
    }
}
