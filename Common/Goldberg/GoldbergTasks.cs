using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using static GoldbergCommon.Goldberg;

namespace GoldbergCommon
{
    public class GoldbergTasks
    {
        public static List<GoldbergGames> ConvertGames(IEnumerable<Game> games, IPlayniteAPI PlayniteApi)
        {
            var GoldbergGames = new List<GoldbergGames>();
            foreach (var game in games)
            {
                string settingspath = GameSteamSettingPath(game);
                string fullpath = string.Empty;
                if (game.CoverImage != null)
                {
                    fullpath = Path.Combine(PlayniteApi.Database.GetFullFilePath(game.CoverImage));
                }
                var goldbergGame = new GoldbergGames
                {
                    Name = game.Name,
                    CoverImage = fullpath,
                    Game = game,
                    CustomBroadcast = FileSystem.FileExists(Path.Combine(settingspath, "custom_broadcasts.txt")),
                    CustomBroadcastAddress = FileSystem.FileExists(Path.Combine(settingspath, "custom_broadcasts.txt")) ?
                    FileSystem.ReadStringFromFile(Path.Combine(settingspath, "custom_broadcasts.txt")).TrimEnd() : string.Empty,
                    DisableLANOnly = FileSystem.FileExists(Path.Combine(settingspath, "disable_lan_only.txt")),
                    DisableNetworking = FileSystem.FileExists(Path.Combine(settingspath, "disable_networking.txt")),
                    DisableOverlay = FileSystem.FileExists(Path.Combine(settingspath, "disable_overlay.txt")),
                    DisableOverlayAchievement = FileSystem.FileExists(Path.Combine(settingspath, "disable_overlay_achievement_notification")),
                    DisableOverlayFriend = FileSystem.FileExists(Path.Combine(settingspath, "disable_overlay_friend_notification.txtdisable_overlay_friend_notification.txt")),
                    DisableOverlaylocalsave = FileSystem.FileExists(Path.Combine(settingspath, "disable_overlay_warning.txt")),
                    OfflineModeSteam = FileSystem.FileExists(Path.Combine(settingspath, "offline.txt")),
                    RunAsAdmin = FileSystem.FileExists(Path.Combine(GameSettingsPath(game), "admin.txt")),
                    DLCExists = FileSystem.FileExists(Path.Combine(settingspath, "DLC.txt")),
                    AchievementsExists = FileSystem.FileExists(Path.Combine(settingspath, "achievements.json")),
                    SettingsExists = FileSystem.DirectoryExists(GameSettingsPath(game))
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
        public static bool InjectJob(Game game, IPlayniteAPI PlayniteApi)
        {
            if (!CopyColdClientIni(game, PlayniteApi))
            {
                return false;
            }
            if (!CreateSymbolicSteamSettings(game))
            {
                return false;
            }
            if (GoldbergSettings.SymbolicLinkAppdata && !CreateSymbolicSteamToAppdata(game, PlayniteApi))
            {
                return false;
            }
            return true;
        }
        public static bool CopyColdClientIni(Game game, IPlayniteAPI PlayniteApi)
        {
            if (!FileSystem.FileExists(Path.Combine(GameSettingsPath(game), "ColdClientLoader.ini")))
            {
                List<Game> games = new List<Game> { game };
                GoldbergGenerator.GenerateGoldbergConfig(games, PlayniteApi);
            }
            try
            {
                FileSystem.CopyFile($"{GameSettingsPath(game)}\\ColdClientLoader.ini", ColdClientIni, true);
                return true;
            }
            catch { return false; }
        }
        public static bool CreateSymbolicSteamSettings(Game game)
        {
            try
            {
                if (FileSystem.DirectoryExists(GameSteamSettingPath(game)))
                {
                    if (FileSystem.DirectoryExists(SteamSettingsPath))
                    {
                        FileSystem.DeleteDirectory(SteamSettingsPath, true);
                    }
                    if (FileSystem.CreateSymbolicLink(SteamSettingsPath, GameSteamSettingPath(game)))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch { return false; }
        }
        public static bool CreateSymbolicSteamToAppdata(Game game, IPlayniteAPI PlayniteApi)
        {
            string goldberggamepath = GameAppdataPath(game);
            string steamuserdatagamepath = GameUserDataSteamPath(game);
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
                                FileSystem.DeleteDirectory(goldbergtarget, true);
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
    }
}
