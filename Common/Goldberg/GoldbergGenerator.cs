using DlcManagerCommon;
using DownloaderCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
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

namespace GoldbergCommon
{
    public class GoldbergGenerator
    {
        private const string schemaUrl = "https://api.steampowered.com/ISteamUserStats/GetSchemaForGame/v2/?";

        public static void CheckAppInfo(GoldbergGame game, SteamService steam)
        {
            if (game.AppInfo == null)
            {
                game.AppInfo = SteamUtilities.GetApp(uint.Parse(game.Appid), steam);
            }
        }

        public static void GenerateInfo(GoldbergGame game, string apiKey, IPlayniteAPI PlayniteApi, SteamService steam)
        {
            Plugin plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(x => x.Id == Guid.Parse("a237961d-d688-4be9-9576-fb635547f854"));

            string pluginPath = plugin.GetPluginUserDataPath();

            string gameSettingsPath = Path.Combine(pluginPath, "GamesInfo", game.Appid);
            string gameSteamSettingsPath = Path.Combine(gameSettingsPath, "steam_settings");

            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility", false);
            progressOptions.IsIndeterminate = true;

            var tasks = new List<Task>();

            PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
            {
                progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_Processing"), game.Name);

                Action<string> progressUpdateHandler = (a) => progress.Text = a;
                steam.Callbacks.OnProgressUpdate += progressUpdateHandler;

                if (game.GenerateAllInfo)
                {
                    CheckAppInfo(game, steam);
                }
                else if (game.GenerateArchitecture
                         || game.GenerateBranches
                         || game.GenerateColdClient
                         || game.GenerateController
                         || game.GenerateDepots
                         || game.GenerateDLC
                         || game.GenerateSupportedLanguages)
                {
                    CheckAppInfo(game, steam);
                }
                steam.Callbacks.OnProgressUpdate -= progressUpdateHandler;
            }, progressOptions);

            progressOptions.Cancelable = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress(async (progress) =>
            {
                Action<string> progressUpdateHandler = (a) => progress.Text = a;
                steam.Callbacks.OnProgressUpdate += progressUpdateHandler;

                if (game.GenerateAllInfo)
                {
                    tasks.Add(Task.Run(() => { GenerateAchievement(gameSteamSettingsPath, game, progress, apiKey); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateArchitecture(game); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateBranches(gameSteamSettingsPath, game, progress); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateColdClient(gameSettingsPath, game, progress); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateController(gameSteamSettingsPath, game, steam, progress); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateDepots(gameSteamSettingsPath, game, progress); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateDLC(pluginPath, game, steam, progress, apiKey); }, progress.CancelToken));
                    tasks.Add(Task.Run(() => { GenerateSupportedLanguages(gameSteamSettingsPath, game, progress); }, progress.CancelToken));
                }
                else
                {
                    if (game.GenerateAchievements)
                    {
                        tasks.Add(Task.Run(() => { GenerateAchievement(gameSteamSettingsPath, game, progress, apiKey); }, progress.CancelToken));
                    }
                    if (game.GenerateArchitecture)
                    {
                        tasks.Add(Task.Run(() => { GenerateArchitecture(game); }, progress.CancelToken));
                    }
                    if (game.GenerateBranches)
                    {
                        tasks.Add(Task.Run(() => { GenerateBranches(gameSteamSettingsPath, game, progress); }, progress.CancelToken));
                    }
                    if (game.GenerateColdClient)
                    {
                        tasks.Add(Task.Run(() => { GenerateColdClient(gameSettingsPath, game, progress); }, progress.CancelToken));
                    }
                    if (game.GenerateController)
                    {
                        tasks.Add(Task.Run(() => { GenerateController(gameSteamSettingsPath, game, steam, progress); }, progress.CancelToken));
                    }
                    if (game.GenerateDepots)
                    {
                        tasks.Add(Task.Run(() => { GenerateDepots(gameSteamSettingsPath, game, progress); }, progress.CancelToken));
                    }
                    if (game.GenerateDLC)
                    {
                        tasks.Add(Task.Run(() => { GenerateDLC(pluginPath, game, steam, progress, apiKey); }, progress.CancelToken));
                    }
                    if (game.GenerateSupportedLanguages)
                    {
                        tasks.Add(Task.Run(() => { GenerateSupportedLanguages(gameSteamSettingsPath, game, progress); }, progress.CancelToken));
                    }
                }

                while (!Task.WhenAll(tasks).IsCompleted)
                {
                    if (progress.CancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await Task.Delay(500);
                }

                await Task.WhenAll(tasks);
                steam.Callbacks.OnProgressUpdate -= progressUpdateHandler;
            }, progressOptions);
        }

        private static void GenerateSupportedLanguages(string gameSteamSettingsPath, GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringSupportedLang"), game.Name);
            if (game.AppInfo.SupportedLanguages.Any())
            {
                FileSystem.WriteStringLinesToFile(Path.Combine(gameSteamSettingsPath, "supported_languages.txt"), game.AppInfo.SupportedLanguages.OrderBy(x => x));
            }
        }

        private static void GenerateBranches(string gameSteamSettingsPath, GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringBranches"), game.Name);

            if (game.AppInfo.Branches.Any())
            {
                game.SelectedBranch = "public";
                game.Branches = game.AppInfo.Branches.Select(x => x.Name).ToList();
                string branch = Serialization.ToJson(game.AppInfo.Branches, true);
                FileSystem.WriteStringToFile(Path.Combine(gameSteamSettingsPath, "branches.json"), branch);
            }
        }

