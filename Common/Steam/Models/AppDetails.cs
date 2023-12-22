using System.Collections.Generic;
using Newtonsoft.Json;
using Playnite.SDK.Data;

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
    public partial class SteamAppDetails<TKey, TValue>
    {
        private Dictionary<TKey, TValue> internalDictionary;

        public SteamAppDetails()
        {
            internalDictionary = new Dictionary<TKey, TValue>();
        }

        public void Add(TKey key, TValue value)
        {
            internalDictionary.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return internalDictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return internalDictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get => internalDictionary[key];
            set => internalDictionary[key] = value;
        }

        public ICollection<TKey> Keys => internalDictionary.Keys;
        public ICollection<TValue> Values => internalDictionary.Values;
    }
    public partial class AppDetailsInfo
    {
        [JsonProperty("success")]
        private bool _success;
        [JsonIgnore]
        public bool Success { get => _success; }
        [JsonProperty("data")]
        private AppDetails _data;
        [JsonIgnore]
        public AppDetails Data { get => _data; }
    }
    public class AppDetails
    {
        [JsonProperty("dlc")]
        private List<string> _dlc;
        [JsonIgnore]
        public List<string> DLC { get => _dlc; }
    }
}