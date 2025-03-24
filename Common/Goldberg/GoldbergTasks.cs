using DlcManagerCommon;
using DownloaderCommon;
using GoldbergCommon.Configs;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamEmuUtility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using static GoldbergCommon.Goldberg;

namespace GoldbergCommon
{
    public class GoldbergTasks
    {
        public static void CheckForUpdate(IPlayniteAPI PlayniteApi, SteamEmuUtilitySettingsViewModel Settings, SteamEmuUtility.SteamEmuUtility plugin)
        {
            string pluginPath = plugin.GetPluginUserDataPath();

            string gbPath = Path.Combine(pluginPath, "Goldberg");

            string verPath = Path.Combine(gbPath, "Version.txt");
            if (!FileSystem.FileExists(verPath))
            {
                return;
            }
            string url = @"https://api.github.com/repos/Detanup01/gbe_fork/releases/latest";
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
                string changelogJson = json.body;
                string changelog = changelogJson.HtmlToPlainText();
                PlayniteApi.Notifications.Add(new NotificationMessage(plugin.Id.ToString(),
                    string.Format(ResourceProvider.GetString("LOCSEU_UpdateAvailable"), "Goldberg"),
                    NotificationType.Info, () =>
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(changelog += $"{Environment.NewLine}{Environment.NewLine} {ResourceProvider.GetString("LOCSEU_Update")}?",
                            "Goldberg", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
                        {
                            Settings.DownloadGoldberg(pluginPath);
                        }
                    }));
            }
        }
        public static GoldbergGame ConvertGame(string pluginPath, Game game)
        {
            return ConvertGames(pluginPath, new List<Game> { game }).FirstOrDefault();
        }
        public static List<GoldbergGame> ConvertGames(string pluginPath, IEnumerable<Game> games)
        {
            var goldbergGames = new List<GoldbergGame>();
            foreach (var game in games)
            {
                string goldbergPath = GetGoldbergAppData();
                string gameSettingsPath = Path.Combine(pluginPath, "GamesInfo", game.GameId);
                string gameSteamSettingsPath = Path.Combine(gameSettingsPath, "steam_settings");

                var goldbergGame = new GoldbergGame(game, gameSettingsPath, gameSteamSettingsPath);
                goldbergGames.Add(goldbergGame);
            }
            return goldbergGames;
        }
        public static void ResetAchievementFile(IEnumerable<Game> games, IPlayniteAPI PlayniteApi)
        {
            string goldbergPath = GetGoldbergAppData();
            string achievementWatcherDir = GetAchievementWatcherAppData();

            GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
            int count = 0;
            PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
            {
                foreach (var game in games)
                {
                    string goldbergGameDataPath = Path.Combine(goldbergPath, game.GameId);
                    string AchievementGameJSONPath = Path.Combine(goldbergGameDataPath, "achievements.json");
                    string AchievementDBPath = Path.Combine(achievementWatcherDir, "steam_cache", "data", $"{game.GameId}.db");
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
        public static bool InjectJob(string pluginPath, GoldbergGame game, IPlayniteAPI PlayniteApi, out string message, SteamEmuUtilitySettings settings)
        {
            string apiKey = settings.SteamWebApi;

            string gamesteamSettingsPath = Path.Combine(pluginPath, "GamesInfo", game.Appid, "steam_settings");

            string steamSettingsPath = Path.Combine(pluginPath, "Goldberg", "steam_settings");

            if (game.ConfigsEmu.UnlockOnlySelectedDLC && !game.ConfigsApp.UnlockAll)
            {
                // if there are no dlc info in dlc manager try to generate it and try to unlock all available dlcs
                if (!DlcManager.HasGameInfo(pluginPath, game.Appid))
                {
                    using (var steam = new SteamService())
                    {
                        PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
                        {
                            Action<string> progressUpdateHandler = (a) => progress.Text = a;
                            steam.Callbacks.OnProgressUpdate += progressUpdateHandler;

                            progress.CancelToken.Register(() =>
                            {
                                steam.Callbacks.OnProgressUpdate -= progressUpdateHandler;
                                steam.Dispose();
                                return;
                            });
                            DlcManager.GenerateDLC(game.Appid, steam, progress, apiKey, pluginPath);
                            steam.Callbacks.OnProgressUpdate -= progressUpdateHandler;
                        }, new GlobalProgressOptions("Steam Emu Utility", false));
                    }
                }
                if (DlcManager.HasDLC(pluginPath, game.Appid))
                {
                    // remove all properties in app::dlcs section to clear the remaining dlcs and use the dlcs based on checked dlc in dlc manager
                    ConfigsCommon.RemoveSection(game.ConfigsApp.IniPath, "app::dlcs");

                    game.ConfigsApp.UnlockAll = false;
                    var dlcs = DlcManager.GetDLC(pluginPath, game.Appid);
                    var dict = new Dictionary<string, string>();
                    foreach (var dlc in dlcs)
                    {
                        if (dlc.Enable)
                        {
                            dict.Add(dlc.Appid.ToString(), dlc.Name);
                        }
                    }
                    game.ConfigsApp.DLC = dict;
                }
            }

            // if configs.emu.ini doesnt have value in Architecture key and the ColdClientLoader.ini doesnt allow inject in different arch, try to generate its info
            if (game.ConfigsEmu.Architecture == string.Empty && !game.ConfigsColdClientLoader.IgnoreLoaderArchDifference)
            {
                if (!InternetCommon.Internet.IsInternetAvailable())
                {
                    message = ResourceProvider.GetString("LOCSEU_ConnectionUnavailable");
                    return false;
                }
                game.GenerateArchitecture = true;
                using (var steam = new SteamService())
                {
                    GoldbergGenerator.GenerateInfo(game, apiKey, PlayniteApi, steam);
                }
                // if fail to obtain info, then allow the coldclient to inject in different arch
                if (!game.ConfigsEmu.Architecture.Equals("64") || !game.ConfigsEmu.Architecture.Equals("32"))
                {
                    game.ConfigsColdClientLoader.IgnoreLoaderArchDifference = true;
                }
            }
            if (!CopyColdClientIni(pluginPath, game, PlayniteApi, out message, apiKey))
            {
                return false;
            }

            bool SettingsExists = FileSystem.DirectoryExists(gamesteamSettingsPath);
            // check if game has steam_settings directory in plugin dir
            if (SettingsExists)
            {
                // if exist then create a symlink to coldclientloader dir to use the steam_settings upon injecting the game
                if (!CreateSymbolicSteamSettings(steamSettingsPath, gamesteamSettingsPath))
                {
                    message = ResourceProvider.GetString("LOCSEU_SymlinkErrorSteamSettings");
                    return false;
                }
            }

            return true;
        }
        private static bool CopyColdClientIni(string pluginPath, GoldbergGame game, IPlayniteAPI PlayniteApi, out string message, string apiKey)
        {
            string gameSettingsPath = Path.Combine(pluginPath, "GamesInfo", game.Appid);

            string coldclientIniPath = Path.Combine(pluginPath, "Goldberg", "ColdClientLoader.ini");

            string gamecoldclientIniPath = Path.Combine(gameSettingsPath, "ColdClientLoader.ini");

            if (!FileSystem.FileExists(gamecoldclientIniPath)
                || game.ConfigsColdClientLoader.Exe.IsNullOrWhiteSpace()
                || game.ConfigsColdClientLoader.Appid.IsNullOrWhiteSpace())
            {
                if (!InternetCommon.Internet.IsInternetAvailable())
                {
                    message = ResourceProvider.GetString("LOCSEU_ConnectionUnavailable");
                    return false;
                }
                game.GenerateColdClient = true;
                using (var steam = new SteamService())
                {
                    GoldbergGenerator.GenerateInfo(game, apiKey, PlayniteApi, steam);
                }
            }
            if (!game.ConfigsColdClientLoader.Exe.IsNullOrWhiteSpace()
                && !game.ConfigsColdClientLoader.Appid.IsNullOrWhiteSpace())
            {
                if (FileSystem.CopyFile(gamecoldclientIniPath, coldclientIniPath, true))
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
        private static bool CreateSymbolicSteamSettings(string steamSettingsPath, string gamesteamSettingsPath)
        {
            if (FileSystem.DirectoryExists(steamSettingsPath))
            {
                FileSystem.DeleteDirectory(steamSettingsPath);
            }
            if (FileSystem.CreateSymbolicLink(steamSettingsPath, gamesteamSettingsPath))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
