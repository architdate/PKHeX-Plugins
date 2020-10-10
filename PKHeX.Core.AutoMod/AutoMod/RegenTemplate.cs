// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    public sealed class RegenTemplate : IBattleTemplate
    {
        public int Species { get; set; }
        public int Format { get; set; }
        public string Nickname { get; set; }
        public string Gender { get; set; }
        public int HeldItem { get; set; }
        public int Ability { get; set; }
        public int Level { get; set; }
        public bool Shiny { get; set; }
        public int Friendship { get; set; }
        public int Nature { get; set; }
        public string Form { get; set; }
        public int FormIndex { get; set; }
        public int HiddenPowerType { get; set; }
        public bool CanGigantamax { get; set; }

        public int[] EVs { get; }
        public int[] IVs { get; }
        public int[] Moves { get; }

        public Ball Ball { get; set; }
        public Shiny ShinyType { get; set; } = Core.Shiny.Random;
        public LanguageID? Language { get; set; }
        public string OT { get; set; }
        public int TID { get; set; } = 12345;
        public int SID { get; set; } = 54321;
        internal int TID7 { get; set; } = 123456;
        internal int SID7 { get; set; } = 1234;
        public int OT_Gender { get; set; }
        public bool OverrideTrainer { get; set; } = false;

        public RegenSet Regen { get; set; } = RegenSet.Default;

        public string Text { get; private set; } = string.Empty;

        public RegenTemplate(IBattleTemplate set, int gen = PKX.Generation)
        {
            Species = set.Species;
            Format = set.Format;
            Nickname = set.Nickname;
            Gender = set.Gender;
            HeldItem = set.HeldItem;
            Ability = set.Ability;
            Level = set.Level;
            Shiny = set.Shiny;
            Friendship = set.Friendship;
            Nature = set.Nature;
            Form = set.Form;
            FormIndex = set.FormIndex;
            EVs = SanitizeEVs(set.EVs, gen);
            IVs = set.IVs;
            HiddenPowerType = set.HiddenPowerType;
            Moves = set.Moves;
            CanGigantamax = set.CanGigantamax;
        }

        public RegenTemplate(ShowdownSet set, int gen = PKX.Generation) : this((IBattleTemplate) set, gen)
        {
            this.SanitizeForm();
            this.SanitizeBattleMoves();
            if (set.InvalidLines.Count != 0)
                Regen = new RegenSet(set.InvalidLines);
        }

        public RegenTemplate(PKM pk, int gen = PKX.Generation) : this(new ShowdownSet(pk), gen)
        {
            this.FixGender(pk.PersonalInfo);
            var set = new ShowdownSet(pk);
            if (set.InvalidLines.Count != 0)
            {
                this.LoadMetadata(pk);
                GetRegenLines(set.GetSetLines());
            }
        }

        private static int[] SanitizeEVs(int[] evs, int gen)
        {
            var copy = (int[])evs.Clone();
            int maxEV = gen >= 6 ? 252 : gen >= 3 ? 255 : 65535;
            for (int i = 0; i < evs.Length; i++)
            {
                if (copy[i] > maxEV)
                    copy[i] = maxEV;
            }
            return copy;
        }
    }
}
