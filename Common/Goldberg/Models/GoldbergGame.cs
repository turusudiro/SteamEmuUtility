using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon.Models;
using System.Collections.Generic;
using System.IO;

namespace GoldbergCommon.Models
{
    public partial class GoldbergGame : ObservableObject
    {
        private string path;
        private readonly IPlayniteAPI PlayniteApi;
        private string steampath;
        public GoldbergGame(string path, string steampath, IPlayniteAPI api)
        {
            this.path = path;
            this.steampath = steampath;
            PlayniteApi = api;
        }
        public Game Game { get; set; }
        public bool UserdataSavesExists
        {
            get
            {
                return Goldberg.UserdataSavesExists(Game);
            }
        }
        public bool EnableCloudSave
        {
            get
            {
                return Goldberg.EnableCloudSave(Game);
            }
            set
            {
                GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility", false);
                progressOptions.IsIndeterminate = true;
                PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
                {
                    GoldbergTasks.SetSymbolicSteamToAppdata(Game, PlayniteApi, value);
                }, progressOptions);
                OnPropertyChanged();
            }
        }
        public ConfigsEmu ConfigsEmu { get; set; }
        public ConfigsColdClientLoader ConfigsColdClientLoader { get; set; }
        public ConfigsMain ConfigsMain { get; set; }
        public ConfigsOverlay ConfigsOverlay { get; set; }
        public ConfigsApp ConfigsApp { get; set; }
        public string ConfigsAppIniPath { get => _configsappinipath; set { _configsappinipath = value; OnPropertyChanged(); } }
        private string _configsappinipath;

        public AppIdInfo AppInfo { get; set; }
        public bool GenerateAllInfo
        {
            get
            {
                return GenerateAchievements && GenerateBuildID && GenerateColdClient && GenerateController && GenerateDepots && GenerateDLC && GenerateSupportedLanguages;
            }
            set
            {
                GenerateAchievements = value;
                GenerateBuildID = value;
                GenerateColdClient = value;
                GenerateController = value;
                GenerateDepots = value;
                GenerateDLC = value;
                GenerateSupportedLanguages = value;
                OnPropertyChanged();
            }
        }
        private bool _generateachievement;
        public bool GenerateAchievements
        {
            get => _generateachievement; set { _generateachievement = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        public bool GenerateArchitecture { get; set; }
        private bool _generatebuildid;
        public bool GenerateBuildID
        {
            get => _generatebuildid; set { _generatebuildid = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        private bool _generatecoldclient;
        public bool GenerateColdClient
        {
            get => _generatecoldclient; set { _generatecoldclient = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        private bool _generatecontroller;
        public bool GenerateController
        {
            get => _generatecontroller; set { _generatecontroller = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        private bool _generatedepots;
        public bool GenerateDepots
        {
            get => _generatedepots; set { _generatedepots = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        private bool _generatedlc;
        public bool GenerateDLC
        {
            get => _generatedlc; set { _generatedlc = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        private bool _generatesupportedlanguages;
        public bool GenerateSupportedLanguages
        {
            get => _generatesupportedlanguages; set { _generatesupportedlanguages = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); }
        }
        public string CustomBroadcastAddress
        {
            get
            {
                string filepath = Path.Combine(path, steampath, "custom_broadcasts.txt");
                if (File.Exists(filepath))
                {
                    return FileSystem.ReadStringFromFile(filepath);
                }
                return string.Empty;
            }
            set
            {
                string filepath = Path.Combine(path, steampath, "custom_broadcasts.txt");
                FileSystem.WriteStringToFileSafe(filepath, value);
                OnPropertyChanged();
            }
        }

        public bool CustomBroadcast
        {
            get
            {
                return FileSystem.FileExists(Path.Combine(path, steampath, "custom_broadcasts.txt"));
            }
            set
            {
                string filepath = Path.Combine(path, steampath, "custom_broadcasts.txt");
                if (value)
                {
                    FileSystem.CreateFile(filepath, true);
                }
                else
                {
                    FileSystem.DeleteFile(filepath);
                }
                OnPropertyChanged();
            }
        }

        public string Name { get; set; }
        public string AppID { get; set; }
    }
}
