using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;
using PKHeX.Core.AutoMod;

namespace AutoModPlugins
{
    public static class ModLogic
    {
        public static string GetShowdownSetsFromBoxCurrent(this SaveFile sav) => GetShowdownSetsFromBox(sav, sav.CurrentBox);

        public static string GetShowdownSetsFromBox(this SaveFile sav, int box)
        {
            var data = sav.GetBoxData(box);
            var sep = Environment.NewLine + Environment.NewLine;
            return ShowdownSet.GetShowdownSets(data, sep);
        }

        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav)
        {
            var species = Enumerable.Range(1, sav.MaxSpeciesID);
            if (sav.GG)
                species = species.Except(Enumerable.Range(152, 555)); // only <=151 && 708+
            return sav.GenerateLivingDex(species);
        }

        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, params int[] species) =>
            sav.GenerateLivingDex((IEnumerable<int>)species);

        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, IEnumerable<int> speciesIDs)
        {
            foreach (var id in speciesIDs)
            {
                if (GetRandomEncounter(sav, sav, id, out PKM pk))
                    yield return pk;
            }
        }

        public static bool GetRandomEncounter(this SaveFile sav, int species, out PKM pk) => GetRandomEncounter(sav, sav, species, out pk);

        public static bool GetRandomEncounter(SaveFile sav, ITrainerInfo tr, int species, out PKM pk)
        {
            var blank = sav.BlankPKM;
            pk = GetRandomEncounter(blank, tr, species);
            if (pk == null)
                return false;

            pk = PKMConverter.ConvertToType(pk, sav.PKMType, out _);
            return pk != null;
        }

        private static PKM GetRandomEncounter(PKM blank, ITrainerInfo tr, int species)
        {
            blank.Species = species;
            blank.Gender = blank.GetSaneGender();
            if (species == 678)
                blank.AltForm = blank.Gender;

            var f = EncounterMovesetGenerator.GeneratePKMs(blank, tr).FirstOrDefault();
            if (f == null)
                return null;
            int abilityretain = f.AbilityNumber >> 1;
            f.Species = species;
            f.Gender = f.GetSaneGender();
            if (species == 678)
                f.AltForm = f.Gender;
            f.CurrentLevel = 100;
            f.Nickname = PKX.GetSpeciesNameGeneration(f.Species, f.Language, f.Format);
            f.IsNicknamed = false;
            f.SetSuggestedMoves();
            f.AbilityNumber = abilityretain;
            f.RefreshAbility(abilityretain);
            return f;
        }

        public static bool LegalizeBox(this SaveFile sav, int box)
        {
            if ((uint)box >= sav.BoxCount)
                return false;

            var data = sav.GetBoxData(box);
            bool modified = false;
            for (int i = 0; i < 30; i++)
            {
                var pk = data[i];
                if (pk.Species <= 0 || new LegalityAnalysis(pk).Valid)
                    continue;
                data[i] = sav.Legalize(pk);
                modified = true;
            }
            if (!modified)
                return false;
            sav.SetBoxData(data, box);
            return true;
        }

        public static bool LegalizeBoxes(this SaveFile sav)
        {
            bool modified = false;
            for (int i = 0; i < sav.BoxCount; i++)
                modified |= sav.LegalizeBox(i);
            return modified;
        }
    }
}
