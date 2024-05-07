using DownloaderCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using PluginsCommon;
using SteamCommon;
using SteamCommon.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static GoldbergCommon.Goldberg;

namespace GoldbergCommon
{
    internal class GoldbergGenerator
    {
        private const string schemaUrl = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?";

        private static void CheckAppInfo(GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress)
        {
            if (game.AppInfo == null)
            {
                game.AppInfo = SteamUtilities.GetAppIdInfo(game.AppID, steam, progress);
            }
        }

        public static void GenerateInfo(GoldbergGame game, string apikey, IPlayniteAPI PlayniteApi)
        {
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
                    var tasks = new List<Task>();
                    if (game.GenerateAllInfo)
                    {
                        CheckAppInfo(game, steam, progress);
                        tasks.Add(Task.Run(() => { GenerateAchievement(game, progress, apikey); }, progress.CancelToken));
                        tasks.Add(Task.Run(() => { GenerateBuildID(game, progress); }, progress.CancelToken));
                        tasks.Add(Task.Run(() => { GenerateColdClient(game, progress); }, progress.CancelToken));
                        tasks.Add(Task.Run(() => { GenerateController(game, steam, progress); }, progress.CancelToken));
                        tasks.Add(Task.Run(() => { GenerateDepots(game, progress); }, progress.CancelToken));
                        tasks.Add(Task.Run(() => { GenerateDLC(game, steam, progress, apikey); }, progress.CancelToken));
                        tasks.Add(Task.Run(() => { GenerateSupportedLanguages(game, progress); }, progress.CancelToken));
                    }
                    else
                    {
                        if (game.GenerateAchievements)
                        {
                            tasks.Add(Task.Run(() => { GenerateAchievement(game, progress, apikey); }, progress.CancelToken));
                        }
                        if (game.GenerateArchitecture)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateArchitecture(game); }, progress.CancelToken));
                        }
                        if (game.GenerateBuildID)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateBuildID(game, progress); }, progress.CancelToken));
                        }
                        if (game.GenerateColdClient)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateColdClient(game, progress); }, progress.CancelToken));
                        }
                        if (game.GenerateController)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateController(game, steam, progress); }, progress.CancelToken));
                        }
                        if (game.GenerateDepots)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateDepots(game, progress); }, progress.CancelToken));
                        }
                        if (game.GenerateDLC)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateDLC(game, steam, progress, apikey); }, progress.CancelToken));
                        }
                        if (game.GenerateSupportedLanguages)
                        {
                            CheckAppInfo(game, steam, progress);
                            tasks.Add(Task.Run(() => { GenerateSupportedLanguages(game, progress); }, progress.CancelToken));
                        }
                    }
                    while (!Task.WhenAll(tasks).IsCompleted)
                    {
                        if (progress.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }
                        await Task.Delay(500);
                    }
                }, progressOptions);
            }
        }

        private static void GenerateSupportedLanguages(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringSupportedLang"), game.Name);
            string path = GameSteamSettingPath(game.AppID);
            if (game.AppInfo?.Common?.SupportedLanguages?.Keys?.Count >= 1)
            {
                FileSystem.WriteStringLinesToFile(Path.Combine(path, "supported_languages.txt"), game.AppInfo.Common.SupportedLanguages.Keys.OrderBy(x => x));
            }
        }

        private static void GenerateBuildID(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringBuildID"), game.Name);
            string path = GameSteamSettingPath(game.AppID);
            if (!string.IsNullOrEmpty(game.AppInfo?.Depots?.Branches?.Public?.BuildId))
            {
                game.ConfigsApp.buildid = game.AppInfo.Depots.Branches.Public.BuildId;
            }
        }

        private static void GenerateDepots(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringDepots"), game.Name);
            string path = GameSteamSettingPath(game.AppID);
            if (game.AppInfo?.Depots?.depots?.Keys?.Count >= 1)
            {
                IEnumerable<string> depots = game.AppInfo.Depots.depots.Keys;
                //FileSystem.WriteStringLinesToFile(Path.Combine(path, "depots.txt"), depots);
                FileSystem.WriteStringLinesToFile(Path.Combine(path, "depots.txt"), depots.OrderBy(x => int.Parse(x)));
            }
        }

        private static void GenerateController(GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringController"), game.Name);
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
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringCheckingController"), game.Name);
            if (game.AppInfo.Common.ControllerSupport == null)
            {
                return null;
            }
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringFoundController"), game.Name);
            progress.Text = $"Configuring {game.Name} : Found controller support for {game.Name}";
            if (game.AppInfo.Config.SteamControllerConfigDetails != null)
            {
                var controllerInfo = game.AppInfo.Config.SteamControllerConfigDetails;
                foreach (var id in controllerInfo)
                {
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringAccessingController"), game.Name, id.Value.ControllerType);
                    if (new string[] { "controller_xbox360", "controller_xboxone" }.Contains(id.Value.ControllerType) || id.Value.EnabledBranches.Contains("public"))
                    {
                        progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringFoundController"), game.Name);
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
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringAccessingController"), game.Name, id.Value.ControllerType);
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
            Dictionary<string, string> dlcs = new Dictionary<string, string>();
            List<string> missingdlcid = new List<string>();
            foreach (var dlc in dlcid)
            {
                if (applistdetails.Applist.Apps.Any(x => x.Appid.ToString().Equals(dlc)))
                {
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringDLC"), game.Name, applistdetails.Applist.Apps.FirstOrDefault(x => x.Appid.ToString().Equals(dlc)).Name);
                    progress.CurrentProgressValue++;
                    dlcs.Add(dlc, applistdetails.Applist.Apps.FirstOrDefault(x => x.Appid.ToString().Equals(dlc)).Name);
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
                                progress.Text = $"{string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringDLC"), game.Name, dlc)}=Unkown App";
                                progress.CurrentProgressValue++;
                                dlcs.Add(dlc, "Unknown App");
                                return;
                            }
                            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringDLC"), game.Name, appinfo.Common.Name);
                            progress.CurrentProgressValue++;
                            dlcs.Add(dlc, appinfo.Common.Name);
                        }
                    }));
                }
                Task.WhenAll(tasks).Wait();
            }
            if (dlcs.Count >= 1)
            {
                game.ConfigsApp.DLC = dlcs.OrderBy(x => int.Parse(x.Key)).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        private static void GenerateArchitecture(GoldbergGame game)
        {
            string exeRegex = @"(?!.*\/).*exe";
            string InstallDirectory = game.Game.InstallDirectory;
            string executable = Regex.Match(game.AppInfo.Config.Launch.FirstOrDefault().Value.Executable, exeRegex).Value;
            if (FileSystem.FileExists(Path.Combine(InstallDirectory, executable)))
            {
                // Get Arch info for Game executable
                string Arch = FileSystem.GetArchitectureType(Path.Combine(InstallDirectory, game.AppInfo.Config.Launch.FirstOrDefault().Value.Executable));

                game.ConfigsEmu.Architecture = Arch;
            }
        }

        private static void GenerateColdClient(GoldbergGame game, GlobalProgressActionArgs progress)
        {
            string InstallDirectory = game.Game.InstallDirectory;
            string SettingsPath = GameSettingsPath(game.AppID);
            string coldclient = Path.Combine(SettingsPath, "ColdClientLoader.ini");
            string exeRegex = @"(?!.*\/).*exe";
            if (!FileSystem.DirectoryExists(SettingsPath))
            {
                FileSystem.CreateDirectory(SettingsPath);
            }
            if (game.AppInfo?.Config?.Launch?.Any(x => string.IsNullOrEmpty(x.Value.Executable)) == true)
            {
                return;
            }
            string executable = Regex.Match(game.AppInfo.Config.Launch.FirstOrDefault().Value.Executable, exeRegex).Value;
            InstallDirectory = Path.Combine(InstallDirectory, Path.GetDirectoryName(game.AppInfo.Config.Launch
                    .Where(x => !string.IsNullOrEmpty(x.Value.Executable)).FirstOrDefault().Value.Executable));
            game.ConfigsColdClientLoader.Exe = Path.Combine(InstallDirectory, executable);
            game.ConfigsColdClientLoader.ExeRunDir = InstallDirectory;
            game.ConfigsColdClientLoader.AppId = game.AppID;
            game.ConfigsColdClientLoader.SteamClientDll = "steamclient.dll";
            game.ConfigsColdClientLoader.SteamClient64Dll = "steamclient64.dll";
        }

        private static void GenerateAchievement(GoldbergGame game, GlobalProgressActionArgs progress, string apikey)
        {
            string SteamSettingsPath = GameSteamSettingPath(game.AppID);
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringAchievements"), game.Name);
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
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringDownloadingAchievements"), game.Name, ach.Name);
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
