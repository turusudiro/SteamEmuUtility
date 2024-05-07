using DownloaderCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamEmuUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using static GoldbergCommon.Goldberg;

namespace GoldbergCommon
{
    public class GoldbergTasks
    {
        public static void CheckForUpdate(IPlayniteAPI PlayniteApi, SteamEmuUtilitySettingsViewModel Settings, SteamEmuUtility.SteamEmuUtility plugin)
        {
            string verPath = Path.Combine(GoldbergPath, "Version.txt");
            if (!FileSystem.FileExists(verPath))
            {
                return;
            }
            string url = @"https://api.github.com/repos/otavepto/gbe_fork/releases/latest";
            string raw = HttpDownloader.DownloadString(url);
            dynamic json = Serialization.FromJson<object>(raw);
            DateTime jsonDate = json.published_at;
            string jsonVer = $"{jsonDate.Year}/{jsonDate.Month}/{jsonDate.Day}";
            string ver = FileSystem.ReadStringFromFile(verPath);
            if (!string.IsNullOrEmpty(ver))
            {
                if (jsonVer.Equals(ver))
                {
                    return;
                }
                PlayniteApi.Notifications.Add(new NotificationMessage(plugin.Id.ToString(), string.Format(ResourceProvider.GetString("LOCSEU_UpdateAvailable"), "Goldberg"),
                    NotificationType.Info, () => Settings.DownloadGoldberg()));
            }
        }
        public static GoldbergGame ConvertGame(Game game, IPlayniteAPI PlayniteApi)
        {
            var goldbergGame = ConvertGames(new List<Game> { game }, PlayniteApi);
            return goldbergGame.FirstOrDefault();
        }
        public static List<GoldbergGame> ConvertGames(IEnumerable<Game> games, IPlayniteAPI PlayniteApi)
        {
            var GoldbergGames = new List<GoldbergGame>();
            foreach (var game in games)
            {
                string steamsettingspath = GameSteamSettingPath(game.GameId);
                string settingspath = GameSettingsPath(game.GameId);
                string fullpath = string.Empty;
                string ColdClientPath = Path.Combine(settingspath, "ColdClientLoader.ini");
                if (game.CoverImage != null)
                {
                    fullpath = Path.Combine(PlayniteApi.Database.GetFullFilePath(game.CoverImage));
                }
                var goldbergGame = new GoldbergGame(settingspath, steamsettingspath, PlayniteApi)
                {
                    Name = game.Name,
                    AppID = game.GameId,
                    Game = game,
                    ConfigsAppIniPath = Path.Combine(steamsettingspath, "configs.app.ini"),
                    ConfigsApp = new ConfigsApp(Path.Combine(steamsettingspath, "configs.app.ini")),
                    ConfigsOverlay = new ConfigsOverlay(Path.Combine(steamsettingspath, "configs.overlay.ini")),
                    ConfigsMain = new ConfigsMain(Path.Combine(steamsettingspath, "configs.main.ini")),
                    ConfigsColdClientLoader = new ConfigsColdClientLoader(ColdClientPath),
                    ConfigsEmu = new ConfigsEmu(Path.Combine(settingspath, "configs.emu.ini")),
                };
                GoldbergGames.Add(goldbergGame);
            }
            return GoldbergGames;
        }
        public static void ResetAchievementFile(IEnumerable<Game> games, IPlayniteAPI PlayniteApi)
        {
            GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
            int count = 0;
            PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
            {
                foreach (var game in games)
                {
                    string AchievementGameJSONPath = Path.Combine(GameAppdataPath(game.GameId), "achievements.json");
                    string AchievementDBPath = Path.Combine(AchievementWatcherAppData, "steam_cache", "data", $"{game.GameId}.db");
                    try
                    {
                        FileSystem.DeleteFile(AchievementGameJSONPath);
                        FileSystem.DeleteFile(AchievementDBPath);
                        count++;
                    }
                    catch { }
                }
            }, progress);
            PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_ResetAchievementSuccess"), count));
        }
        public static bool InjectJob(GoldbergGame game, IPlayniteAPI PlayniteApi, out string message, string apikey)
        {
            if (game.ConfigsEmu.Architecture == string.Empty && !game.ConfigsColdClientLoader.IgnoreLoaderArchDifference)
            {
                if (!InternetCommon.Internet.IsInternetAvailable())
                {
                    message = ResourceProvider.GetString("LOCSEU_ConnectionUnavailable");
                    return false;
                }
                game.GenerateArchitecture = true;
                GoldbergGenerator.GenerateInfo(game, apikey, PlayniteApi);
                if (!game.ConfigsEmu.Architecture.Equals("64") || !game.ConfigsEmu.Architecture.Equals("32"))
                {
                    game.ConfigsColdClientLoader.IgnoreLoaderArchDifference = true;
                }
            }
            if (!CopyColdClientIni(game, PlayniteApi, out message, apikey))
            {
                return false;
            }
            bool SettingsExists = FileSystem.DirectoryExists(GameSteamSettingPath(game.AppID));
            if (SettingsExists)
            {
                if (!CreateSymbolicSteamSettings(game.Game))
                {
                    message = ResourceProvider.GetString("LOCSEU_SymlinkErrorSteamSettings");
                    return false;
                }
            }
            return true;
        }
        private static bool CopyColdClientIni(GoldbergGame game, IPlayniteAPI PlayniteApi, out string message, string apikey)
        {
            if (!FileSystem.FileExists(Path.Combine(GameSettingsPath(game.AppID), "ColdClientLoader.ini"))
                || game.ConfigsColdClientLoader.Exe.IsNullOrWhiteSpace()
                || game.ConfigsColdClientLoader.AppId.IsNullOrWhiteSpace())
            {
                if (!InternetCommon.Internet.IsInternetAvailable())
                {
                    message = ResourceProvider.GetString("LOCSEU_ConnectionUnavailable");
                    return false;
                }
                game.GenerateColdClient = true;
                GoldbergGenerator.GenerateInfo(game, apikey, PlayniteApi);
            }
            if (!game.ConfigsColdClientLoader.Exe.IsNullOrWhiteSpace()
                && !game.ConfigsColdClientLoader.AppId.IsNullOrWhiteSpace())
            {
                if (FileSystem.CopyFile($"{GameSettingsPath(game.AppID)}\\ColdClientLoader.ini", ColdClientIni, true))
                {
                    message = string.Empty;
                    return true;
                }
                else
                {
                    message = ResourceProvider.GetString("LOCSEU_CopyErrorColdClientLoader");
                    return false;
                }
            }
            else
            {
                message = ResourceProvider.GetString("LOCSEU_ColdClientExeOrAppidNotFound");
                return false;
            }
        }
        private static bool CreateSymbolicSteamSettings(Game game)
        {
            if (FileSystem.DirectoryExists(SteamSettingsPath))
            {
                FileSystem.DeleteDirectory(SteamSettingsPath);
            }
            if (FileSystem.CreateSymbolicLink(SteamSettingsPath, GameSteamSettingPath(game.GameId)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool SetSymbolicSteamToAppdata(Game game, IPlayniteAPI PlayniteApi, bool Enable = true)
        {
            string goldberggamepath = GameAppdataPath(game.GameId);
            string steamuserdatagamepath = GameUserDataSteamPath(game.GameId);
            if (!Enable)
            {
                var savesPath = Directory.GetDirectories(GameAppdataPath(game.GameId), "*", SearchOption.TopDirectoryOnly);
                foreach (var dir in savesPath)
                {
                    if (FileSystem.IsSymbolicLink(dir))
                    {
                        FileSystem.DeleteDirectory(dir);
                    }
                }
                return false;
            }
            try
            {
                var userdatappid = Directory.GetDirectories(steamuserdatagamepath, "*", SearchOption.TopDirectoryOnly);
                foreach (var dir in userdatappid)
                {
                    string rootdirectory = Path.GetFileName(dir);
                    string goldbergtarget = Path.Combine(goldberggamepath, rootdirectory);
                    if (FileSystem.DirectoryExists(goldbergtarget))
                    {
                        var dialog = PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_ReplaceAppDataSymbolicWarning"), goldbergtarget, game.Name),
                            "Steam Emu Utility", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                        if (dialog == MessageBoxResult.Yes)
                        {
                            string backupPath = Path.Combine(GameSettingsPath(game.GameId), "Saves", Goldberg.ConfigsUser.ID, Path.GetFileName(goldbergtarget));
                            FileSystem.CopyDirectory(goldbergtarget, backupPath);
                            try
                            {
                                FileSystem.DeleteDirectory(goldbergtarget);
                            }
                            catch { continue; }
                        }
                        else if (dialog == MessageBoxResult.No)
                        {
                            return false;
                        }
                    }
                    if (!FileSystem.CreateSymbolicLink(goldbergtarget, dir))
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_SymbolicDeveloperOffError"));
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
