namespace AutoModPlugins
{
    public class ALMSettingMetadata
    {
        public string SettingName { get; }
        public string? Description { get; set; }
        public string? Category { get; set; }

        public ALMSettingMetadata(string settingName, string? description = null, string? category = null)
        {
            SettingName = settingName;
            Description = description;
            Category = category;
        }
    }
}
