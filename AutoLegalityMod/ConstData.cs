namespace AutoLegalityMod
{
    public class ConstData
    {
        public byte[] resetpk7 = new byte[] { 0, 0, 0, 0, 0, 0, 205, 56, 34, 3, 0, 0, 57, 48, 49, 212, 0, 0, 0, 0, 101, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 77, 0, 97, 0, 114, 0, 115, 0, 104, 0, 97, 0, 100, 0, 111, 0, 119, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 35, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 0, 75, 0, 72, 0, 101, 0, 88, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 17, 6, 29, 0, 0, 0, 0, 0, 4, 0, 0, 31, 64, 0, 0, 2, 0, 0, 0, 0, };

        public byte[] resetpk6 = new byte[] { 0, 0, 0, 0, 0, 0, 16, 48, 209, 2, 0, 0, 57, 48, 49, 212, 0, 0, 0, 0, 11, 0, 0, 0, 0, 0, 0, 0, 0, 4, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 86, 0, 111, 0, 108, 0, 99, 0, 97, 0, 110, 0, 105, 0, 111, 0, 110, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 35, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 80, 0, 75, 0, 72, 0, 101, 0, 88, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 18, 3, 7, 0, 0, 0, 0, 0, 4, 0, 0, 26, 64, 0, 0, 2, 0, 0, 0, 0, };

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
