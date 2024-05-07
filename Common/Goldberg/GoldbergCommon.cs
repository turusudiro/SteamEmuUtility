using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamEmuUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public static ConfigsMain ConfigsMain
        {
            get { return new ConfigsMain(); }
        }
        public static ConfigsUser ConfigsUser
        {
            get { return new ConfigsUser(); }
        }
        public static string ConfigsMainIniPath
        {
            get { return Path.Combine(GoldbergAppData, "settings", "configs.main.ini"); }
        }
        public static string ConfigsUserIniPath
        {
            get { return Path.Combine(GoldbergAppData, "settings", "configs.user.ini"); }
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
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GSE Saves");
                }
                catch { return string.Empty; }
            }
        }
        public static void UpdateConfigs(SteamEmuUtilitySettings settings)
        {
            ConfigsUser.AccountName = settings._goldbergaccountname;
            ConfigsUser.ID = settings._goldbergusersteamid;
            ConfigsUser.IP = settings._goldbergcountryip;
            ConfigsUser.Language = settings._goldberglanguage;
            ConfigsMain.EnableAccountAvatar = settings._goldbergenableaccountavatar.Value;
            ConfigsMain.Listen_Port = settings._goldberglistenport;
            CustomBroadcasts = settings._goldbergcustombroadcasts;
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
                    FileSystem.WriteStringToFile(filePath, value, createDirectory: true);
                }
                catch { }
            }
        }
        private static string UserSteamID
        {
            get
            {
                return ConfigsUser.ID;
            }
            set
            {
                ConfigsUser.ID = value;
            }
        }
        private static ulong UserSteamID3
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
        public static bool EnableCloudSave(Game game)
        {
            string appid = game.GameId;
            string goldberggamepath = GameAppdataPath(appid);
            string steamuserdatagamepath = GameUserDataSteamPath(appid);
            try
            {
                var userdatappid = Directory.GetDirectories(steamuserdatagamepath, "*", SearchOption.TopDirectoryOnly);
                if (userdatappid.Count() <= 0)
                {
                    return false;
                }
                foreach (var dir in userdatappid)
                {
                    string rootdirectory = Path.GetFileName(dir);
                    string goldbergtarget = Path.Combine(goldberggamepath, rootdirectory);
                    if (!FileSystem.DirectoryExists(goldbergtarget))
                    {
                        return false;
                    }
                    if (!FileSystem.IsSymbolicLink(goldbergtarget))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch { return false; }
        }
        public static bool UserdataSavesExists(Game game)
        {
            string steamuserdatagamepath = GameUserDataSteamPath(game.GameId);
            try
            {
                var userdatappid = Directory.GetDirectories(steamuserdatagamepath, "*", SearchOption.TopDirectoryOnly);
                if (userdatappid.Count() <= 0)
                {
                    return false;
                }
                else { return true; }
            }
            catch { return false; }
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
        public static string GameUserDataSteamPath(string appid)
        {
            return Path.Combine(Steam.SteamDirectory, "userdata", UserSteamID3.ToString(), appid);
        }
        public static GameFeature Feature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(goldbergfeature);
        }
        public static string GameAppdataPath(string appid)
        {
            return Path.Combine(GoldbergAppData, appid);
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
