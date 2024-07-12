using DownloaderCommon;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using PluginsCommon;
using ProcessCommon;
using SteamCommon;
using SteamEmuUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static GreenLumaCommon.GreenLuma;

namespace GreenLumaCommon
{
    public class GreenLumaTasks
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        public static void CheckForUpdate(IPlayniteAPI PlayniteApi, SteamEmuUtility.SteamEmuUtility plugin)
        {
            string pluginPath = plugin.GetPluginUserDataPath();
            string pluginGreenLumaPath = Path.Combine(pluginPath, "GreenLuma");

            string rawString = HttpDownloader.DownloadString(Url);
            if (rawString != null && Regex.IsMatch(rawString, @"<title>.*?GreenLuma \d{4} \d.*<\/title>"))
            {
                var match = Regex.Match(rawString, @"<title>.*?GreenLuma \d{4} \d.*<\/title>");
                string result = Regex.Replace(Regex.Replace(match.Value, @"<.*?>", ""), @".*GreenLuma", "").Trim();
                if (string.IsNullOrEmpty(result))
                {
                    return;
                }
                var versionInfo = new GreenLumaVersion() { Version = result.Split(' ')[1] };

                var greenlumaVersion = Serialization.TryFromJsonFile(Path
                    .Combine(pluginGreenLumaPath, "Version.json"), out GreenLumaVersion lumaVersion) ? lumaVersion.Version : "0.0.0";

                if (versionInfo.Version == greenlumaVersion)
                {
                    return;
                }
                else
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(plugin.Id.ToString() + "GreenLuma", string.Format(ResourceProvider.GetString("LOCSEU_UpdateAvailable"), "GreenLuma"),
                    NotificationType.Info, () => ProcessUtilities.StartUrl(Url)));
                }
            }
        }
        private static CancellationTokenSource cancellationTokenSource;
        private static bool CleanTaskIsRunning;
        /// <summary>
        /// Clean GreenLuma Files
        /// <para>Clean all GreenLuma directoies and files, including restore the original files of Steam.</para>
        /// </summary>
        public static async Task CleanAfterSteamExit(string pluginPath, string steamDir, IEnumerable<DirectoryInfo> dirGreenLumaOnSteam, IEnumerable<FileInfo> fileGreenLumaOnSteam)
        {
            cancellationTokenSource = new CancellationTokenSource();

            CleanTaskIsRunning = true;

            try
            {
                while (Steam.IsSteamRunning())
                {
                    await Task.Delay(1000, cancellationTokenSource.Token);
                }
            }
            catch
            {
                CleanTaskIsRunning = false;
                cancellationTokenSource.Dispose();
                return;
            }

            CleanGreenLuma(pluginPath, steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam);
        }
        private static void CleanGreenLuma(string pluginPath, string steamDir, IEnumerable<DirectoryInfo> dirGreenLumaOnSteam, IEnumerable<FileInfo> fileGreenLumaOnSteam)
        {
            try
            {
                foreach (var dir in dirGreenLumaOnSteam)
                {
                    if (dir.Exists)
                    {
                        dir.Delete(true);
                    }
                }

                foreach (var file in fileGreenLumaOnSteam)
                {
                    if (Regex.IsMatch(file.Name, X64launcherRegex, RegexOptions.IgnoreCase))
                    {
                        var fileinfo = FileVersionInfo.GetVersionInfo(file.FullName);
                        bool fromValve = fileinfo.ProductName == "Steam" ? true : false;
                        if (fromValve)
                        {
                            continue;
                        }

                        FileInfo x64backup = FileSystem.GetFiles(Path.Combine(pluginPath, "Backup"),
                            X64launcherRegex, RegexOptions.IgnoreCase).FirstOrDefault();

                        FileSystem.CopyFile(x64backup.FullName, file.FullName);
                        continue;
                    }
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Dispose();
                }
            }
            catch { }
        }
        private static void CheckTask()
        {
            if (CleanTaskIsRunning)
            {
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch { }
                CleanTaskIsRunning = false;
            }
        }
        /// <summary>
        /// Clean Steam appcache
        /// <para>Delete Steam appcache/appinfo.vdf and appcache/packageinfo.vdf if GreenLuma not working for some reason.</para>
        /// </summary>
        private static void CleanAppCache(string steamDir)
        {
            string appinfo = Path.Combine(steamDir, "appcache\\appinfo.vdf");
            string packageinfo = Path.Combine(steamDir, "appcache\\packageinfo.vdf");
            string[] appcache = new string[]
            {
                appinfo,
                packageinfo
            };
            foreach (string file in appcache)
            {
                if (FileSystem.FileExists(file))
                {
                    FileSystem.DeleteFile(file, true);
                }
            }
        }
        /// <summary>
        /// Start DLLInjector.exe
        /// <para>Start DLLInjector.exe process on Steam directory</para>
        /// </summary>
        /// <param name="args">game args</param>
        /// <param name="maxAttempts">Attempts to run DLLInjector.exe if it fails</param>
        /// <param name="delay">Delay the start <c>in Milliseconds</c> of DLLInjector.exe to make sure Steam process is killed </param>
        private static bool StartInjector(string injectorPath, int maxAttempts, int delay, GreenLumaMode mode)
        {
            maxAttempts++;
            string fileName = injectorPath;
            Process process = ProcessUtilities.TryStartProcess(injectorPath, maxAttempts, delay);

            if (mode == GreenLumaMode.Stealth || mode == GreenLumaMode.Family)
            {
                bool dllinjectorRunning = process.HasExited && process.ExitCode == 0;

                return dllinjectorRunning ? true : false;
            }
            bool dllinjectorRunningAndError = ProcessUtilities.IsErrorDialog(process, process.ProcessName) && ProcessUtilities.IsProcessRunning("steam");
            if (ProcessUtilities.IsProcessRunning(process.ProcessName))
            {
                if (dllinjectorRunningAndError)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static void StartGreenLumaJob(IPlayniteAPI PlayniteApi, IEnumerable<string> appids, IEnumerable<FileInfo> greenlumaFiles)
        {
            Plugin plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(x => x.Id == Guid.Parse("a237961d-d688-4be9-9576-fb635547f854"));

            string pluginPath = plugin.GetPluginUserDataPath();
            string glPath = Path.Combine(pluginPath, "GreenLuma");


            SteamEmuUtilitySettings settings = plugin.LoadPluginSettings<SteamEmuUtilitySettings>();

            bool isSteamRunning = Steam.IsSteamRunning();

            var lastrun = Serialization.TryFromJsonFile(Path.Combine(pluginPath, "LastRun.json"), out GreenLumaLastRun content) ?
                content : new GreenLumaLastRun
                {
                    Appids = Enumerable.Empty<string>(),
                    FamilyMode = false,
                    ProcessID = string.Empty
                };

            bool applistConfigured = false;

            string steamProcessID = string.Empty;


            if (isSteamRunning)
            {
                steamProcessID = Steam.GetSteamProcessId();

                // assuming steam is running by plugin if steam already running with ProcessID from lastrun
                if (!string.IsNullOrEmpty(lastrun.ProcessID) && lastrun.ProcessID.Equals(steamProcessID))
                {
                    bool isInjectedDifferentMode = lastrun.FamilyMode || !lastrun.StealthMode;
                    // check if steam is injected with configured applist from lastrun
                    applistConfigured = ApplistConfigured(appids, lastrun.Appids);

                    if (isInjectedDifferentMode)
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamInjectedDiffMode"),
                            ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            CheckTask();
                            while (Steam.IsSteamRunning())
                            {
                                Steam.KillSteam();
                                Thread.Sleep(settings.MillisecondsToWait);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else if (applistConfigured)
                    {
                        return;
                    }
                    // if not then tell user steam is injected without configured applist
                    else
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamWithoutConfiguredApplist"),
                        ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            CheckTask();
                            while (Steam.IsSteamRunning())
                            {
                                Steam.KillSteam();
                                Thread.Sleep(settings.MillisecondsToWait);
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                // if those condition is not met, then assuming steam is running without any injector or running outside plugin
                else
                {
                    if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamIsRunning"),
                        ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        CheckTask();
                        while (Steam.IsSteamRunning())
                        {
                            Steam.KillSteam();
                            Thread.Sleep(settings.MillisecondsToWait);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            var argsList = new List<string>();
            if (settings.EnableSteamArgs)
            {
                argsList.Add(settings.SteamArgs);
            }

            string steamDir = Steam.GetSteamDirectory();

            string injectorPath = greenlumaFiles.FirstOrDefault(x => Regex.IsMatch(x.Name, InjectorRegex, RegexOptions.IgnoreCase)).FullName;

            string applistPath = Path.Combine(glPath, "applist");

            bool skipUpdate = settings.SkipUpdateStealth;

            string dll = greenlumaFiles.FirstOrDefault(x => Regex.IsMatch(x.Name, GreenLumaDLL86Regex, RegexOptions.IgnoreCase)).Name;


            var dirGreenLumaOnSteam = FileSystem.GetDirectories(steamDir, GreenLumaDirectoriesRegex, RegexOptions.IgnoreCase);

            var fileGreenLumaOnSteam = FileSystem.GetFiles(steamDir, GreenLumaFilesRegex, RegexOptions.IgnoreCase);

            if (dirGreenLumaOnSteam.Any() || fileGreenLumaOnSteam.Any())
            {
                CleanGreenLuma(pluginPath, steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam);
            }
            if (skipUpdate)
            {
                argsList.Add("-inhibitbootstrap");
            }
            try
            {
                GreenLumaGenerator.WriteAppList(appids, applistPath, settings.CleanApplist);
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ex.InnerException.Message);
                return;
            }
            GreenLumaGenerator.CreateDLLInjectorIni(pluginPath, GreenLumaMode.Stealth, Steam.GetSteamExecutable(), argsList, dll, glPath);
            if (settings.CleanAppCache)
            {
                CleanAppCache(steamDir);
            }

            if (!StartInjector(injectorPath, settings.MaxAttemptDLLInjector, settings.MillisecondsToWait, GreenLumaMode.Stealth))
            {
                PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_InjectorError"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            steamProcessID = Steam.GetSteamProcessId();

            // tell the lastrun to update its json file if all above executed without error, assuming steam is injected with configured appids
            UpdateLastRun(appids, GreenLumaMode.Stealth, pluginPath, steamProcessID);
        }
        public static void StartGreenLumaJob(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi, IEnumerable<string> appids, GreenLumaMode mode, IEnumerable<FileInfo> greenlumaFiles)
        {
            string appid = args.Game.GameId;

            Plugin plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(x => x.Id == Guid.Parse("a237961d-d688-4be9-9576-fb635547f854"));

            string pluginPath = plugin.GetPluginUserDataPath();
            string glPath = Path.Combine(pluginPath, "GreenLuma");


            SteamEmuUtilitySettings settings = plugin.LoadPluginSettings<SteamEmuUtilitySettings>();

            bool isSteamRunning = Steam.IsSteamRunning();

            var lastrun = Serialization.TryFromJsonFile(Path.Combine(pluginPath, "LastRun.json"), out GreenLumaLastRun content) ?
                content : new GreenLumaLastRun
                {
                    Appids = Enumerable.Empty<string>(),
                    FamilyMode = false,
                    ProcessID = string.Empty
                };

            bool applistConfigured = false;
            bool isInjectedDifferentMode = false;

            string steamProcessID = string.Empty;


            if (isSteamRunning)
            {
                steamProcessID = Steam.GetSteamProcessId();

                // assuming steam is running by plugin if steam already running with ProcessID from lastrun
                if (!string.IsNullOrEmpty(lastrun.ProcessID) && lastrun.ProcessID.Equals(steamProcessID))
                {
                    switch (mode)
                    {
                        case GreenLumaMode.Normal:
                            isInjectedDifferentMode = lastrun.FamilyMode || lastrun.StealthMode;
                            break;
                        case GreenLumaMode.Stealth:
                            isInjectedDifferentMode = lastrun.FamilyMode || !lastrun.StealthMode;
                            break;
                        case GreenLumaMode.Family:
                            isInjectedDifferentMode = lastrun.StealthMode || !lastrun.FamilyMode;
                            break;
                    }
                    // check if steam is injected with configured applist from lastrun
                    applistConfigured = ApplistConfigured(appids, lastrun.Appids);

                    if (isInjectedDifferentMode)
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamInjectedDiffMode"),
                            ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            CheckTask();
                            while (Steam.IsSteamRunning())
                            {
                                Steam.KillSteam();
                                Thread.Sleep(settings.MillisecondsToWait);
                            }
                        }
                        else
                        {
                            args.CancelStartup = true;
                            return;
                        }
                    }
                    else if (applistConfigured)
                    {
                        return;
                    }
                    // if not then tell user steam is injected without configured applist
                    else
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamWithoutConfiguredApplist"),
                        ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            CheckTask();
                            while (Steam.IsSteamRunning())
                            {
                                Steam.KillSteam();
                                Thread.Sleep(settings.MillisecondsToWait);
                            }
                        }
                        else
                        {
                            args.CancelStartup = true;
                            return;
                        }
                    }
                }
                // if those condition is not met, then assuming steam is running without any injector or running outside plugin
                else
                {
                    if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamIsRunning"),
                        ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        CheckTask();
                        while (Steam.IsSteamRunning())
                        {
                            Steam.KillSteam();
                            Thread.Sleep(settings.MillisecondsToWait);
                        }
                    }
                    else
                    {
                        args.CancelStartup = true;
                        return;
                    }
                }
            }

            var argsList = new List<string>()
            {
                $"-applaunch {appid}"
            };

            if (settings.EnableSteamArgs)
            {
                argsList.Add(settings.SteamArgs);
            }

            string steamDir = Steam.GetSteamDirectory();

            var injectorFile = greenlumaFiles.FirstOrDefault(x => Regex.IsMatch(x.Name, InjectorRegex, RegexOptions.IgnoreCase));

            string injectorPath = injectorFile.FullName;

            if (mode == GreenLumaMode.Stealth || mode == GreenLumaMode.Family)
            {
                string applistPath = Path.Combine(glPath, "applist");

                bool skipUpdate = (mode == GreenLumaMode.Stealth && settings.SkipUpdateStealth) || (mode == GreenLumaMode.Family && settings.SkipUpdateFamily);

                string dll = mode == GreenLumaMode.Stealth ?
                    greenlumaFiles.FirstOrDefault(x => Regex.IsMatch(x.Name, GreenLumaDLL86Regex, RegexOptions.IgnoreCase)).Name
                    :
                    greenlumaFiles.FirstOrDefault(x => Regex.IsMatch(x.Name, FamilyRegex, RegexOptions.IgnoreCase)).Name;

                var dirGreenLumaOnSteam = FileSystem.GetDirectories(steamDir, GreenLumaDirectoriesRegex, RegexOptions.IgnoreCase);

                var fileGreenLumaOnSteam = FileSystem.GetFiles(steamDir, GreenLumaFilesRegex, RegexOptions.IgnoreCase);

                if (dirGreenLumaOnSteam.Any() || fileGreenLumaOnSteam.Any())
                {
                    CleanGreenLuma(pluginPath, steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam);
                }
                if (skipUpdate)
                {
                    argsList.Add("-inhibitbootstrap");
                }
                try
                {
                    GreenLumaGenerator.WriteAppList(appids, applistPath, settings.CleanApplist);
                }
                catch (Exception ex)
                {
                    args.CancelStartup = true;
                    PlayniteApi.Dialogs.ShowErrorMessage(ex.InnerException.Message);
                    return;
                }
                GreenLumaGenerator.CreateDLLInjectorIni(pluginPath, mode, Steam.GetSteamExecutable(), argsList, dll, glPath);
            }
            else if (mode == GreenLumaMode.Normal)
            {
                string applistPath = Path.Combine(steamDir, "applist");
                injectorPath = Path.Combine(steamDir, injectorFile.Name);

                string appOwnershipTicketsPath = Path.Combine(pluginPath, "GamesInfo", "AppOwnershipTickets");
                string encryptedAppTicketsPath = Path.Combine(pluginPath, "GamesInfo", "EncryptedAppTickets");

                if (FileSystem.FileExists(Path.Combine(appOwnershipTicketsPath, $"Ticket.{appid}")) && settings.InjectAppOwnership)
                {
                    logger.Info("Found AppOwnershipTickets, copying...");
                    InjectAppOwnershipTickets(Path.Combine(appOwnershipTicketsPath, $"Ticket.{appid}"), steamDir);
                }
                if (FileSystem.FileExists(Path.Combine(encryptedAppTicketsPath, $"EncryptedTicket.{appid}")) && settings.InjectEncryptedApp)
                {
                    logger.Info("Found EncryptedAppTickets, copying...");
                    InjectEncryptedAppTickets(Path.Combine(encryptedAppTicketsPath, $"EncryptedTicket.{appid}"), steamDir);
                }


                FileInfo x64steam = FileSystem.GetFiles(steamDir, X64launcherRegex, RegexOptions.IgnoreCase).FirstOrDefault();

                var x64steamFileVersionInfo = FileVersionInfo.GetVersionInfo(x64steam.FullName);
                bool fromValve = x64steamFileVersionInfo.ProductName == "Steam" ? true : false;

                // if x64launcher is from steam, try to backup
                if (fromValve)
                {
                    string backupPath = Path.Combine(pluginPath, "Backup");
                    FileInfo x64backup = FileSystem.GetFiles(backupPath, X64launcherRegex, RegexOptions.IgnoreCase).FirstOrDefault();

                    // if there's no backup found, backup immediately
                    if (x64backup == null)
                    {
                        FileSystem.CopyFile(x64steam.FullName, Path.Combine(pluginPath, backupPath, x64steam.Name));
                    }
                    // if there's a backup but the files is not same from original steam, do a backup
                    else if (x64backup.Length != x64steam.Length)
                    {
                        FileSystem.CopyFile(x64steam.FullName, Path.Combine(pluginPath, backupPath, x64steam.Name));
                    }
                }

                try
                {
                    GreenLumaGenerator.WriteAppList(appids, applistPath, settings.CleanApplist);
                }
                catch (Exception ex)
                {
                    args.CancelStartup = true;
                    PlayniteApi.Dialogs.ShowErrorMessage(ex.InnerException.Message);
                    return;
                }

                string dll = greenlumaFiles.FirstOrDefault(x => Regex.IsMatch(x.Name, GreenLumaDLL86Regex, RegexOptions.IgnoreCase)).Name;

                GreenLumaGenerator.CreateDLLInjectorIni(pluginPath, mode, argsList, dll, steamDir);

                foreach (FileInfo file in greenlumaFiles)
                {
                    if (Regex.IsMatch(file.Name, X64launcherRegex, RegexOptions.IgnoreCase))
                    {
                        FileSystem.CopyFile(file.FullName, x64steam.FullName);
                        continue;
                    }
                    else if (Regex.IsMatch(file.Name, AchievementRegex, RegexOptions.IgnoreCase))
                    {
                        // try to get year value from greenluma dll file
                        Match match = Regex.Match(dll, @"\d{4}");
                        string year = string.Empty;

                        if (match.Success)
                        {
                            year = match.Value;
                        }
                        else
                        {
                            //if fail, use current year
                            year = DateTime.Now.Year.ToString();
                        }

                        string destinationPath = Path.Combine(steamDir, $"GreenLuma{year}_Files", file.Name);
                        FileSystem.CopyFile(file.FullName, destinationPath);
                        continue;
                    }
                    else
                    {
                        FileSystem.CopyFile(file.FullName, Path.Combine(steamDir, file.Name));
                    }
                }
            }

            if (settings.CleanAppCache)
            {
                CleanAppCache(steamDir);
            }

            if (!StartInjector(injectorPath, settings.MaxAttemptDLLInjector, settings.MillisecondsToWait, mode))
            {
                PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_InjectorError"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                args.CancelStartup = true;
                return;
            }

            steamProcessID = Steam.GetSteamProcessId();

            // tell the lastrun to update its json file if all above executed without error, assuming steam is injected with configured appids
            UpdateLastRun(appids, mode, pluginPath, steamProcessID);
        }
        private static void UpdateLastRun(IEnumerable<string> appids, GreenLumaMode mode, string pluginPath, string processID)
        {
            bool family = false;
            bool stealth = false;

            if (mode == GreenLumaMode.Family)
            {
                family = true;
            }

            if (mode == GreenLumaMode.Stealth)
            {
                stealth = true;
            }

            GreenLumaLastRun lastRun = new GreenLumaLastRun()
            {
                Appids = appids,
                FamilyMode = family,
                ProcessID = processID,
                StealthMode = stealth,
            };
            string path = Path.Combine(pluginPath, "LastRun.json");
            string json = Serialization.ToJson(lastRun, true);

            FileSystem.WriteStringToFile(path, json, true);
        }
        private static void InjectAppOwnershipTickets(string source, string destination)
        {
            string destinationFile = Path.Combine(destination, "AppOwnershipTickets", Path.GetFileName(source));
            if (!FileSystem.DirectoryExists(Path.GetDirectoryName(destinationFile)))
            {
                FileSystem.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }
            try
            {
                FileSystem.CopyFile(source, destinationFile);
            }
            catch { }
        }
        private static void InjectEncryptedAppTickets(string source, string destination)
        {
            string destinationFile = Path.Combine(destination, "EncryptedAppTickets", Path.GetFileName(source));
            if (!FileSystem.DirectoryExists(Path.GetDirectoryName(destinationFile)))
            {
                FileSystem.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }
            try
            {
                FileSystem.CopyFile(source, destinationFile, true);
            }
            catch { }
        }
        public static void LoadTicket(IPlayniteAPI PlayniteApi)
        {
            Plugin plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(x => x.Id == Guid.Parse("a237961d-d688-4be9-9576-fb635547f854"));

            string pluginPath = plugin.GetPluginUserDataPath();
            string glPath = Path.Combine(pluginPath, "GreenLuma");

            string appOwnershipTicketsPath = Path.Combine(pluginPath, "GamesInfo", "AppOwnershipTickets");
            string encryptedAppTicketsPath = Path.Combine(pluginPath, "GamesInfo", "EncryptedAppTickets");

            var files = PlayniteApi.Dialogs.SelectFiles($"{ResourceProvider.GetString("LOCSEU_TicketFiles")}|EncryptedTicket.*;Ticket.*");
            if (files == null)
            {
                return;
            }
            else if (!files.Any())
            {
                return;
            }

            List<string> appOwnershipTickets = files.Where(x => Regex.IsMatch(Path.GetFileName(x), @"^Ticket.*", RegexOptions.IgnoreCase)).ToList();
            List<string> encryptedAppTickets = files.Where(x => Regex.IsMatch(Path.GetFileName(x), @"^EncryptedTicket.*")).ToList();

            bool availableAppOwnership = appOwnershipTickets.Any();
            bool availableEncryptedApp = encryptedAppTickets.Any();
            logger.Info(availableAppOwnership.ToString());
            logger.Info(availableEncryptedApp.ToString());
            if (!FileSystem.DirectoryExists(appOwnershipTicketsPath) && availableAppOwnership)
            {
                FileSystem.CreateDirectory(appOwnershipTicketsPath);
            }
            if (!FileSystem.DirectoryExists(encryptedAppTicketsPath) && availableEncryptedApp)
            {
                FileSystem.CreateDirectory(encryptedAppTicketsPath);
            }
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility");
            PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
            {
                progress.ProgressMaxValue = appOwnershipTickets.Count + encryptedAppTickets.Count;
                foreach (string file in appOwnershipTickets)
                {
                    string destinationFile = Path.Combine(appOwnershipTicketsPath, Path.GetFileName(file));
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_CopyingFile"), Path.GetFileName(file), destinationFile);
                    progress.CurrentProgressValue++;
                    if (FileSystem.FileExists(destinationFile))
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_FileExists"), Path.GetFileName(file)),
                            "Steam Emu Utility", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.No)
                        {
                            progress.Text += string.Format(ResourceProvider.GetString("LOCSEU_SkippingFile"), Path.GetFileName(file));
                            progress.CurrentProgressValue++;
                            continue;
                        }
                    }
                    FileSystem.CopyFile(file, destinationFile, true);
                }
                foreach (string file in encryptedAppTickets)
                {
                    string destinationFile = Path.Combine(encryptedAppTicketsPath, Path.GetFileName(file));
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_CopyingFile"), Path.GetFileName(file), destinationFile);
                    progress.CurrentProgressValue++;
                    if (FileSystem.FileExists(destinationFile))
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_FileExists"), Path.GetFileName(file)), "Steam Emu Utility", MessageBoxButton.YesNo, MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                        {
                            progress.Text += string.Format(ResourceProvider.GetString("LOCSEU_SkippingFile"), Path.GetFileName(file));
                            progress.CurrentProgressValue++;
                            continue;
                        }
                    }
                    FileSystem.CopyFile(file, destinationFile, true);
                }
            }, progressOptions);
            if (availableAppOwnership && !availableEncryptedApp)
            {
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_CopiedFile"), appOwnershipTickets.Count));
            }
            else if (availableEncryptedApp && !availableAppOwnership)
            {
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_CopiedFile"), encryptedAppTickets.Count));
            }
            else if (availableEncryptedApp && availableAppOwnership)
            {
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_CopiedFile"), appOwnershipTickets.Count + encryptedAppTickets.Count));
            }
        }
    }
}
