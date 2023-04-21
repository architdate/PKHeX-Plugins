using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace AutoModPlugins.GUI
{
    public partial class ALMSettings : Form
    {
        private PluginSettings pluginSettings;
        public ALMSettings(PluginSettings obj)
        {
            pluginSettings = obj;
            InitializeComponent();
            EditSettingsProperties(obj);
            PG_Settings.SelectedObject = obj;
            this.TranslateInterface(WinFormsTranslator.CurrentLanguage);
            var parent = FindForm();
            if (parent != null)
                this.CenterToForm(parent);
        }

        private void SettingsEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.W && ModifierKeys == Keys.Control)
                Close();
        }

        private void ALMSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            // ALM Settings
            ShowdownSetLoader.SetAPILegalitySettings(pluginSettings);

            pluginSettings.Save();
        }

        private void EditSettingsProperties(object _settings)
        {
            var lang = WinFormsTranslator.CurrentLanguage;
            var translation = WinFormsTranslator.GetContext(lang);
            var type_descriptor = TypeDescriptor.GetProvider(_settings).GetTypeDescriptor(_settings);
            if (type_descriptor == null)
                return;
            var ctd = new PropertyOverridingTypeDescriptor(type_descriptor);
            foreach (var pd in TypeDescriptor.GetProperties(_settings).OfType<PropertyDescriptor>())
            {
                var desc = "Property Description needs to be defined. Please raise this issue on GitHub or at the discord: https://discord.gg/tDMvSRv";
                if (pd.Description != null)
                    desc = translation.GetTranslatedText($"{pd.Name}_description", pd.Description);

                var category = "Uncategorized Settings";
                if (pd.Category != null)
                    category = translation.GetTranslatedText($"{pd.Name}_category", pd.Category);
                if (desc == null || category == null)
                    throw new Exception("Category / Description translations are null");
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

        public override object GetPropertyOwner(PropertyDescriptor? pd)
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

        public override PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
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

        public override ICustomTypeDescriptor GetTypeDescriptor(Type? objectType, object? instance)
        {
            return ctd;
        }
    }
}
