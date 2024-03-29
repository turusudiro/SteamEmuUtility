﻿using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamEmuUtility;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoldbergCommon
{
    public class Goldberg
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private const string goldbergfeature = "[SEU] Goldberg";
        private static SteamEmuUtilitySettings settings;
        public static SteamEmuUtilitySettings GoldbergSettings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = value;
            }
        }
        public static string SteamWebAPIKey
        {
            get { return GoldbergSettings.SteamWebApi; }
        }
        public static string GoldbergAvatar
        {
            get
            {
                try
                {
                    // Check if the directory exists
                    if (FileSystem.DirectoryExists(Path.Combine(GoldbergAppData, "settings")))
                    {
                        // Search for the file with the specified name
                        string[] files = Directory.GetFiles(Path.Combine(GoldbergAppData, "settings"), $"account_avatar.*");

                        if (files.Length > 0)
                        {
                            // If multiple files match, you may want to choose one based on your criteria
                            // In this example, the first matching file is selected
                            return files[0];
                        }
                    }
                }
                catch { }

                return null;
            }
        }
        public static string ColdClientIni
        {
            get
            {
                return Path.Combine(GoldbergPath, "ColdClientLoader.ini");
            }
        }
        public static string ColdClientExecutable32
        {
            get
            {
                return Path.Combine(GoldbergPath, "steamclient_loader_32.exe");
            }
        }
        public static string ColdClientExecutable64
        {
            get
            {
                return Path.Combine(GoldbergPath, "steamclient_loader_64.exe");
            }
        }
        public static string AchievementWatcherAppData
        {
            get
            {
                try
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Achievement Watcher");
                }
                catch { return string.Empty; }
            }
        }
        public static string GoldbergPath
        {
            get
            {
                return Path.Combine(pluginpath, "Goldberg");
            }
        }
        public static string GoldbergAppData
        {
            get
            {
                try
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Goldberg SteamEmu Saves");
                }
                catch { return string.Empty; }
            }
        }
        public static string AccountName
        {
            get
            {
                try
                {
                    return FileSystem.ReadStringFromFile(Path.Combine(GoldbergAppData, "settings\\account_name.txt"));
                }
                catch { return string.Empty; }
            }
            set
            {
                string filePath = Path.Combine(GoldbergAppData, "settings\\account_name.txt");
                try
                {
                    if (!FileSystem.DirectoryExists(Path.GetDirectoryName(filePath)))
                    {
                        FileSystem.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    FileSystem.WriteStringToFile(filePath, value);
                }
                catch { }
            }
        }
        public static string Language
        {
            get
            {
                try
                {
                    return FileSystem.ReadStringFromFile(Path.Combine(GoldbergAppData, "settings\\language.txt"));
                }
                catch { return string.Empty; }
            }
            set
            {
                string filePath = Path.Combine(GoldbergAppData, "settings\\language.txt");
                try
                {
                    if (!FileSystem.DirectoryExists(Path.GetDirectoryName(filePath)))
                    {
                        FileSystem.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    FileSystem.WriteStringToFile(filePath, value);
                }
                catch { }
            }
        }
        public static string ListenPort
        {
            get
            {
                try
                {
                    return FileSystem.ReadStringFromFile(Path.Combine(GoldbergAppData, "settings\\listen_port.txt"));
                }
                catch { return string.Empty; }
            }
            set
            {
                string filePath = Path.Combine(GoldbergAppData, "settings\\listen_port.txt");
                try
                {
                    if (!FileSystem.DirectoryExists(Path.GetDirectoryName(filePath)))
                    {
                        FileSystem.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    FileSystem.WriteStringToFile(filePath, value);
                }
                catch { }
            }
        }
        public static string CustomBroadcasts
        {
            get
            {
                try
                {
                    return FileSystem.ReadStringFromFile(Path.Combine(GoldbergAppData, "settings\\custom_broadcasts.txt"));
                }
                catch { return string.Empty; }
            }
            set
            {
                string filePath = Path.Combine(GoldbergAppData, "settings\\custom_broadcasts.txt");
                try
                {
                    if (!FileSystem.DirectoryExists(Path.GetDirectoryName(filePath)))
                    {
                        FileSystem.CreateDirectory(Path.GetDirectoryName(filePath));
                    }
                    FileSystem.WriteStringToFile(filePath, value);
                }
                catch { }
            }
        }
        public static string UserSteamID
        {
            get
            {
                try
                {
                    return FileSystem.ReadStringFromFile(Path.Combine(GoldbergAppData, "settings\\user_steam_id.txt"));
                }
                catch { return string.Empty; }
            }
            set
            {
                string filePath = Path.Combine(GoldbergAppData, "settings\\user_steam_id.txt");
                try
                {
                    FileSystem.WriteStringToFile(filePath, value);
                }
                catch { }
            }
        }
        public static ulong UserSteamID3
        {
            get
            {
                if (ulong.TryParse(UserSteamID, out ulong steamID))
                {
                    return steamID - 76561197960265728;
                }
                else
                {
                    // Handle the case where UserSteamID is not a valid ulong
                    // For example, return a default value or throw an exception
                    throw new InvalidOperationException("UserSteamID is not a valid ulong.");
                }
            }
        }
        public static bool ColdClientExists(out List<string> missingFiles)
        {
            var ColdClientFiles = new List<string>
            {
            $"{GoldbergPath}\\steamclient.dll",
            $"{GoldbergPath}\\extra_dlls\\steamclient_extra.dll",
            $"{GoldbergPath}\\extra_dlls\\steamclient_extra64.dll",
            $"{GoldbergPath}\\steamclient_loader_32.exe",
            $"{GoldbergPath}\\steamclient_loader_64.exe",
            $"{GoldbergPath}\\steamclient64.dll",
            };
            missingFiles = new List<string>();

            foreach (string file in ColdClientFiles)
            {
                if (!FileSystem.FileExists(file))
                {
                    // If the file doesn't exist, add it to the list of missing files
                    missingFiles.Add(Path.GetFileName(file));
                }
            }

            // Return true if there are no missing files, otherwise return false
            return missingFiles.Count == 0;
        }
        public static string GameUserDataSteamPath(Game game)
        {
            return Path.Combine(Steam.SteamDirectory, "userdata", UserSteamID3.ToString(), game.GameId);
        }
        public static GameFeature Feature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(goldbergfeature);
        }
        public static string GameAppdataPath(Game game)
        {
            return Path.Combine(GoldbergAppData, game.GameId);
        }
        public static string GameSteamSettingPath(string appid)
        {
            return Path.Combine(CommonPath, appid, "steam_settings");
        }
        public static string GameSettingsPath(string appid)
        {
            return Path.Combine(CommonPath, appid);
        }
        public static string SteamSettingsPath
        {
            get
            {
                return Path.Combine(GoldbergPath, "steam_settings");
            }
        }
        private static string CommonPath { get { return Path.Combine(pluginpath, "Common", "Goldberg"); } }
        private static string pluginpath;
        public static string PluginPath
        {
            get
            {
                return pluginpath;
            }
            set
            {
                pluginpath = value;
            }
        }

    }
}
