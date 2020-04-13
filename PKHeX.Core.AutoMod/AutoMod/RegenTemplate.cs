// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace PKHeX.Core
{
    public sealed class RegenTemplate : IBattleTemplate
    {
        public int Species { get; set; }
        public int Format { get; set; } = PKMConverter.Format;
        public string Nickname { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public int HeldItem { get; set; }
        public int Ability { get; set; } = -1;
        public int Level { get; set; } = 100;
        public bool Shiny { get; set; }
        public int Friendship { get; set; } = 255;
        public int Nature { get; set; } = -1;
        public string Form { get; set; } = string.Empty;
        public int FormIndex { get; set; }
        public int[] EVs { get; set; } = new[] { 00, 00, 00, 00, 00, 00 };
        public int[] IVs { get; set; } = new[] { 31, 31, 31, 31, 31, 31 };
        public int HiddenPowerType { get; set; } = -1;
        public int[] Moves { get; } = new[] { 0, 0, 0, 0 };
        public bool CanGigantamax { get; set; }

        public RegenTemplate(ShowdownSet set)
        {
            Species = set.Species;
        }
    }
}
