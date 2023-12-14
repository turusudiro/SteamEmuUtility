using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using static GoldbergCommon.Goldberg;
using static GoldbergCommon.GoldbergTasks;

namespace GoldbergCommon
{
    internal class GoldbergGenerator
    {
        private const string schemaUrl = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?";
        public static void GenerateGoldbergConfig(object selectedGames, IPlayniteAPI PlayniteApi)
        {
            List<GoldbergGames> games = selectedGames is IEnumerable<Game> Games
                ? ConvertGames(Games, PlayniteApi)
                    : selectedGames is IEnumerable<GoldbergGames> otherGames
                        ? otherGames.ToList() : null;
            if (games == null)
            {
                return;
            }
            var steam = new SteamService();
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility");
            progressOptions.Cancelable = true;
            PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
            {
                var AppInfo = SteamCMDApi.GetAppInfo(games.Select(x => x.Game.GameId), progress);
                if (AppInfo?.Data?.Count == games.Count())
                {
                    foreach (var game in games)
                    {
                        if (!AppInfo.Data.ContainsKey(game.Game.GameId))
                        {
                            continue;
                        }
                        GenerateColdClient(AppInfo, game.Game);
                        GenerateSteamSettings(game);
                        if (!game.DLCExists || game.ReconfigureDLC)
                        {
                            GenerateDLC(AppInfo, steam, game.Game, progress);
                        }
                        if (!game.AchievementsExists || game.ReconfigureAchievements)
                        {
                            GenerateAchievement(game.Game, progress);
                        }
                    }
                    return;
                }
                if (!steam.AnonymousLogin(progress))
                {
                    return;
                }
                AppInfo = steam.GetAppInfo(games.Select(x => uint.Parse(x.Game.GameId))
                    .Intersect(AppInfo.Data.Select(x => uint.Parse(x.Key))), progress, AppInfo);
                var productInfo = steam.GetAppInfo(games.Select(x => uint.Parse(x.Game.GameId)), progress);
                if (progress.CancelToken.IsCancellationRequested)
                {
                    PlayniteApi.Dialogs.ShowMessage("Canceled");
                    return;
                }
                if (productInfo == null)
                {
                    return;
                }
                foreach (var game in games)
                {
                    if (AppInfo?.Data != null && AppInfo.Data.ContainsKey(game.Game.GameId))
                    {
                        continue;
                    }
                    GenerateColdClient(productInfo, game.Game);
                    if (!game.DLCExists || game.ReconfigureDLC)
                    {
                        GenerateDLC(AppInfo, steam, game.Game, progress);
                    }
                    if (!game.AchievementsExists || game.ReconfigureAchievements)
                    {
                        GenerateAchievement(game.Game, progress);
                    }
                }
                steam.LogOff(progress);
            }, progressOptions);
        }
        public static void GenerateSteamSettings(GoldbergGames game)
        {
            if (game.CustomBroadcast)
            {
                if (game.CustomBroadcastAddress != null && game.CustomBroadcastAddress != string.Empty)
                {
                    FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "custom_broadcasts.txt"), game.CustomBroadcastAddress.Split(' ').ToList());
                }
            }
            else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "custom_broadcasts.txt")); }

            if (game.DisableLANOnly)
            {
                FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "disable_lan_only.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_lan_only.txt")); }

            if (game.DisableNetworking)
            {
                FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "disable_networking.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_networking.txt")); }

            if (game.DisableOverlay)
            {
                FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_achievement_notification.txt"));
                FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_friend_notification.txt"));
                FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_warning.txt"));
                FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay.txt"), string.Empty);
            }
            else
            {
                FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay.txt"));
                if (game.DisableOverlayAchievement)
                {
                    FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_achievement_notification.txt"), string.Empty);
                }
                else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_achievement_notification.txt")); }
                if (game.DisableOverlayFriend)
                {
                    FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_friend_notification.txt"), string.Empty);
                }
                else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_friend_notification.txt")); }
                if (game.DisableOverlaylocalsave)
                {
                    FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_warning.txt"), string.Empty);
                }
                else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "disable_overlay_warning.txt")); }
            }

            if (game.OfflineModeSteam)
            {
                FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game.Game), "offline.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(GameSteamSettingPath(game.Game), "offline.txt")); }

            if (game.RunAsAdmin)
            {
                FileSystem.TryWriteText(Path.Combine(GameSettingsPath(game.Game), "admin.txt"), string.Empty);
            }
            else { FileSystem.DeleteFile(Path.Combine(GameSettingsPath(game.Game), "admin.txt")); }
        }
        public static void GenerateDLC(AppInfo appinfo, SteamService steam, Game game, GlobalProgressActionArgs progress)
        {
            int appid = int.Parse(game.GameId);
            string AlphaNumOnlyRegex = "[^0-9a-zA-Z]+";
            string gameName = appinfo.Data[game.GameId].Common.Name;
            List<int> dlcid = new List<int>();
            var appdetailsdlc = SteamAppDetails.GetAppDetailsStore(new List<int> { appid }, progress).Result;
            if (!IStoreService.CacheExists(PluginPath))
            {
                IStoreService.GenerateCache(PluginPath, progress);
            }
            if (IStoreService.Cache1Week(PluginPath))
            {
                IStoreService.UpdateCache(PluginPath, progress);
            }
            var applistdetails = IStoreService.GetApplistDetails(PluginPath);
            if (appinfo.Data[game.GameId]?.Depots?.depots != null && appinfo.Data[game.GameId].Depots.depots.Any(x => x.Value.dlcappid != null))
            {
                var dlcappid = appinfo.Data[game.GameId].Depots.depots
                    .Where(x => x.Value.dlcappid != null).Select(x => int.TryParse(x.Value.dlcappid, out int result) ? result : 0).ToList();
                dlcid.AddMissing(dlcappid);
            }
            if (appinfo.Data[game.GameId]?.Extended?.ListOfDLC != null)
            {
                dlcid.AddMissing(appinfo.Data[game.GameId].Extended.ListOfDLC);
            }
            if (appdetailsdlc[appid]?.Data?.DLC?.Count >= 1)
            {
                dlcid.AddMissing(appdetailsdlc[appid].Data.DLC);
            }
            if (applistdetails.Applist.Apps.Any(x => x.Name.IndexOf(appinfo.Data[game.GameId].Common.Name, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var filter = applistdetails.Applist.Apps.Where(x => Regex.Replace(x.Name, AlphaNumOnlyRegex, "")
                .Contains(Regex.Replace(gameName, AlphaNumOnlyRegex, ""))).ToList();
                dlcid.AddMissing(filter.Select(x => x.Appid));
            }
            List<string> dlcs = new List<string>();
            List<int> missingdlcid = new List<int>();
            foreach (var dlc in dlcid)
            {
                if (applistdetails.Applist.Apps.Any(x => x.Appid.Equals(dlc)))
                {
                    progress.Text = $"Configuring {applistdetails.Applist.Apps.FirstOrDefault(x => x.Appid.Equals(dlc)).Name}";
                    dlcs.Add($"{dlc}={applistdetails.Applist.Apps.FirstOrDefault(x => x.Appid.Equals(dlc)).Name}");
                    continue;
                }
                missingdlcid.Add(dlc);
            }
            if (missingdlcid.Count >= 1)
            {
                List<int> missingdlcidseconds = new List<int>();
                appinfo = SteamCMDApi.GetAppInfo(missingdlcid.Select(x => x.ToString()), progress, appinfo);
                foreach (var dlc in missingdlcid)
                {
                    if (appinfo.Data.ContainsKey(dlc.ToString()))
                    {
                        if (appinfo.Data[dlc.ToString()].Common?.Name == null)
                        {
                            dlcs.Add($"{dlc}=Unknown App");
                            continue;
                        }
                        dlcs.Add($"{dlc}={appinfo.Data[dlc.ToString()].Common.Name}");
                        continue;
                    }
                    missingdlcidseconds.Add(dlc);
                }
                if (missingdlcidseconds.Count >= 1)
                {
                    appinfo = steam.GetAppInfo(missingdlcidseconds.Select(x => (uint)x), progress, appinfo);
                    foreach (var dlc in missingdlcidseconds)
                    {
                        if (appinfo.Data.ContainsKey(dlc.ToString()))
                        {
                            if (appinfo.Data[dlc.ToString()].Common?.Name == null)
                            {
                                dlcs.Add($"{dlc}=Unknown App");
                                continue;
                            }
                            dlcs.Add($"{dlc}={appinfo.Data[dlc.ToString()].Common.Name}");
                        }
                    }
                }
            }
            if (dlcs.Count >= 1)
            {
                FileSystem.TryWriteText(Path.Combine(GameSteamSettingPath(game), "DLC.txt"), dlcs);
            }
        }
        public static void GenerateColdClient(AppInfo productInfo, Game game)
        {
            string coldclient = Path.Combine(GameSettingsPath(game), "ColdClientLoader.ini");
            if (!FileSystem.DirectoryExists(GameSettingsPath(game)))
            {
                FileSystem.CreateDirectory(GameSettingsPath(game));
            }
            List<string> configs = new List<string>
            {
                @"[SteamClient]",
                $"Exe={Path.Combine(game.InstallDirectory, productInfo.Data[game.GameId].Config.Launch.FirstOrDefault().Value.Executable)}",
                $"ExeRunDir={game.InstallDirectory}",
                $"AppId={game.GameId}",
                @"SteamClientDll=steamclient.dll",
                @"SteamClient64Dll=steamclient64.dll"
            };
            FileSystem.WriteStringLinesToFile(coldclient, configs);
        }
        public static void GenerateAchievement(Game game, GlobalProgressActionArgs progress)
        {
            progress.Text = $"Searching Achievements for {game.Name}";
            progress.CurrentProgressValue = 0;
            var job = GetSchema(int.Parse(game.GameId)).Result;
            var achievementPath = Path.Combine(GameSteamSettingPath(game), "achievements_images");
            if (job != null && job.AvailableGameStats.Achievements.Count >= 1)
            {
                progress.ProgressMaxValue = job.AvailableGameStats.Achievements.Count;
                if (!FileSystem.DirectoryExists(achievementPath))
                {
                    FileSystem.CreateDirectory(achievementPath);
                }
                foreach (var ach in job.AvailableGameStats.Achievements)
                {
                    progress.Text = $"Downloading {ach.Name} achievement icon";
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
                    FileSystem.WriteStringToFile(Path.Combine(GameSteamSettingPath(game), "achievements.json"), Serialization.ToJson(job.AvailableGameStats.Achievements, true));
                }
                catch { }
            }
        }
        public static async Task<SchemaAppDetails> GetSchema(int appid)
        {
            if (GoldbergSettings.SteamWebApi == null)
            {
                return null;
            }
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{schemaUrl}key={SteamWebAPIKey}&appid={appid}");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var json = Serialization.FromJson<Dictionary<string, SchemaAppDetails>>(jsonResponse);
                    var appDetails = new SchemaAppDetails();
                    appDetails = json[json.Keys.First()];
                    if (appDetails.AvailableGameStats.Achievements == null)
                    {
                        return null;
                    }
                    return appDetails;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
