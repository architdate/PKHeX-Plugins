using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using AutoModPlugins.Properties;

namespace AutoModPlugins.GUI
{
    public partial class ALMSettings : Form
    {
        private readonly ALMSettingMetadata[] settings = new[]
        {
            // Trainer Data
            new ALMSettingMetadata("AllowTrainerOverride", "Allows overriding trainer data with \"OT\", \"TID\", \"SID\", and \"OTGender\" as part of a Showdown set.", "Trainer Data"),
            new ALMSettingMetadata("UseTrainerData", "Enables use of custom trainer data based on the \"trainers\" folder.", "Trainer Data"),

            // Connection Settings
            new ALMSettingMetadata("LatestIP", "Stores the last IP used by LiveHeX.", "Connection"),
            new ALMSettingMetadata("USBBotBasePreferred", "Allows LiveHeX to use USB-Botbase instead of sys-botbase.", "Connection"),

            // Customization
            new ALMSettingMetadata("ForceSpecifiedBall", "Allows overriding Poké Ball with \"Ball\" in a Showdown set.", "Customization"),
            new ALMSettingMetadata("PrioritizeGame", "If enabled, tries to generate a Pokémon based on PrioritizeGameVersion first.", "Customization"),
            new ALMSettingMetadata("PriorityGameVersion", "Setting this to \"Any\" prioritizes the current save game, and setting a specific game prioritizes that instead.", "Customization"),
            new ALMSettingMetadata("SetAllLegalRibbons", "Adds all ribbons that are legal according to PKHeX legality.", "Customization"),
            new ALMSettingMetadata("SetBattleVersion", "Sets all past-generation Pokémon as Battle Ready for games that support it.", "Customization"),
            new ALMSettingMetadata("SetMatchingBalls", "Attempts to choose a matching Poké Ball based on Pokémon color.", "Customization"),

            // Legality
            new ALMSettingMetadata("Timeout", "Global timeout per Pokémon being generated (in seconds)", "Legality"),
            new ALMSettingMetadata("PrioritizeEncounters", "Defines the order in which Pokémon encounters are prioritized", "Legality"),
            new ALMSettingMetadata("SetRandomTracker", "Randomizes a HOME tracker for every Pokémon.", "Legality"),
            new ALMSettingMetadata("UseXOROSHIRO", "Generates legal nonshiny Generation 8 raid Pokémon based on the game's RNG.", "Legality"),
            new ALMSettingMetadata("EnableEasterEggs", "Produces an Easter Egg Pokémon if the provided set is illegal.", "Legality"),

            // Living Dex
            new ALMSettingMetadata("IncludeForms", "Generate all forms of the Pokémon. Note that some generations may not have enough box space for all forms.", "Living Dex"),
            new ALMSettingMetadata("SetShiny", "Try to generate the shiny version of the Pokémon if possible.", "Living Dex"),

            // Miscellaneous
            new ALMSettingMetadata("GPSSBaseURL", "Base URL for Flagbrew's Global PKSM Sharing Service (GPSS) features.", "Miscellaneous"),
            new ALMSettingMetadata("PromptForSmogonImport", "Used for \"Generate Smogon Sets\". If set to true, ALM will ask for approval for each set before attempting to generate it.", "Miscellaneous"),
            new ALMSettingMetadata("UseMarkings", "Sets markings on the Pokémon based on IVs.", "Miscellaneous"),
            new ALMSettingMetadata("UseCompetitiveMarkings", "Sets IVs of 31 to blue and 30 to red if enabled. Otherwise, sets IVs of 31 to blue and 0 to red.", "Miscellaneous"),
        };

        public ALMSettings(object obj)
        {
            InitializeComponent();
            EditSettingsProperties(obj);
            PG_Settings.SelectedObject = obj;
            this.TranslateInterface(WinFormsTranslator.CurrentLanguage);

            this.CenterToForm(FindForm());
        }

        private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
                Close();
        }

        private void ALMSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ALM Settings
            ShowdownSetLoader.SetAPILegalitySettings();

            AutoLegality.Default.Save();
        }

        private void EditSettingsProperties(object _settings)
        {
            var lang = WinFormsTranslator.CurrentLanguage;
            var translation = WinFormsTranslator.GetContext(lang);
            var ctd = new PropertyOverridingTypeDescriptor(TypeDescriptor.GetProvider(_settings).GetTypeDescriptor(_settings));
            foreach (var pd in TypeDescriptor.GetProperties(_settings).OfType<PropertyDescriptor>())
            {
                var s = Array.Find(settings, z => z.SettingName == pd.Name);
                if (s == null)
                    continue;

                var desc = "Property Description needs to be defined. Please raise this issue on GitHub or at the discord: https://discord.gg/tDMvSRv";
                if (s.Description != null)
                    desc = translation.GetTranslatedText($"{s.SettingName}_description", s.Description);

                var category = "Uncategorized Settings";
                if (s.Category != null)
                    category = translation.GetTranslatedText($"{s.SettingName}_category", s.Category);

                PropertyDescriptor pd2 = TypeDescriptor.CreateProperty(_settings.GetType(), pd, new DescriptionAttribute(desc), new CategoryAttribute(category));
                ctd.OverrideProperty(pd2);
            }
            TypeDescriptor.AddProvider(new TypeDescriptorOverridingProvider(ctd), _settings);
        }
    }

    public class PropertyOverridingTypeDescriptor : CustomTypeDescriptor
    {
        private readonly Dictionary<string, PropertyDescriptor> overridePds = new();

        public PropertyOverridingTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        { }

        public void OverrideProperty(PropertyDescriptor pd)
        {
            overridePds[pd.Name] = pd;
        }

        public override object GetPropertyOwner(PropertyDescriptor pd)
        {
            var o = base.GetPropertyOwner(pd);
            return o ?? this;
        }

        public PropertyDescriptorCollection GetPropertiesImpl(PropertyDescriptorCollection pdc)
        {
            var pdl = new List<PropertyDescriptor>(pdc.Count + 1);

            foreach (PropertyDescriptor pd in pdc)
                pdl.Add(overridePds.TryGetValue(pd.Name, out var value) ? value : pd);

            return new PropertyDescriptorCollection(pdl.ToArray());
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return GetPropertiesImpl(base.GetProperties());
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetPropertiesImpl(base.GetProperties(attributes));
        }
    }

    public class TypeDescriptorOverridingProvider : TypeDescriptionProvider
    {
        private readonly ICustomTypeDescriptor ctd;

        public TypeDescriptorOverridingProvider(ICustomTypeDescriptor ctd)
        {
            this.ctd = ctd;
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            return ctd;
        }
    }
}
