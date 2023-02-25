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
        // Living Dex Settings
        public static bool IncludeForms { get; set; }
        public static bool SetShiny { get; set; }
        public static bool SetAlpha { get; set; }

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
        /// <param name="attempts"></param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, out int attempts)
        {
            var species = Enumerable.Range(1, sav.MaxSpeciesID).Where(z => sav.Personal.IsSpeciesInGame((ushort)z)).Select(z => (ushort)z);
            return sav.GenerateLivingDex(species, includeforms: IncludeForms, shiny: SetShiny, alpha: SetAlpha, out attempts);
        }

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="speciesIDs">Species IDs to generate</param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, params ushort[] speciesIDs) =>
            sav.GenerateLivingDex(speciesIDs, includeforms: IncludeForms, shiny: SetShiny, alpha: SetAlpha, out _);

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="speciesIDs">Species IDs to generate</param>
        /// <param name="includeforms">Include all forms in the resulting list of data</param>
        /// <param name="shiny"></param>
        /// <param name="alpha"></param>
        /// <param name="attempts"></param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, IEnumerable<ushort> speciesIDs, bool includeforms, bool shiny, bool alpha, out int attempts)
        {
            attempts = 0;
            var tr = APILegality.UseTrainerData ? TrainerSettings.GetSavedTrainerData(sav.Version, sav.Generation, fallback: sav, lang: (LanguageID)sav.Language) : sav;
            var pt = sav.Personal;
            List<PKM> pklist = new();
            foreach (var id in speciesIDs)
            {
                var num_forms = pt[id].FormCount;
                var count = pklist.Count;
                for (byte i = 0; i < num_forms; i++)
                {
                    if (!sav.Personal.IsPresentInGame(id, i) || FormInfo.IsLordForm(id, i, sav.Context) || FormInfo.IsBattleOnlyForm(id, i, sav.Generation)
                        || FormInfo.IsFusedForm(id, i, sav.Generation) || (FormInfo.IsTotemForm(id, i) && sav.Context is not EntityContext.Gen7))
                        continue;

                    var pk = AddPKM(sav, tr, id, i, shiny, alpha);
                    if (pk is not null)
                    {
                        attempts++;
                        pklist.Add(pk);
                    }

                    if (!includeforms && pk is not null)
                        break;
                }
            }
            return pklist;
        }

        private static PKM? AddPKM(SaveFile sav, ITrainerInfo tr, ushort species, byte? form, bool shiny, bool alpha)
        {
            if (tr.GetRandomEncounter(species, form, shiny, alpha, out var pk) && pk is not null && pk.Species > 0)
            {
                pk.Heal();
                return pk;
            }
            
            if (sav is SAV2 && GetRandomEncounter(new SAV1(GameVersion.Y) { Language = tr.Language, OT = tr.OT, TID16 = tr.TID16 }, species, 0, shiny, false, out var pkm) && pkm is PK1 pk1)
                return pk1;

            return null;
        }

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="alpha"></param>
        /// <param name="attempt"></param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(this SaveFile sav, ushort species, byte? form, bool shiny, bool alpha, out PKM? pk) => ((ITrainerInfo)sav).GetRandomEncounter(species, form, shiny, alpha, out pk);

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="alpha"></param>
        /// <param name="attempt"></param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(this ITrainerInfo tr, ushort species, byte? form, bool shiny, bool alpha, out PKM? pk)
        {
            var blank = EntityBlank.GetBlank(tr);
            pk = GetRandomEncounter(blank, tr, species, form, shiny, alpha);
            if (pk is null)
                return false;

            pk = EntityConverter.ConvertToType(pk, blank.GetType(), out _);
            return pk is not null;
        }

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="blank">Template data that will have its properties modified</param>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="alpha"></param>
        /// <param name="attempt"></param>
        /// <returns>Result legal pkm, null if data should be ignored.</returns>
        private static PKM? GetRandomEncounter(PKM blank, ITrainerInfo tr, ushort species, byte? form, bool shiny, bool alpha)
        {
            blank.Species = species;
            blank.Gender = blank.GetSaneGender();
            if (species is ((ushort)Species.Meowstic) or ((ushort)Species.Indeedee))
            {
                if (form is null)
                    blank.Form = (byte)blank.Gender;
                else
                    blank.Gender = (int)form;
            }

            var template = EntityBlank.GetBlank(tr.Generation, (GameVersion)tr.Game);
            if (form is null)
            {
                var f = GetAvailableForm(blank);
                if (f == -1)
                    return null;

                blank.Form = (byte)f;
            }
            else blank.Form = (byte)form;

            var item = GetFormSpecificItem(tr.Game, blank.Species, blank.Form);
            if (item is not null)
                blank.HeldItem = (int)item;

            if (blank.Species == (ushort)Species.Keldeo && blank.Form == 1)
                blank.Move1 = (ushort)Move.SecretSword;

            if (blank.GetIsFormInvalid(tr, blank.Form))
                return null;

            var setText = new ShowdownSet(blank).Text.Split('\r')[0];
            if (shiny && !SimpleEdits.IsShinyLockedSpeciesForm(blank.Species, blank.Form))
                setText += Environment.NewLine + "Shiny: Yes";

            if (template is IAlphaReadOnly && alpha)
                setText += Environment.NewLine + "Alpha: Yes";

            var sset = new ShowdownSet(setText);
            var set = new RegenTemplate(sset) { Nickname = string.Empty };
            template.ApplySetDetails(set);

            var success = tr.TryAPIConvert(set, template, out PKM pk);
            if (success == LegalizationResult.Regenerated && pk.Form == blank.Form)
                return pk;

            // just get a legal pkm and return. Only validate form and not shininess or alpha.
            var legalencs = EncounterMovesetGenerator.GeneratePKMs(blank, tr, blank.Moves).Where(z => new LegalityAnalysis(z).Valid);
            var firstenc = GetFirstEncounter(legalencs, blank.Form);
            if (firstenc is null)
                return null;

            var originspecies = firstenc.Species;
            if (originspecies != blank.Species)
            {
                firstenc.Species = blank.Species;
                firstenc.CurrentLevel = 100;
                if (!firstenc.IsNicknamed)
                    firstenc.Nickname = SpeciesName.GetSpeciesNameGeneration(firstenc.Species, firstenc.Language, firstenc.Format);

                firstenc.SetSuggestedFormArgument(originspecies);
                firstenc.RefreshAbility(firstenc.AbilityNumber >> 1);
            }

            var second = EntityConverter.ConvertToType(firstenc, blank.GetType(), out _);
            if (second is null)
                return null;

            second.HeldItem = blank.HeldItem;
            second.SetSuggestedMoves();
            second.SetHandlerandMemory(tr, null);

            if (second is IScaledSizeValue sv)
            {
                sv.HeightAbsolute = sv.CalcHeightAbsolute;
                sv.WeightAbsolute = sv.CalcWeightAbsolute;
            }

            if (second.Form == blank.Form)
                return second;

            // force form and check legality as a last ditch effort.
            second.Form = blank.Form;
            if (second is IScaledSizeValue sc)
            {
                sc.HeightAbsolute = sc.CalcHeightAbsolute;
                sc.WeightAbsolute = sc.CalcWeightAbsolute;
            }

            var la = new LegalityAnalysis(second);
            if (la.Valid)
                return second;
            return null;
        }

        private static int GetAvailableForm(this PKM pk)
        {
            var species = pk.Species;
            var pi = pk.PersonalInfo;
            var formcount = pi.FormCount;
            if (formcount == 0)
                return -1;

            if (!(pk.SWSH || pk.BDSP || pk.LA))
                return pk.Form;
            static bool IsPresentInGameSWSH(ushort species, byte form) => PersonalTable.SWSH.IsPresentInGame(species, form);
            static bool IsPresentInGameBDSP(ushort species, byte form) => PersonalTable.BDSP.IsPresentInGame(species, form);
            static bool IsPresentInGameLA(ushort species, byte form) =>   PersonalTable.LA.  IsPresentInGame(species, form);
            for (byte f = 0; f < formcount; f++)
            {
                if (pk.LA   && IsPresentInGameLA  (species, f)) return f;
                if (pk.BDSP && IsPresentInGameBDSP(species, f)) return f;
                if (pk.SWSH && IsPresentInGameSWSH(species, f)) return f;
            }
            return -1;
        }

        private static bool GetIsFormInvalid(this PKM pk, ITrainerInfo tr, byte form)
        {
            var generation = tr.Generation;
            var species = pk.Species;
            switch ((Species)species)
            {
                case Species.Unown when generation == 2 && form >= 26:
                    return true;
                case Species.Floette when form == 5:
                    return true;
                case Species.Shaymin or Species.Furfrou or Species.Hoopa when form != 0 && generation <= 6:
                    return true;
                case Species.Arceus when generation == 4 && form == 9: // ??? form
                    return true;
            }
            if (FormInfo.IsBattleOnlyForm(pk.Species, form, generation))
                return true;
            if (form == 0)
                return false;

            if (species == 25 || SimpleEdits.AlolanOriginForms.Contains(species))
            {
                if (generation >= 7 && pk.Generation is (< 7) and (not -1))
                    return true;
            }

            return false;
        }

        private static int? GetFormSpecificItem(int game, ushort species, byte form)
        {
            if (game == (int)GameVersion.PLA)
                return null;
            var generation = ((GameVersion)game).GetGeneration();
            return species switch
            {
                (ushort)Species.Arceus => generation != 4 || form < 9 ? SimpleEdits.GetArceusHeldItemFromForm(form) : SimpleEdits.GetArceusHeldItemFromForm(form - 1),
                (ushort)Species.Silvally => SimpleEdits.GetSilvallyHeldItemFromForm(form),
                (ushort)Species.Genesect => SimpleEdits.GetGenesectHeldItemFromForm(form),
                (ushort)Species.Giratina => form == 1 ? 112 : null, // Griseous Orb
                (ushort)Species.Zacian => form == 1 ? 1103 : null, // Rusted Sword
                (ushort)Species.Zamazenta => form == 1 ? 1104 : null, // Rusted Shield
                _ => null
            };
        }

        private static PKM? GetFirstEncounter(IEnumerable<PKM> legalencs, byte? form)
        {
            if (form is null)
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
