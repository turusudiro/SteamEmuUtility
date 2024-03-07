using DownloaderCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamEmuUtility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using static GoldbergCommon.Goldberg;

namespace GoldbergCommon
{
    public class GoldbergTasks
    {
        public static void CheckForUpdate(IPlayniteAPI PlayniteApi, SteamEmuUtilitySettingsViewModel Settings)
        {
            string changelogpath = Path.Combine(GoldbergPath, "CHANGELOG.md");
            if (!FileSystem.FileExists(changelogpath))
            {
                return;
            }
            string regexPattern = @"\d{4}[\W_][0-9]+[\W_][0-9]+";
            string url = @"https://api.github.com/repos/otavepto/gbe_fork/releases/latest";
            string raw = HttpDownloader.DownloadString(url);
            dynamic json = Serialization.FromJson<object>(raw);
            string date = json.tag_name;
            var dateFixed = Regex.Match(date, regexPattern).Value.Replace("_", "/");
            var dateFileExists = Regex.Match(FileSystem.ReadStringFromFile(changelogpath), regexPattern);
            if (!string.IsNullOrEmpty(dateFileExists.Value))
            {
                if (dateFixed == dateFileExists.Value)
                {
                    return;
                }
                if (PlayniteApi.Dialogs.ShowMessage("Goldberg : Update available. " +
                        "Please download the latest version. " +
                        "Would you like to download it now?", "Goldberg",
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Settings.DownloadGoldberg();
                }
            }
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
                var goldbergGame = new GoldbergGame
                {
                    Name = game.Name,
                    CoverImage = fullpath,
                    AppID = game.GameId,
                    CustomBroadcast = FileSystem.FileExists(Path.Combine(steamsettingspath, "custom_broadcasts.txt")),
                    CustomBroadcastAddress = FileSystem.FileExists(Path.Combine(steamsettingspath, "custom_broadcasts.txt")) ?
                    FileSystem.ReadStringFromFile(Path.Combine(steamsettingspath, "custom_broadcasts.txt")).TrimEnd() : string.Empty,
                    DisableLANOnly = FileSystem.FileExists(Path.Combine(steamsettingspath, "disable_lan_only.txt")),
                    DisableNetworking = FileSystem.FileExists(Path.Combine(steamsettingspath, "disable_networking.txt")),
                    DisableOverlay = FileSystem.FileExists(Path.Combine(steamsettingspath, "disable_overlay.txt")),
                    DisableOverlayAchievement = FileSystem.FileExists(Path.Combine(steamsettingspath, "disable_overlay_achievement_notification")),
                    DisableOverlayFriend = FileSystem.FileExists(Path.Combine(steamsettingspath, "disable_overlay_friend_notification.txtdisable_overlay_friend_notification.txt")),
                    DisableOverlaylocalsave = FileSystem.FileExists(Path.Combine(steamsettingspath, "disable_overlay_warning.txt")),
                    OfflineModeSteam = FileSystem.FileExists(Path.Combine(steamsettingspath, "offline.txt")),
                    RunAsAdmin = FileSystem.FileExists(Path.Combine(settingspath, "admin.txt")),
                    SettingsExists = FileSystem.DirectoryExists(settingspath),
                    InstallDirectory = game.InstallDirectory,
                    GoldbergExists = IsGoldbergExists(game.GameId),
                    PatchSteamStub = FileSystem.FileExists(ColdClientPath) ? FileSystem.ReadStringFromFile(ColdClientPath).Contains("extra_dlls") : false,
                };
                GoldbergGames.Add(goldbergGame);
            }
            return GoldbergGames;
        }
        public static bool IsGoldbergExists(string appid)
        {
            string steamsettingspath = GameSteamSettingPath(appid);
            string settingspath = GameSettingsPath(appid);
            string achievements = Path.Combine(steamsettingspath, "achievements.json");
            string dlc = Path.Combine(steamsettingspath, "DLC.txt");
            string controller = Path.Combine(steamsettingspath, "controller");
            string coldclient = Path.Combine(settingspath, "ColdClientLoader.ini");
            string depots = Path.Combine(steamsettingspath, "depots.txt");
            string buildid = Path.Combine(steamsettingspath, "build_id.txt");
            string supportedlanguages = Path.Combine(steamsettingspath, "supported_languages.txt");
            if (FileSystem.FileExists(achievements) ||
                FileSystem.FileExists(dlc) ||
                Directory.Exists(controller) ||
                FileSystem.FileExists(coldclient) ||
                FileSystem.FileExists(depots) ||
                FileSystem.FileExists(buildid) ||
                FileSystem.FileExists(supportedlanguages))
            {
                return true; // At least one file or directory exists
            }

            return false; // None of the files or directories exist

        }
        public static void ResetAchievementFile(IEnumerable<Game> games, IPlayniteAPI PlayniteApi)
        {
            GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
            int count = 0;
            PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
            {
                foreach (var game in games)
                {
                    string AchievementGameJSONPath = Path.Combine(GameAppdataPath(game), "achievements.json");
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
            PlayniteApi.Dialogs.ShowMessage($"Achievement reset successful for {count} games.");
        }
        public static bool InjectJob(Game game, IPlayniteAPI PlayniteApi, out string message, string apikey)
        {
            message = string.Empty;
            if (!CopyColdClientIni(game, PlayniteApi, apikey))
            {
                message = "Cannot copy coldclient, make sure to generate config first.";
                return false;
            }
            if (!CreateSymbolicSteamSettings(game))
            {
                message = "Cannot create symlink for steam_settings.";
                return false;
            }
            if (GoldbergSettings.SymbolicLinkAppdata && !CreateSymbolicSteamToAppdata(game, PlayniteApi))
            {
                message = "Cannot create symlink for Goldberg appdarta.";
                return false;
            }
            return true;
        }
        public static bool CopyColdClientIni(Game game, IPlayniteAPI PlayniteApi, string apikey)
        {
            if (!FileSystem.FileExists(Path.Combine(GameSettingsPath(game.GameId), "ColdClientLoader.ini")))
            {
                List<Game> games = new List<Game> { game };
                GoldbergGenerator.GenerateGoldbergConfig(games, PlayniteApi, apikey);
            }
            try
            {
                FileSystem.CopyFile($"{GameSettingsPath(game.GameId)}\\ColdClientLoader.ini", ColdClientIni, true);
                return true;
            }
            catch { return false; }
        }
        public static bool CreateSymbolicSteamSettings(Game game)
        {
            try
            {
                if (FileSystem.DirectoryExists(GameSteamSettingPath(game.GameId)))
                {
                    if (FileSystem.DirectoryExists(SteamSettingsPath))
                    {
                        FileSystem.DeleteDirectory(SteamSettingsPath);
                    }
                    if (FileSystem.CreateSymbolicLink(SteamSettingsPath, GameSteamSettingPath(game.GameId)))
                    {
                        return true;
                    }
                }
                return true;
            }
            catch { return false; }
        }
        public static bool CreateSymbolicSteamToAppdata(Game game, IPlayniteAPI PlayniteApi)
        {
            string goldberggamepath = GameAppdataPath(game);
            string steamuserdatagamepath = GameUserDataSteamPath(game);
            try
            {
                var userdatappid = Directory.GetDirectories(steamuserdatagamepath, "*", SearchOption.TopDirectoryOnly);
                if (userdatappid.Count() <= 0)
                {
                    return true;
                }
                if (!FileSystem.DirectoryExists(goldberggamepath))
                {
                    FileSystem.CreateDirectory(goldberggamepath);
                }
                foreach (var dir in userdatappid)
                {
                    string rootdirectory = Path.GetFileName(dir);
                    string goldbergtarget = Path.Combine(goldberggamepath, rootdirectory);
                    if (FileSystem.DirectoryExists(goldbergtarget))
                    {
                        if (!FileSystem.IsSymbolicLink(goldbergtarget))
                        {
                            var dialog = PlayniteApi.Dialogs.ShowMessage($"There's an existing folder {goldbergtarget} for {game.Name}, do you want to overwrite it? (THIS ACTION MAY CAUSE LOSS YOUR SAVEDATA!)", "", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation);
                            if (dialog == System.Windows.MessageBoxResult.Yes)
                            {
                                try
                                {
                                    FileSystem.DeleteDirectory(goldbergtarget);
                                }
                                catch { continue; }
                            }
                            else if (dialog == System.Windows.MessageBoxResult.No)
                            {
                                return false;
                            }
                        }
                    }
                    if (FileSystem.DirectoryExists(goldbergtarget) && FileSystem.IsSymbolicLink(goldbergtarget))
                    {
                        continue;
                    }
                    if (FileSystem.CreateSymbolicLink(goldbergtarget, dir))
                    {
                        continue;
                    }
                }
                return true;
            }
            catch { return false; }
        }
    }
}
