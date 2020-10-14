namespace AutoModPlugins
{
    public class ALMSettingMetadata
    {
        public string SettingName { get; }
        public string? Description { get; set; } = null;
        public string? Category { get; set; } = null;

        public ALMSettingMetadata(string settingName, string? description = null, string? category = null)
        {
            SettingName = settingName;
            Description = description;
            Category = category;
        }
    }
}