        private static void GenerateDepots(string gameSteamSettingsPath, GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringDepots"), game.Name);
            if (game.AppInfo.Depots.Any())
            {
                FileSystem.WriteStringLinesToFile(Path.Combine(gameSteamSettingsPath, "depots.txt"), game.AppInfo.Depots.Keys.OrderBy(x => x).Select(x => x.ToString()));
            }
        }

        private static void GenerateController(string gameSteamSettingsPath, GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringController"), game.Name);
            var controllerinfo = GetController(game, steam, progress);
            if (controllerinfo != null)
            {
                string path = Path.Combine(gameSteamSettingsPath, "controller");
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
            progress.CancelToken.ThrowIfCancellationRequested();
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringCheckingController"), game.Name);
            if (!game.AppInfo.ControllerSupport.HasValue)
            {
                return null;
            }

            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringFoundController"), game.Name);
            if (game.AppInfo.SteamControllerConfigDetails != null)
            {
                var controllerInfo = game.AppInfo.SteamControllerConfigDetails;
                foreach (var id in controllerInfo)
                {
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringAccessingController"), game.Name, id.Value.ControllerType);
                    if (new string[] { "controller_xbox360", "controller_xboxone" }.Contains(id.Value.ControllerType) || id.Value.EnabledBranches.Contains("public"))
                    {
                        progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringFoundController"), game.Name);
                        var publishedfiledetails = steam.GetPublishedFileDetails(id.Key);
                        if (publishedfiledetails != null)
                        {
                            var vdf = HttpDownloader.DownloadString(publishedfiledetails.file_url);
                            return ParseController.Parse(KeyValue.LoadFromString(vdf));
                        }
                    }
                }
            }
            if (game.AppInfo.SteamControllerTouchConfigDetails != null)
            {
                var controllerInfo = game.AppInfo.SteamControllerTouchConfigDetails;
                foreach (var id in controllerInfo)
                {
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringAccessingController"), game.Name, id.Value.ControllerType);
                    var publishedfiledetails = steam.GetPublishedFileDetails(id.Key);
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

        public static void GenerateDLC(string pluginPath, GoldbergGame game, SteamService steam, GlobalProgressActionArgs progress, string apiKey)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            if (!DlcManager.HasGameInfo(pluginPath, game.Appid))
            {
                DlcManager.GenerateDLC(game.Appid, steam, progress, apiKey, pluginPath);
            }
            if (DlcManager.HasDLC(pluginPath, game.Appid))
            {
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

        private static void GenerateArchitecture(GoldbergGame game)
        {
            string exeRegex = @"(?!.*\/).*exe";
            string InstallDirectory = game.InstallDirectory;
            string executable = Regex.Match(game.AppInfo.Launch.FirstOrDefault().Executable, exeRegex).Value;
            if (FileSystem.FileExists(Path.Combine(InstallDirectory, executable)))
            {
                // Get Arch info for Game executable
                string Arch = FileSystem.GetArchitectureType(Path.Combine(InstallDirectory, game.AppInfo.Launch.FirstOrDefault().Executable));

                game.ConfigsEmu.Architecture = Arch;
            }
        }

        private static void GenerateColdClient(string gameSettingsPath, GoldbergGame game, GlobalProgressActionArgs progress)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            string InstallDirectory = game.InstallDirectory;
            string coldclient = Path.Combine(gameSettingsPath, "ColdClientLoader.ini");
            string exeRegex = @"(?!.*\/).*exe";
            if (!FileSystem.DirectoryExists(gameSettingsPath))
            {
                FileSystem.CreateDirectory(gameSettingsPath);
            }
            if (game.AppInfo?.Launch?.Any(x => string.IsNullOrEmpty(x.Executable)) == true)
            {
                return;
            }
            string executable = Regex.Match(game.AppInfo.Launch.FirstOrDefault().Executable, exeRegex).Value;
            InstallDirectory = Path.Combine(InstallDirectory, Path.GetDirectoryName(game.AppInfo.Launch
                    .Where(x => !string.IsNullOrEmpty(x.Executable)).FirstOrDefault().Executable));
            game.ConfigsColdClientLoader.Exe = Path.Combine(InstallDirectory, executable);
            game.ConfigsColdClientLoader.ExeRunDir = InstallDirectory;
            game.ConfigsColdClientLoader.Appid = game.Appid;
            game.ConfigsColdClientLoader.SteamClientDll = "steamclient.dll";
            game.ConfigsColdClientLoader.SteamClient64Dll = "steamclient64.dll";
        }

        private static void GenerateAchievement(string gameSteamSettingsPath, GoldbergGame game, GlobalProgressActionArgs progress, string apiKey)
        {
            progress.CancelToken.ThrowIfCancellationRequested();
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_ConfiguringAchievements"), game.Name);
            progress.CurrentProgressValue = 0;

            var job = GetSchema(game.Appid, apiKey);
            var achievementPath = Path.Combine(gameSteamSettingsPath, "achievements_images");
            if (job != null && job.AvailableGameStats.Achievements.Count >= 1)
            {
                progress.IsIndeterminate = false;
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
                    FileSystem.WriteStringToFile(Path.Combine(gameSteamSettingsPath, "achievements.json"), Serialization.ToJson(job.AvailableGameStats.Achievements, true));
                }
                catch { }
            }
        }

        private static SchemaAppDetails GetSchema(string appid, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                return null;
            }
            var content = HttpDownloader.DownloadString($"{schemaUrl}key={apiKey}&appid={appid}");
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
