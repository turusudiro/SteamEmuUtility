using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SteamCommon.Models
{
    public partial class SchemaAppDetails
    {
        [SerializationPropertyName("gameName")]
        public string Name { get; set; }
        [SerializationPropertyName("gameVersion")]
        public string Version { get; set; }
        public class AvailableGameStatsData
        {
            public class AchievementsData
            {
                [SerializationPropertyName("name")]
                public string Name { get; set; }
                [SerializationPropertyName("defaultvalue")]
                public string DefaultValue { get; set; }
                [SerializationPropertyName("displayName")]
                public string DisplayName { get; set; }
                [SerializationPropertyName("hidden")]
                public string Hidden { get; set; }
                [SerializationPropertyName("description")]
                public string Description { get; set; }
                [SerializationPropertyName("icon")]
                public string Icon { get; set; }
                [SerializationPropertyName("icongray")]
                public string IconGray { get; set; }
            }
            [SerializationPropertyName("achievements")]
            public List<AchievementsData> Achievements { get; set; } = new List<AchievementsData>();
        }
        [SerializationPropertyName("availableGameStats")]
        public AvailableGameStatsData AvailableGameStats { get; set; } = new AvailableGameStatsData();
    }
    public partial class AppDetailsInfo
    {
        [SerializationPropertyName("success")]
        public bool Success { get; set; }
        [SerializationPropertyName("data")]
        public AppDetails Data { get; set; }
    }
    public class AppDetails
    {
        [SerializationPropertyName("Name")]
        public string Name { get; set; }
        [SerializationPropertyName("type")]
        public string Type { get; set; }
        [SerializationPropertyName("header_image")]
        public string ImageLink { get; set; }
        [SerializationPropertyName("dlc")]
        public IList<string> DLC { get; set; }
    }
}