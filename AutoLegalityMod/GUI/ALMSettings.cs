using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using AutoModPlugins.Properties;

namespace AutoModPlugins.GUI
{
    public partial class ALMSettings : Form
    {
        private readonly ALMSettingMetadata[] settings = new[]
        {
            new ALMSettingMetadata("AllowTrainerOverride", "Allows overriding trainer data", "Trainer Data Settings"),
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
            PropertyOverridingTypeDescriptor ctd = new PropertyOverridingTypeDescriptor(TypeDescriptor.GetProvider(_settings).GetTypeDescriptor(_settings));
            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(_settings))
            {
                var desc = "Property Description needs to be defined. Please raise this issue on GitHub or at the discord: https://discord.gg/tDMvSRv";
                var category = "Uncategorized Settings";
                foreach (var s in settings)
                {
                    if (pd.Name == s.SettingName)
                    {
                        if (s.Description != null) desc = s.Description;
                        if (s.Category != null) category = s.Category;
                    }
                }
                PropertyDescriptor pd2 = TypeDescriptor.CreateProperty(_settings.GetType(), pd, new DescriptionAttribute(desc), new CategoryAttribute(category));
                ctd.OverrideProperty(pd2);
            }
            TypeDescriptor.AddProvider(new TypeDescriptorOverridingProvider(ctd), _settings);
        }

        
    }

    public class PropertyOverridingTypeDescriptor : CustomTypeDescriptor
    {
        private readonly Dictionary<string, PropertyDescriptor> overridePds = new Dictionary<string, PropertyDescriptor>();

        public PropertyOverridingTypeDescriptor(ICustomTypeDescriptor parent)
            : base(parent)
        { }

        public void OverrideProperty(PropertyDescriptor pd)
        {
            overridePds[pd.Name] = pd;
        }

        public override object GetPropertyOwner(PropertyDescriptor pd)
        {
            object o = base.GetPropertyOwner(pd);

            if (o == null)
            {
                return this;
            }

            return o;
        }

        public PropertyDescriptorCollection GetPropertiesImpl(PropertyDescriptorCollection pdc)
        {
            List<PropertyDescriptor> pdl = new List<PropertyDescriptor>(pdc.Count + 1);

            foreach (PropertyDescriptor pd in pdc)
            {
                if (overridePds.ContainsKey(pd.Name))
                {
                    pdl.Add(overridePds[pd.Name]);
                }
                else
                {
                    pdl.Add(pd);
                }
            }

            PropertyDescriptorCollection ret = new PropertyDescriptorCollection(pdl.ToArray());

            return ret;
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
