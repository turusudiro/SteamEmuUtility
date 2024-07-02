using GoldbergCommon.Configs;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon.Models;
using System.Collections.Generic;
using System.IO;

namespace GoldbergCommon.Models
{
    public partial class GoldbergGame : ObservableObject
    {
        private readonly string gameSettingsPath;
        private readonly string gameSteamSettingsPath;
        public GoldbergGame(Game game, string gameSettingsPath, string gameSteamSettingsPath)
        {
            Name = game.Name;
            Appid = game.GameId;
            InstallDirectory = game.InstallDirectory;
            this.gameSettingsPath = gameSettingsPath;
            this.gameSteamSettingsPath = gameSteamSettingsPath;
            ConfigsApp = new ConfigsApp(gameSteamSettingsPath);
            ConfigsColdClientLoader = new ConfigsColdClientLoader(gameSettingsPath);
            ConfigsEmu = new ConfigsEmu(gameSettingsPath);
            ConfigsMain = new ConfigsMain(gameSteamSettingsPath);
            ConfigsOverlay = new ConfigsOverlay(gameSteamSettingsPath);
        }
        public string InstallDirectory { get; }
        public bool UserDataSaveExists { get; set; }
        public bool IsCloudSaveAvailable { get; set; }
        public bool EnableCloudSave { get; set; }
        public ConfigsEmu ConfigsEmu { get; set; }
        public ConfigsColdClientLoader ConfigsColdClientLoader { get; set; }
        public ConfigsMain ConfigsMain { get; set; }
        public ConfigsOverlay ConfigsOverlay { get; set; }
        public ConfigsApp ConfigsApp { get; set; }
        public App AppInfo { get; set; }
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
        private string custombroadcastaddress;
        public string CustomBroadcastAddress
        {
            get
            {
                if (string.IsNullOrEmpty(custombroadcastaddress))
                {
                    string filepath = Path.Combine(gameSteamSettingsPath, "custom_broadcasts.txt");
                    string configsEmu = Path.Combine(gameSettingsPath, "configs.emu.ini");
                    if (FileSystem.FileExists(filepath))
                    {
                        custombroadcastaddress = FileSystem.ReadStringFromFile(filepath);
                    }
                    else if (FileSystem.FileExists(configsEmu))
                    {
                        custombroadcastaddress = ConfigsCommon.GetValue(configsEmu, "Main", "Broadcasts");
                    }
                }
                return custombroadcastaddress;
            }
            set
            {
                string filepath = Path.Combine(gameSteamSettingsPath, "custom_broadcasts.txt");
                string configsEmu = Path.Combine(gameSettingsPath, "configs.emu.ini");
                ConfigsCommon.SerializeConfigs(value, configsEmu, "Main", "Broadcasts");
                FileSystem.WriteStringToFileSafe(filepath, value);
                custombroadcastaddress = value;
                OnPropertyChanged();
            }
        }
        public bool CustomBroadcast
        {
            get
            {
                return FileSystem.FileExists(Path.Combine(gameSteamSettingsPath, "custom_broadcasts.txt"));
            }
            set
            {
                string filepath = Path.Combine(gameSteamSettingsPath, "custom_broadcasts.txt");
                if (value)
                {
                    FileSystem.CreateFile(filepath, true);
                    if (!string.IsNullOrEmpty(CustomBroadcastAddress))
                    {
                        FileSystem.WriteStringToFileSafe(filepath, CustomBroadcastAddress);
                    }
                }
                else
                {
                    FileSystem.DeleteFile(filepath);
                }
                OnPropertyChanged();
            }
        }
        public string Name { get; set; }
        public string Appid { get; set; }
    }
}
