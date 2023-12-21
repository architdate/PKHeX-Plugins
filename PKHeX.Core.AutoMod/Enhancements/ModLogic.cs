﻿using System;
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
        public static LivingDexConfig Config { get; set; } = new()
        {
            IncludeForms = false,
            SetShiny = false,
            SetAlpha = false,
            NativeOnly = false,
        };

        /// <summary>
        /// Exports the <see cref="SaveFile.CurrentBox"/> to <see cref="ShowdownSet"/> as a single string.
        /// </summary>
        /// <param name="provider">Save File to export from</param>
        /// <returns>Concatenated string of all sets in the current box.</returns>
        public static string GetRegenSetsFromBoxCurrent(this ISaveFileProvider provider) =>
            GetRegenSetsFromBox(provider.SAV, provider.CurrentBox);

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
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav) =>
            sav.GenerateLivingDex(Config);

        /// <summary>
        /// Gets a living dex (one per species, not every form)
        /// </summary>
        /// <param name="sav">Save File to receive the generated <see cref="PKM"/>.</param>
        /// <param name="cfg"></param>
        /// <returns>Consumable list of newly generated <see cref="PKM"/> data.</returns>
        public static IEnumerable<PKM> GenerateLivingDex(this SaveFile sav, LivingDexConfig cfg)
        {
            List<PKM> pklist = [];
            var tr = APILegality.UseTrainerData
                ? TrainerSettings.GetSavedTrainerData(sav.Version, sav.Generation, fallback: sav, lang: (LanguageID)sav.Language)
                : sav;
            var pt = sav.Personal;
            var species = Enumerable.Range(1, sav.MaxSpeciesID).Select(x => (ushort)x);
            foreach (var s in species)
            {
                if (!pt.IsSpeciesInGame(s))
                    continue;

                var num_forms = pt[s].FormCount;
                var str = GameInfo.Strings;
                if (num_forms == 1 && cfg.IncludeForms) // Validate through form lists
                    num_forms = (byte)FormConverter.GetFormList(s, str.types, str.forms, GameInfo.GenderSymbolUnicode, sav.Context).Length;

                for (byte f = 0; f < num_forms; f++)
                {
                    if (!sav.Personal.IsPresentInGame(s, f))
                        continue;
                    if (FormInfo.IsLordForm(s, f, sav.Context))
                        continue;
                    if (FormInfo.IsBattleOnlyForm(s, f, sav.Generation))
                        continue;
                    if (FormInfo.IsFusedForm(s, f, sav.Generation))
                        continue;
                    if (FormInfo.IsTotemForm(s, f) && sav.Context is not EntityContext.Gen7)
                        continue;

                    var pk = AddPKM(sav, tr, s, f, cfg.SetShiny, cfg.SetAlpha, cfg.NativeOnly);
                    if (pk is null || pklist.Find(x => x.Species == pk.Species && x.Form == pk.Form) is not null)
                        continue;

                    pklist.Add(pk);
                    if (!cfg.IncludeForms)
                        break;
                }
            }
            return pklist;
        }

        private static PKM? AddPKM(
            SaveFile sav,
            ITrainerInfo tr,
            ushort species,
            byte form,
            bool shiny,
            bool alpha,
            bool nativeOnly
        )
        {
            if (tr.GetRandomEncounter(species, form, shiny, alpha, nativeOnly, out var pk) && pk is { Species: > 0 })
            {
                pk.Heal();
                return pk;
            }
            if (sav is SAV2)
            {
                var g1 = new SAV1(GameVersion.YW) { Language = tr.Language, OT = tr.OT, TID16 = tr.TID16 };
                if (GetRandomEncounter(g1, species, form, shiny, false, nativeOnly, out var pkm))
                {
                    if (pkm is PK1 pk1)
                        return pk1.ConvertToPK2();
                }
            }

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
        /// <param name="nativeOnly"></param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(
            this SaveFile sav,
            ushort species,
            byte form,
            bool shiny,
            bool alpha,
            bool nativeOnly,
            out PKM? pk
        ) =>
            ((ITrainerInfo)sav).GetRandomEncounter(species, form, shiny, alpha, nativeOnly, out pk);

        /// <summary>
        /// Gets a legal <see cref="PKM"/> from a random in-game encounter's data.
        /// </summary>
        /// <param name="tr">Trainer Data to use in generating the encounter</param>
        /// <param name="species">Species ID to generate</param>
        /// <param name="form">Form to generate; if left null, picks first encounter</param>
        /// <param name="shiny"></param>
        /// <param name="alpha"></param>
        /// <param name="nativeOnly"></param>
        /// <param name="pk">Result legal pkm</param>
        /// <returns>True if a valid result was generated, false if the result should be ignored.</returns>
        public static bool GetRandomEncounter(
            this ITrainerInfo tr,
            ushort species,
            byte form,
            bool shiny,
            bool alpha,
            bool nativeOnly,
            out PKM? pk
        )
        {
            var blank = EntityBlank.GetBlank(tr);
            pk = GetRandomEncounter(blank, tr, species, form, shiny, alpha, nativeOnly);
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
        /// <param name="nativeOnly"></param>
        /// <returns>Result legal pkm, null if data should be ignored.</returns>
        private static PKM? GetRandomEncounter(
            PKM blank,
            ITrainerInfo tr,
            ushort species,
            byte form,
            bool shiny,
            bool alpha,
            bool nativeOnly
        )
        {
            blank.Species = species;
            blank.Gender = blank.GetSaneGender();
            if (species is ((ushort)Species.Meowstic) or ((ushort)Species.Indeedee))
                blank.Form = (byte)blank.Gender;
            else
                blank.Form = form;

            var template = EntityBlank.GetBlank(tr.Generation, (GameVersion)tr.Game);
            var item = GetFormSpecificItem(tr.Game, blank.Species, blank.Form);
            if (item is not null)
                blank.HeldItem = (int)item;

            if (blank is { Species: (ushort)Species.Keldeo, Form: 1 })
                blank.Move1 = (ushort)Move.SecretSword;

            if (blank.GetIsFormInvalid(tr, blank.Form))
                return null;

            var setText = new ShowdownSet(blank).Text.Split('\r')[0];
            if (shiny && !SimpleEdits.IsShinyLockedSpeciesForm(blank.Species, blank.Form))
                setText += Environment.NewLine + "Shiny: Yes";

            if (template is IAlphaReadOnly && alpha && tr.Game == (int)GameVersion.PLA)
                setText += Environment.NewLine + "Alpha: Yes";

            var sset = new ShowdownSet(setText);
            var set = new RegenTemplate(sset) { Nickname = string.Empty };
            template.ApplySetDetails(set);

            var t = template.Clone();
            var almres = tr.TryAPIConvert(set, t, nativeOnly);
            var pk = almres.Created;
            var success = almres.Status;
            if (pk.Species == (ushort)Species.Unown && pk.Form != blank.Form)
                pk.Form = blank.Form;

            if (success == LegalizationResult.Regenerated)
                return pk;

            sset = new ShowdownSet(setText.Split('\r')[0]);
            set = new RegenTemplate(sset) { Nickname = string.Empty };
            template.ApplySetDetails(set);

            t = template.Clone();
            almres = tr.TryAPIConvert(set, t, nativeOnly);
            pk = almres.Created;
            success = almres.Status;
            if (pk.Species is (ushort)Species.Gholdengo)
            {
                pk.SetSuggestedFormArgument();
                pk.SetSuggestedMoves();
                success = LegalizationResult.Regenerated;
            }

            if (success == LegalizationResult.Regenerated)
                return pk;
            return null;
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
                case Species.Shaymin
                or Species.Furfrou
                or Species.Hoopa when form != 0 && generation <= 6:
                    return true;
                case Species.Arceus when generation == 4 && form == 9: // ??? form
                    return true;
                case Species.Scatterbug or Species.Spewpa when form > Vivillon3DS.MaxWildFormID:
                    return true;
            }
            if (FormInfo.IsBattleOnlyForm(pk.Species, form, generation))
                return true;
            if (form == 0)
                return false;

            if (species == 25 || SimpleEdits.AlolanOriginForms.Contains(species))
            {
                if (generation >= 7 && pk.Generation is < 7 and not -1)
                    return true;
            }

            return false;
        }

        private static int? GetFormSpecificItem(int game, int species, int form)
        {
            if (game == (int)GameVersion.PLA)
                return null;
            var generation = ((GameVersion)game).GetGeneration();
            return species switch
            {
                (ushort)Species.Arceus
                    => generation != 4 || form < 9
                        ? SimpleEdits.GetArceusHeldItemFromForm(form)
                        : SimpleEdits.GetArceusHeldItemFromForm(form - 1),
                (ushort)Species.Silvally => SimpleEdits.GetSilvallyHeldItemFromForm(form),
                (ushort)Species.Genesect => SimpleEdits.GetGenesectHeldItemFromForm(form),
                (ushort)Species.Giratina => form == 1 ? 112 : null, // Griseous Orb
                (ushort)Species.Zacian => form == 1 ? 1103 : null, // Rusted Sword
                (ushort)Species.Zamazenta => form == 1 ? 1104 : null, // Rusted Shield
                _ => null
            };
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
