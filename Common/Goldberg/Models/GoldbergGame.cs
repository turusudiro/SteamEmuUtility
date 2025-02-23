﻿using GoldbergCommon.Configs;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoldbergCommon.Models
{
    public partial class GoldbergGame : ObservableObject
    {
        private readonly string gameSettingsPath;
        public readonly string gameSteamSettingsPath;
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
        private IEnumerable<string> _branches;
        public IEnumerable<string> Branches
        {
            get => _branches;
            set
            {
                _branches = value;
                OnPropertyChanged();
            }
        }
        private string _selectedbranch;
        public string SelectedBranch
        {
            get => _selectedbranch;
            set
            {
                _selectedbranch = value;
                OnPropertyChanged();
            }
        }
        public bool GenerateInfo
        {
            get
            {
                return GenerateAllInfo || GenerateAchievements || GenerateArchitecture || GenerateBranches || GenerateColdClient || GenerateController || GenerateDepots || GenerateDLC || GenerateSupportedLanguages;
            }
        }
        public bool GenerateAllInfo
        {
            get
            {
                return GenerateAchievements && GenerateArchitecture && GenerateBranches && GenerateColdClient && GenerateController && GenerateDepots && GenerateDLC && GenerateSupportedLanguages;
            }
            set
            {
                GenerateAchievements = value;
                GenerateArchitecture = value;
                GenerateBranches = value;
                GenerateColdClient = value;
                GenerateController = value;
                GenerateDepots = value;
                GenerateDLC = value;
                GenerateSupportedLanguages = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GenerateInfo));
            }
        }
        private bool _generateachievement;
        public bool GenerateAchievements
        {
            get => _generateachievement; set { _generateachievement = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatearchitecture;
        public bool GenerateArchitecture
        {
            get => _generatearchitecture;
            set { _generatearchitecture = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatebranches;
        public bool GenerateBranches
        {
            get => _generatebranches; set { _generatebranches = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatecoldclient;
        public bool GenerateColdClient
        {
            get => _generatecoldclient; set { _generatecoldclient = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatecontroller;
        public bool GenerateController
        {
            get => _generatecontroller; set { _generatecontroller = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatedepots;
        public bool GenerateDepots
        {
            get => _generatedepots; set { _generatedepots = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatedlc;
        public bool GenerateDLC
        {
            get => _generatedlc; set { _generatedlc = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private bool _generatesupportedlanguages;
        public bool GenerateSupportedLanguages
        {
            get => _generatesupportedlanguages; set { _generatesupportedlanguages = value; OnPropertyChanged(); OnPropertyChanged(nameof(GenerateAllInfo)); OnPropertyChanged(nameof(GenerateInfo)); }
        }
        private string custombroadcastaddress;
        public string CustomBroadcastAddress
        {
            get
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
                return custombroadcastaddress;
            }
            set
            {
                string filepath = Path.Combine(gameSteamSettingsPath, "custom_broadcasts.txt");
                string configsEmu = Path.Combine(gameSettingsPath, "configs.emu.ini");

                string encodedValue;
                if (value.Contains(Environment.NewLine))
                {
                    encodedValue = value.Replace(Environment.NewLine, "||");
                }
                else
                {
                    encodedValue = value;
                }
                ConfigsCommon.SerializeConfigs(encodedValue, configsEmu, "Main", "Broadcasts");
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
