using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamEmuUtility.ViewModels;
using System;
using System.Collections.Generic;

namespace SteamEmuUtility.Common.Goldberg.Models
{
    public class GoldbergMod
    {
        [JsonIgnore]
        public string ID { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("steam_id_owner")]
        public ulong? SteamIDOwner { get; set; }
        [JsonProperty("time_created")]
        public int? TimeCreated { get; set; }
        [JsonProperty("time_updated")]
        public int? TimeUpdated { get; set; }
        [JsonProperty("time_added")]
        public int? TimeAdded { get; set; }
        [JsonProperty("tags")]
        public string Tags { get; set; }
        [JsonProperty("primary_filename")]
        public string PrimaryFileName { get; set; }
        [JsonProperty("primary_filesize")]
        public int? PrimaryFileSize { get; set; }
        [JsonProperty("preview_filename")]
        public string PreviewFileName { get; set; }
        [JsonProperty("preview_filesize")]
        public int? PreviewFileSize { get; set; }
        [JsonProperty("total_files_sizes")]
        public int? TotalFilesSizes { get; set; }
        [JsonProperty("min_game_branch")]
        public string MinGameBranch { get; set; }
        [JsonProperty("max_game_branch")]
        public string MaxGameBranch { get; set; }
        [JsonProperty("workshop_item_url")]
        public string WorkshopItemURL { get; set; }
        [JsonProperty("upvotes")]
        public int? Upvotes { get; set; }
        [JsonProperty("downvotes")]
        public int? Downvotes { get; set; }
        [JsonProperty("num_children")]
        public int? NumChildren { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("preview_url")]
        public string PreviewURL { get; set; }
        [JsonProperty("score")]
        public double? Score { get; set; }
        [JsonProperty("metadata")]
        public string Metadata { get; set; }
        public bool ShouldSerializePreviewFileName()
        {
            return !string.IsNullOrEmpty(PreviewFileName);
        }
        public GoldbergMod() { }
        public GoldbergMod(GoldbergModViewModel vm)
        {
            ID = vm.ID;
            Title = vm.Title;
            Description = vm.Description;
            SteamIDOwner = vm.SteamIDOwner;
            TimeCreated = vm.TimeCreated;
            TimeUpdated = vm.TimeUpdated;
            TimeAdded = vm.TimeAdded;
            Tags = vm.Tags;
            PrimaryFileName = vm.PrimaryFileName;
            PrimaryFileSize = vm.PrimaryFileSize;
            PreviewFileName = vm.PreviewFileName;
            PreviewFileSize = vm.PreviewFileSize;
            TotalFilesSizes = vm.TotalFilesSizes;
            MinGameBranch = vm.MinGameBranch;
            MaxGameBranch = vm.MaxGameBranch;
            WorkshopItemURL = vm.WorkshopItemURL;
            Upvotes = vm.Upvotes;
            Downvotes = vm.Downvotes;
            NumChildren = vm.NumChildren;
            Path = vm.Path;
            PreviewURL = vm.PreviewURL;
            Score = vm.Score;
            Metadata = vm.Metadata;
        }
    }
    public class GoldbergModDictionaryConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var dict = new Dictionary<string, GoldbergMod>();

            foreach (var prop in jObject.Properties())
            {
                var mod = prop.Value.ToObject<GoldbergMod>(serializer);
                mod.ID = prop.Name;
                dict[prop.Name] = mod;
            }

            return dict;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dict = (Dictionary<string, GoldbergMod>)value;
            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(Dictionary<string, GoldbergMod>);
    }
}
