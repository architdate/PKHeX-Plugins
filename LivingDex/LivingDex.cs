using PKHeX.Core;
using System;
using System.Linq;
using System.Windows.Forms;
using AutoLegalityMod;

namespace LivingDex
{
    public class LivingDex : AutoModPlugin
    {
        public override string Name => "Generate Living Dex";
        public override int Priority => 1;

        protected override void AddPluginControl(ToolStripDropDownItem modmenu)
        {
            var ctrl = new ToolStripMenuItem(Name);
            modmenu.DropDownItems.Add(ctrl);
            ctrl.Click += GenLivingDex;
            ctrl.Image = LivingDexResources.livingdex;
        }

        private void GenLivingDex(object sender, EventArgs e)
        {
            var bd = SaveFileEditor.SAV.BoxData;

            var tr = SaveFileEditor.SAV;
            for (int i = 1; i <= tr.MaxSpeciesID; i++)
            {
                var pk = SaveFileEditor.SAV.BlankPKM;
                pk.Species = i;
                pk.Gender = pk.GetSaneGender();
                if (i == 678)
                    pk.AltForm = pk.Gender;
                var f = EncounterMovesetGenerator.GeneratePKMs(pk, tr).FirstOrDefault();
                if (f != null)
                {
                    int abilityretain = f.AbilityNumber >> 1;
                    f.Species = i;
                    f.Gender = f.GetSaneGender();
                    if (i == 678)
                        f.AltForm = f.Gender;
                    f.CurrentLevel = 100;
                    f.Nickname = PKX.GetSpeciesNameGeneration(f.Species, f.Language, SaveFileEditor.SAV.Generation);
                    f.IsNicknamed = false;
                    SetSuggestedMoves(f);
                    f.AbilityNumber = abilityretain;
                    f.RefreshAbility(abilityretain);
                    bd[i] = PKMConverter.ConvertToType(f, SaveFileEditor.SAV.PKMType, out _);
                }
            }
            SaveFileEditor.SAV.BoxData = bd;
            SaveFileEditor.ReloadSlots();
        }

        private void SetSuggestedMoves(PKM pkm, bool random = false)
        {
            int[] m = pkm.GetMoveSet(random);
            if (m?.Any(z => z != 0) != true)
            {
                return;
            }

            if (pkm.Moves.SequenceEqual(m))
                return;

            pkm.SetMoves(m);
        }
    }
}
