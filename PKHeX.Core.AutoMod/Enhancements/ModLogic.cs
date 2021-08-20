using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core.AutoMod
{
    /// <summary>
    /// Miscellaneous enhancement methods
    /// </summary>
    public static class ModLogic
    {
        /// <summary>
        /// Exports the <see cref="SaveFile.CurrentBox"/> to <see cref="ShowdownSet"/> as a single string.
        /// </summary>
        /// <param name="provider">Save File to export from</param>
        /// <returns>Concatenated string of all sets in the current box.</returns>
        public static string GetRegenSetsFromBoxCurrent(this ISaveFileProvider provider) => GetRegenSetsFromBox(provider.SAV, provider.CurrentBox);

        /// <summary>
        /// Exports the <see cref="box"/> to <see cref="ShowdownSet"/> as a single string.
        /// </summary>
        /// <param name="sav">Save File to export from</param>
        /// <param name="box">Box to export from</param>
        /// <returns>Concatenated string of all sets in the specified box.</returns>
        public static string GetRegenSetsFromBox(this SaveFile sav, int box)
        {
            var data = sav.GetBoxData(box);
            var sep = Environment.NewLine + Environment.NewLine;
            return data.GetRegenSets(sep);
        }

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav)
        {
            var species = Enumerable.Range(1, sav.MaxSpeciesID);
            if (sav is SAV7b)
                species = species.Where(z => z is <= 151 or 808 or 809); // only include Kanto and M&M
            if (sav is SAV8)
                species = species.Where(z => ((PersonalInfoSWSH)PersonalTable.SWSH.GetFormEntry(z, 0)).IsPresentInGame || SimpleEdits.Zukan8Additions.Contains(z));
            return sav.GenerateLivingDex(species, includeforms: false);
        }

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="speciesIDs">Species IDs to generate</param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, params int[] speciesIDs) =>
            sav.GenerateLivingDex(speciesIDs, includeforms: true);

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="speciesIDs">Species IDs to generate</param>
        /// <param name="includeforms">Include all forms in the resulting list of data</param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, IEnumerable<int> speciesIDs, bool includeforms = true)
        {
            var tr = APILegality.UseTrainerData ? TrainerSettings.GetSavedTrainerData(sav.Version, sav.Generation, fallback: sav, lang: (LanguageID)sav.Language) : sav;
            var pt = sav.Personal;
            List<PKM> pklist = new();
            foreach (var id in speciesIDs)
            {
                if (!includeforms)
                {
                    AddPKM(sav, tr, pklist, id, null);
                    continue;
                }
                var num_forms = pt[id].FormCount;
                for (int i = 0; i < num_forms; i++)
                {
                    if (sav is SAV8 && !((PersonalInfoSWSH)pt.GetFormEntry(id, i)).IsPresentInGame)
                        continue;
                    AddPKM(sav, tr, pklist, id, i);
                }
            }
            return pklist;
        }

        private static void AddPKM(SaveFile sav, ITrainerInfo tr, List<PKM> pklist, int species, int? form)
        {
            if (tr.GetRandomEncounter(species, form, out var pk) && pk != null)
            {
                pk.Heal();
                pklist.Add(pk);
            }
            else if (sav is SAV2 && GetRandomEncounter(new SAV1(GameVersion.Y) { Language = tr.Language, OT = tr.OT, TID = tr.TID }, species, 0, out var pkm) && pkm is PK1 pk1)
            {
                pklist.Add(pk1.ConvertToPK2());
            }
        }

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(this SaveFile sav, int species, int? form, out PKM? pk) => ((ITrainerInfo)sav).GetRandomEncounter(species, form, out pk);

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(this ITrainerInfo tr, int species, int? form, out PKM? pk)
        {
            var blank = PKMConverter.GetBlank(tr.Generation, tr.Game);
            pk = GetRandomEncounter(blank, tr, species, form);
            if (pk == null)
                return false;

            pk = PKMConverter.ConvertToType(pk, blank.GetType(), out _);
            return pk != null;
        }

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="blank">Template data that will have its properties modified</param>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <returns>Result legal pkm, null if data should be ignored.</returns>
        private static PKM? GetRandomEncounter(PKM blank, ITrainerInfo tr, int species, int? form)
        {
            blank.Species = species;
            blank.Gender = blank.GetSaneGender();
            if (species is ((int)Species.Meowstic) or ((int)Species.Indeedee))
            {
                if (form == null)
                    blank.Form = blank.Gender;
                else
                    blank.Gender = (int)form;
            }

            var legalencs = EncounterMovesetGenerator.GeneratePKMs(blank, tr).Where(z => new LegalityAnalysis(z).Valid);
            var firstenc = GetFirstEncounter(legalencs, form);
            if (firstenc == null)
                return null;

            var f = PKMConverter.ConvertToType(firstenc, blank.GetType(), out _);
            if (f == null || (form != null && f.Form != (int)form))
            {
                var template = PKMConverter.GetBlank(tr.Generation, (GameVersion)tr.Game);
                if (form != null)
                    blank.Form = (int)form;
                var set = new ShowdownSet(new ShowdownSet(blank).Text.Split('\r')[0]);
                template.ApplySetDetails(set);
                var success = tr.TryAPIConvert(set, template, out PKM pk);
                return success == LegalizationResult.Regenerated ? pk : null;
            }
            var an = f.AbilityNumber;
            f.Species = species;
            f.Gender = f.GetSaneGender();
            if (species is ((int)Species.Meowstic) or ((int)Species.Indeedee))
                f.Form = f.Gender;
            f.CurrentLevel = 100;
            f.Nickname = SpeciesName.GetSpeciesNameGeneration(f.Species, f.Language, f.Format);
            f.IsNicknamed = false;
            f.SetSuggestedMoves();
            f.SetSuggestedMovePP(0);
            f.SetSuggestedMovePP(1);
            f.SetSuggestedMovePP(2);
            f.SetSuggestedMovePP(3);
            f.RefreshAbility(an >> 1);
            var info = new LegalityAnalysis(f).Info;
            if (info.Generation > 0 && info.EvoChainsAllGens[info.Generation].All(z => z.Species != info.EncounterMatch.Species))
            {
                f.CurrentHandler = 1;
                f.HT_Name = f.OT_Name;
                if (f is IHandlerLanguage h)
                    h.HT_Language = 1;
            }
            if (f is IFormArgument)
                f.SetSuggestedFormArgument(info.EncounterMatch.Species);
            int wIndex = WurmpleUtil.GetWurmpleEvoGroup(f.Species);
            if (wIndex != -1)
                f.EncryptionConstant = WurmpleUtil.GetWurmpleEncryptionConstant(wIndex);
            if (f is IHomeTrack { Tracker: 0 } ht && APILegality.SetRandomTracker)
                ht.Tracker = APILegality.GetRandomULong();
            if (new LegalityAnalysis(f).Valid)
                return f;

            // local name clashes!
            {
                var template = PKMConverter.GetBlank(tr.Generation, (GameVersion)tr.Game);
                var set = new ShowdownSet(new ShowdownSet(blank).Text.Split('\r')[0]);
                template.ApplySetDetails(set);
                var success = tr.TryAPIConvert(set, template, out PKM pk);
                return success == LegalizationResult.Regenerated ? pk : null;
            }
        }

        private static PKM? GetFirstEncounter(IEnumerable<PKM> legalencs, int? form)
        {
            if (form == null)
                return legalencs.FirstOrDefault();

            PKM? result = null;
            foreach (var pk in legalencs)
            {
                if (pk.Form == form)
                    return pk;
                result ??= pk;
            }
            return result;
        }

        /// <summary>
        /// Legalizes all <see cref="PKM"/> in the specified <see cref="box"/>.
        /// </summary>
        /// <param name="sav">Save File to legalize</param>
        /// <param name="box">Box to legalize</param>
        /// <returns>Count of Pokémon that are now legal.</returns>
        public static int LegalizeBox(this SaveFile sav, int box)
        {
            if ((uint)box >= sav.BoxCount)
                return -1;

            var data = sav.GetBoxData(box);
            var ctr = sav.LegalizeAll(data);
            if (ctr > 0)
                sav.SetBoxData(data, box);
            return ctr;
        }

        /// <summary>
        /// Legalizes all <see cref="PKM"/> in all boxes.
        /// </summary>
        /// <param name="sav">Save File to legalize</param>
        /// <returns>Count of Pokémon that are now legal.</returns>
        public static int LegalizeBoxes(this SaveFile sav)
        {
            if (!sav.HasBox)
                return -1;
            var ctr = 0;
            for (int i = 0; i < sav.BoxCount; i++)
            {
                var result = sav.LegalizeBox(i);
                if (result < 0)
                    return result;
                ctr += result;
            }
            return ctr;
        }

        /// <summary>
        /// Legalizes all <see cref="PKM"/> in the provided <see cref="data"/>.
        /// </summary>
        /// <param name="sav">Save File context to legalize with</param>
        /// <param name="data">Data to legalize</param>
        /// <returns>Count of Pokémon that are now legal.</returns>
        public static int LegalizeAll(this SaveFile sav, IList<PKM> data)
        {
            var ctr = 0;
            for (int i = 0; i < data.Count; i++)
            {
                var pk = data[i];
                if (pk.Species <= 0 || new LegalityAnalysis(pk).Valid)
                    continue;

                var result = sav.Legalize(pk);
                result.Heal();
                if (!new LegalityAnalysis(result).Valid)
                    continue; // failed to legalize

                data[i] = result;
                ctr++;
            }

            return ctr;
        }
    }
}
