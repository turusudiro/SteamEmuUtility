using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DownloaderCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamCommon.Models;
using SteamKit2;
using static GoldbergCommon.Goldberg;
using static GoldbergCommon.GoldbergTasks;

namespace GoldbergCommon
{
    internal class GoldbergGenerator
    {
        private const string schemaUrl = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?";
        public static void GenerateGoldbergConfig(object selectedGames, IPlayniteAPI PlayniteApi, string apikey)
        {
            List<GoldbergGame> games = selectedGames is IEnumerable<Game> Games
                ? ConvertGames(Games, PlayniteApi)
                    : selectedGames is IEnumerable<GoldbergGame> otherGames
                        ? otherGames.ToList() : null;
            if (games == null)
            {
                return;
            }
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility", true);
            progressOptions.IsIndeterminate = false;
            using (var steam = new SteamService())
            {
                PlayniteApi.Dialogs.ActivateGlobalProgress(async (progress) =>
                {
                    progress.CancelToken.Register(() =>
                    {
                        steam.Dispose();
                        return;
                    });
                    int total = games.Count;
                    int configured = 0;
                    foreach (var game in games)
                    {
                        progress.Text = $"Configuring {game.Name}";

                        var tasks = new List<Task> {
                        Task.Run(() => { GenerateSteamSettings(game); }, progress.CancelToken),
                        };
                        if (game.ReconfigureGoldberg || !game.GoldbergExists)
                        {
                            game.AppInfo = SteamUtilities.GetAppIdInfo(game.AppID, steam, progress);
                            if (game.AppInfo == null)
                            {
                                continue;
                            }
                            tasks.AddRange(new List<Task>
                            {
                                Task.Run(() => { GenerateColdClient(game, progress); }, progress.CancelToken),
                                Task.Run(() => { GenerateController(game, steam, progress); }, progress.CancelToken),
                                Task.Run(() => { GenerateDLC(game, steam, progress, apikey); }, progress.CancelToken),
                                Task.Run(() => { GenerateAchievement(game, progress, apikey); }, progress.CancelToken),
                                Task.Run(() => { GenerateDepots(game, progress); }, progress.CancelToken),
                                Task.Run(() => { GenerateBuildID(game, progress); }, progress.CancelToken),
                                Task.Run(() => { GenerateSupportedLanguages(game, progress); }, progress.CancelToken),
                            });
                        }
                        while (!Task.WhenAll(tasks).IsCompleted)
                        {
                            if (progress.CancelToken.IsCancellationRequested)
                            {
                                return;
                            }
                            await Task.Delay(500);
                        }
                        progress.ProgressMaxValue = total;
                        progress.CurrentProgressValue = configured += 1;
                    }
                }, progressOptions);
            }
        }
        private static void GenerateSupportedLanguages(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.Text = $"Configuring {game.Name} : Configuring supported languages";
            string path = GameSteamSettingPath(game.AppID);
            if (game.AppInfo?.Common?.SupportedLanguages?.Keys?.Count >= 1)
            {
                FileSystem.WriteStringLinesToFile(Path.Combine(path, "supported_languages.txt"), game.AppInfo.Common.SupportedLanguages.Keys.OrderBy(x => x));
            }
        }
        private static void GenerateBuildID(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.Text = $"Configuring {game.Name} : Configuring Build ID";
            string path = GameSteamSettingPath(game.AppID);
            if (!string.IsNullOrEmpty(game.AppInfo?.Depots?.Branches?.Public?.BuildId))
            {
                FileSystem.TryWriteText(Path.Combine(path, "build_id.txt"), game.AppInfo.Depots.Branches.Public.BuildId);
            }
        }
        private static void GenerateDepots(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.Text = $"Configuring {game.Name} : Configuring depots";
            string path = GameSteamSettingPath(game.AppID);
            if (game.AppInfo?.Depots?.depots?.Keys?.Count >= 1)
            {
                IEnumerable<string> depots = game.AppInfo.Depots.depots.Keys;
                FileSystem.WriteStringLinesToFile(Path.Combine(path, "depots.txt"), depots);
            }
        }
        private static void GenerateController(GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress)
        {
            progress.Text = $"Configuring {game.Name} : Configuring controller";
            var controllerinfo = GetController(game, steam, progress);
            if (controllerinfo != null)
            {
                string path = Path.Combine(GameSteamSettingPath(game.AppID), "controller");
                if (!FileSystem.DirectoryExists(path))
                {
                    FileSystem.CreateDirectory(path);
                }
                foreach (var actions in controllerinfo)
                {
                    FileSystem.WriteStringLinesToFile(Path.Combine(path, actions.Key) + ".txt", actions.Value);
                }
            }
        }
        private static Dictionary<string, List<string>> GetController(GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress)
        {
            progress.Text = $"Configuring {game.Name} : Checking controller support for {game.Name}";
            if (game.AppInfo.Common.ControllerSupport == null)
            {
                return null;
            }
            progress.Text = $"Configuring {game.Name} : Found controller support for {game.Name}";
            if (game.AppInfo.Config.SteamControllerConfigDetails != null)
            {
                var controllerInfo = game.AppInfo.Config.SteamControllerConfigDetails;
                foreach (var id in controllerInfo)
                {
                    progress.Text = $"Configuring {game.Name} : accessing {id.Value.ControllerType} {game.Name}";
                    if (new string[] { "controller_xbox360", "controller_xboxone" }.Contains(id.Value.ControllerType) || id.Value.EnabledBranches.Contains("public"))
                    {
                        progress.Text = $"Configuring {game.Name} : Found xbox controller support for {game.Name}";
                        var publishedfiledetails = steam.GetPublishedFileDetails(uint.Parse(id.Key), progress);
                        if (publishedfiledetails != null)
                        {
                            var vdf = HttpDownloader.DownloadString(publishedfiledetails.file_url);
                            return ParseController.Parse(KeyValue.LoadFromString(vdf));
                        }

                    }
                }
            }
            if (game.AppInfo.Config.SteamControllerTouchConfigDetails != null)
            {
                var controllerInfo = game.AppInfo.Config.SteamControllerTouchConfigDetails;
                foreach (var id in controllerInfo)
                {
                    progress.Text = $"Configuring {game.Name} : accessing {id.Value.ControllerType} {game.Name}";
                    var publishedfiledetails = steam.GetPublishedFileDetails(uint.Parse(id.Key), progress);
                    if (publishedfiledetails != null)
                    {
                        var vdf = HttpDownloader.DownloadString(publishedfiledetails.file_url);
                        return ParseController.Parse(KeyValue.LoadFromString(vdf));
                    }
                }
            }
            // return default controller config if controller support found but cannot find any info in appinfo
            return new Dictionary<string, List<string>> { { "MenuControls", ParseController.KeymapDigitaldefault } }; ;
        }
        public static void GenerateSteamSettings(GoldbergGame game)
        {
            string settingspath = GameSteamSettingPath(game.AppID);
            if (!FileSystem.DirectoryExists(settingspath))
            {
                FileSystem.CreateDirectory(settingspath);
            }
            if (game.CustomBroadcast)
            {
                if (game.CustomBroadcastAddress != null && game.CustomBroadcastAddress != string.Empty)
                {
                    FileSystem.TryWriteText(Path.Combine(settingspath, "custom_broadcasts.txt"), game.CustomBroadcastAddress.Split(' ').ToList());
                }
            }
            else { FileSystem.DeleteFile(Path.Combine(settingspath, "custom_broadcasts.txt")); }

            if (game.DisableLANOnly)
            {
                FileSystem.TryWriteText(Path.Combine(settingspath, "disable_lan_only.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(settingspath, "disable_lan_only.txt")); }

            if (game.DisableNetworking)
            {
                FileSystem.TryWriteText(Path.Combine(settingspath, "disable_networking.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(settingspath, "disable_networking.txt")); }

            if (game.DisableOverlay)
            {
                FileSystem.DeleteFile(Path.Combine(settingspath, "disable_overlay_achievement_notification.txt"));
                FileSystem.DeleteFile(Path.Combine(settingspath, "disable_overlay_friend_notification.txt"));
                FileSystem.DeleteFile(Path.Combine(settingspath, "disable_overlay_warning.txt"));
                FileSystem.TryWriteText(Path.Combine(settingspath, "disable_overlay.txt"), string.Empty);
            }
            else
            {
                FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.AppID), "disable_overlay.txt"));
                if (game.DisableOverlayAchievement)
                {
                    FileSystem.TryWriteText(Path.Combine(settingspath, "disable_overlay_achievement_notification.txt"), string.Empty);
                }
                else { FileSystem.DeleteFile(Path.Combine(settingspath, "disable_overlay_achievement_notification.txt")); }
                if (game.DisableOverlayFriend)
                {
                    FileSystem.TryWriteText(Path.Combine(settingspath, "disable_overlay_friend_notification.txt"), string.Empty);
                }
                else { FileSystem.DeleteFile(Path.Combine(settingspath, "disable_overlay_friend_notification.txt")); }
                if (game.DisableOverlaylocalsave)
                {
                    FileSystem.TryWriteText(Path.Combine(settingspath, "disable_overlay_warning.txt"), string.Empty);
                }
                else { FileSystem.DeleteFile(Path.Combine(settingspath, "disable_overlay_warning.txt")); }
            }

            if (game.OfflineModeSteam)
            {
                FileSystem.TryWriteText(Path.Combine(settingspath, "offline.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(settingspath, "offline.txt")); }

            if (game.RunAsAdmin)
            {
                FileSystem.TryWriteText(Path.Combine(GameSettingsPath(game.AppID), "admin.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(GameSettingsPath(game.AppID), "admin.txt")); }
        }
        public static void GenerateDLC(GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress, string apikey)
        {
            string AlphaNumOnlyRegex = "[^0-9a-zA-Z]+";
            string gameName = game.AppInfo.Common.Name;
            List<string> dlcid = new List<string>();
            var appdetailsdlc = SteamAppDetails.GetAppDetailsStore(new List<string> { game.AppID }, progress).Result;
            if (!IStoreService.CacheExists(PluginPath))
            {
                IStoreService.GenerateCache(PluginPath, progress, apikey);
            }
            if (IStoreService.Cache1Day(PluginPath))
            {
                IStoreService.UpdateCache(PluginPath, progress, apikey);
            }
            var applistdetails = IStoreService.GetApplistDetails(PluginPath);
            if (game.AppInfo?.Depots?.depots != null && game.AppInfo.Depots.depots.Any(x => x.Value.dlcappid != null))
            {
                var dlcappid = game.AppInfo.Depots.depots
                    .Where(x => x.Value.dlcappid != null).Select(x => string.IsNullOrEmpty(x.Value?.dlcappid) ? string.Empty : x.Value?.dlcappid).ToList();

                dlcid.AddMissing(dlcappid.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            if (game.AppInfo?.Extended?.ListOfDLC != null)
            {
                dlcid.AddMissing(game.AppInfo.Extended.ListOfDLC);
            }
            if (appdetailsdlc[game.AppID]?.Data?.DLC?.Count >= 1)
            {
                dlcid.AddMissing(appdetailsdlc[game.AppID].Data.DLC);
            }
            if (applistdetails.Applist.Apps.Any(x => x.Name.IndexOf(game.AppInfo.Common.Name, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var filter = applistdetails.Applist.Apps.Where(x => Regex.Replace(x.Name, AlphaNumOnlyRegex, "")
                .Contains(Regex.Replace(gameName, AlphaNumOnlyRegex, ""))).ToList();
                dlcid.AddMissing(filter.Select(x => x.Appid.ToString()));
            }
            progress.IsIndeterminate = false;
            progress.ProgressMaxValue = dlcid.Count;
            progress.CurrentProgressValue = 0;
            List<string> dlcs = new List<string>();
            List<string> missingdlcid = new List<string>();
            foreach (var dlc in dlcid)
            {
                if (applistdetails.Applist.Apps.Any(x => x.Appid.ToString().Equals(dlc)))
                {
                    progress.Text = $"Configuring {game.Name} : Configuring {applistdetails.Applist.Apps.FirstOrDefault(x => x.Appid.ToString().Equals(dlc)).Name}";
                    progress.CurrentProgressValue++;
                    dlcs.Add($"{dlc}={applistdetails.Applist.Apps.FirstOrDefault(x => x.Appid.ToString().Equals(dlc)).Name}");
                    continue;
                }
                missingdlcid.Add(dlc);
            }
            if (missingdlcid.Count >= 1)
            {
                var tasks = new List<Task>();
                foreach (var dlc in missingdlcid)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        var appinfo = SteamUtilities.GetAppIdInfo(dlc, steam, progress);

                        if (appinfo != null)
                        {
                            if (appinfo.Common?.Name == null)
                            {
                                progress.Text = $"Configuring {game.Name} : Configuring {dlc}=Unknown App";
                                progress.CurrentProgressValue++;
                                dlcs.Add($"{dlc}=Unknown App");
                                return;
                            }

                            progress.Text = $"Configuring {game.Name} : Configuring {appinfo.Common.Name}";
                            progress.CurrentProgressValue++;
                            dlcs.Add($"{dlc}={appinfo.Common.Name}");
                        }
                    }));
                }
                Task.WhenAll(tasks).Wait();
            }
            if (dlcs.Count >= 1)
            {
                FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.AppID), "DLC.txt"), dlcs);
            }
        }
        public static void GenerateColdClient(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            string InstallDirectory = game.InstallDirectory;
            string SettingsPath = GameSettingsPath(game.AppID);
            string coldclient = Path.Combine(SettingsPath, "ColdClientLoader.ini");
            if (FileSystem.FileExists(coldclient))
            {
                return;
            }
            if (!FileSystem.DirectoryExists(SettingsPath))
            {
                FileSystem.CreateDirectory(SettingsPath);
            }
            if (game.AppInfo?.Config?.Launch?.Any(x => string.IsNullOrEmpty(x.Value.Executable)) == true)
            {
                return;
            }
            InstallDirectory = Path.Combine(InstallDirectory, Path.GetDirectoryName(game.AppInfo.Config.Launch
                    .Where(x => !string.IsNullOrEmpty(x.Value.Executable)).FirstOrDefault().Value.Executable));
            List<string> configs = new List<string>
            {
                @"[SteamClient]",
                $"Exe={Path.Combine(InstallDirectory, game.AppInfo.Config.Launch.FirstOrDefault().Value.Executable)}",
                $"ExeRunDir={InstallDirectory}",
                $"ExeCommandLine={game.AppInfo.Config.Launch.FirstOrDefault().Value?.Arguments}",
                $"AppId={game.AppID}",
                @"SteamClientDll=steamclient.dll",
                @"SteamClient64Dll=steamclient64.dll"
            };
            FileSystem.WriteStringLinesToFile(coldclient, configs);
        }
        public static void GenerateAchievement(GoldbergGame game, GlobalProgressActionArgs progress, string apikey)
        {
            string SteamSettingsPath = GameSteamSettingPath(game.AppID);
            progress.Text = $"Configuring {game.Name} : Searching Achievements";
            progress.CurrentProgressValue = 0;
            var job = GetSchema(game.AppID, apikey);
            var achievementPath = Path.Combine(SteamSettingsPath, "achievements_images");
            if (job != null && job.AvailableGameStats.Achievements.Count >= 1)
            {
                progress.ProgressMaxValue = job.AvailableGameStats.Achievements.Count;
                if (!FileSystem.DirectoryExists(achievementPath))
                {
                    FileSystem.CreateDirectory(achievementPath);
                }
                foreach (var ach in job.AvailableGameStats.Achievements)
                {
                    if (progress.CancelToken.IsCancellationRequested)
                    {
                        return;
                    }
                    progress.Text = $"Configuring {game.Name} : Downloading {ach.Name} achievement icon";
                    progress.CurrentProgressValue++;
                    string file = Path.GetFileName(ach.Icon);
                    string filegray = Path.GetFileName(ach.IconGray);
                    HttpDownloader.DownloadFile(ach.Icon, Path.Combine(achievementPath, file));
                    ach.Icon = $"achievements_images/{file}";
                    HttpDownloader.DownloadFile(ach.IconGray, Path.Combine(achievementPath, filegray));
                    ach.IconGray = $"achievements_images/{filegray}";
                }
                try
                {
                    FileSystem.WriteStringToFile(Path.Combine(SteamSettingsPath, "achievements.json"), Serialization.ToJson(job.AvailableGameStats.Achievements, true));
                }
                catch { }
            }
        }
        private static SchemaAppDetails GetSchema(string appid, string apikey)
        {
            if (GoldbergSettings.SteamWebApi == null)
            {
                return null;
            }
            var content = HttpDownloader.DownloadString($"{schemaUrl}key={apikey}&appid={appid}");
            if (content == null)
            {
                return null;
            }
            var json = Serialization.FromJson<Dictionary<string, SchemaAppDetails>>(content);
            var appDetails = json[json.Keys.First()];
            if (appDetails.AvailableGameStats.Achievements == null)
            {
                return null;
            }
            return appDetails;
        }
    }
}
