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

            if (!GreenLumaFilesExists(pluginGreenLumaPath, out _))
            {
                return;
            }
            string rawString = HttpDownloader.DownloadString(Url);
            if (rawString != null && Regex.IsMatch(rawString, @"<title>.*?GreenLuma \d{4} \d.*<\/title>"))
            {
                var match = Regex.Match(rawString, @"<title>.*?GreenLuma \d{4} \d.*<\/title>");
                string result = Regex.Replace(Regex.Replace(match.Value, @"<.*?>", ""), @".*GreenLuma", "").Trim();
                if (string.IsNullOrEmpty(result))
                {
                    return;
                }
                var versionInfo = new GreenLumaVersion() { Year = result.Split(' ')[0], Version = result.Split(' ')[1] };

                string currentYear = GetGreenLumaYear(pluginGreenLumaPath);

                var greenlumaVersion = Serialization.TryFromJsonFile(Path
                    .Combine(pluginPath, "GreenLuma", "Version.json"), out GreenLumaVersion lumaVersion) ? lumaVersion.Version : "0.0.0";

                if (versionInfo.Year == currentYear && versionInfo.Version == greenlumaVersion)
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
        /// <para>Clean all GreenLuma Steam directory, including restore the original files of Steam.</para>
        /// </summary>
        public static async Task CleanAfterSteamExit(string pluginPath)
        {
            string steamDir = Steam.GetSteamDirectory();

            var dirGreenLumaOnSteam = GetGreenLumaDirectories(steamDir);

            var fileGreenLumaOnSteam = GetGreenLumaFiles(steamDir);

            //if there are no GreenLuma files in steam then abort.
            if (!dirGreenLumaOnSteam.Any() && !fileGreenLumaOnSteam.Any())
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            CleanTaskIsRunning = true;

            try
            {
                while (Steam.IsSteamRunning() || IsDLLInjectorRunning())
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

            CleanGreenLuma(pluginPath, steamDir);
        }
        private static void CleanGreenLuma(string pluginPath, string steamDir)
        {
            string logs = Path.Combine(steamDir, "logs");

            if (FileSystem.DirectoryExists(logs))
            {
                foreach (var log in Directory.GetFiles(logs, "GreenLuma*"))
                {
                    FileSystem.DeleteFile(log);
                }
            }

            var dirGreenLumaOnSteam = GetGreenLumaDirectories(steamDir);

            var fileGreenLumaOnSteam = GetGreenLumaFiles(steamDir);

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
                    if (file.Name.Equals("x64launcher.exe"))
                    {
                        RestoreX64Launcher(pluginPath, steamDir);
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
        public static void StartGreenLumaJob(IPlayniteAPI PlayniteApi, IEnumerable<string> appids)
        {
            Plugin plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(x => x.Id == Guid.Parse("a237961d-d688-4be9-9576-fb635547f854"));

            string pluginPath = plugin.GetPluginUserDataPath();
            string glPath = Path.Combine(pluginPath, "GreenLuma");

            string applistPath = Path.Combine(glPath, "applist");
            string injectorPath = Path.Combine(glPath, "DLLInjector.exe");

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
            bool isInjectorRunning = IsDLLInjectorRunning();

            string steamProcessID = string.Empty;

            if (isSteamRunning)
            {
                steamProcessID = Steam.GetSteamProcessId();

                // assuming steam is running by plugin if steam already running with ProcessID from lastrun
                if (!string.IsNullOrEmpty(lastrun.ProcessID) && lastrun.ProcessID.Equals(steamProcessID))
                {
                    // check if steam is injected with configured applist from lastrun
                    applistConfigured = ApplistConfigured(appids, lastrun.Appids);

                    if (applistConfigured)
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
                        CheckTask(); ;
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

                

            bool skipUpdate = settings.SkipUpdateStealth;

            if (IsGreenLumaPresentOnSteamDir())
            {
                CleanGreenLuma(pluginPath, steamDir);
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
            GreenLumaGenerator.CreateDLLInjectorIni(pluginPath, GreenLumaMode.Stealth, Steam.GetSteamExecutable(), argsList, glPath);
                

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
        public static void StartGreenLumaJob(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi, IEnumerable<string> appids, GreenLumaMode mode)
        {
            string appid = args.Game.GameId;

            Plugin plugin = PlayniteApi.Addons.Plugins.FirstOrDefault(x => x.Id == Guid.Parse("a237961d-d688-4be9-9576-fb635547f854"));

            string pluginPath = plugin.GetPluginUserDataPath();
            string glPath = Path.Combine(pluginPath, "GreenLuma");
            string applistPath = string.Empty;
            string injectorPath = string.Empty;

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
            bool isInjectorRunning = IsDLLInjectorRunning();

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
                            isInjectedDifferentMode = lastrun.FamilyMode || isInjectorRunning;
                            break;
                        case GreenLumaMode.Family:
                            isInjectedDifferentMode = !lastrun.FamilyMode || isInjectorRunning;
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
                        CheckTask(); ;
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

            if (mode == GreenLumaMode.Stealth || mode == GreenLumaMode.Family)
            {
                applistPath = Path.Combine(glPath, "applist");
                injectorPath = Path.Combine(glPath, "DLLInjector.exe");

                bool skipUpdate = (mode == GreenLumaMode.Stealth && settings.SkipUpdateStealth) || (mode == GreenLumaMode.Family && settings.SkipUpdateFamily);

                if (IsGreenLumaPresentOnSteamDir())
                {
                    CleanGreenLuma(pluginPath, steamDir);
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
                GreenLumaGenerator.CreateDLLInjectorIni(pluginPath, mode, Steam.GetSteamExecutable(), argsList, glPath);
            }
            else if (mode == GreenLumaMode.Normal)
            {
                applistPath = Path.Combine(steamDir, "applist");
                injectorPath = Path.Combine(steamDir, "DLLInjector.exe");

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
                BackupX64Launcher(pluginPath, steamDir);
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
                GreenLumaGenerator.CreateDLLInjectorIni(pluginPath, mode, argsList, steamDir);

                // Copy required NormalMode files into steam dir
                CopyGreenLumaNormalMode(pluginPath, steamDir);
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
        private static void CopyGreenLumaNormalMode(string pluginPath, string steamDir)
        {
            string glPath = Path.Combine(pluginPath, "GreenLuma");

            var files = GetGreenLumaFiles(glPath);
            var directories = GetGreenLumaDirectories(glPath);

            files = files.Where(x => x.Extension.Contains("dll") || x.Extension.Contains("wav") || x.Extension.Contains("exe"));
            try
            {
                string year = GetGreenLumaYear(glPath);

                foreach (var file in files)
                {
                    string destination = Path.Combine(steamDir, file.Name);

                    if (file.Name.Equals("AchievementUnlocked.wav"))
                    {
                        destination = Path.Combine(steamDir, $"GreenLuma{year}_Files", file.Name);
                        string destinationDir = Path.Combine(steamDir, $"GreenLuma{year}_Files");

                        if (!FileSystem.DirectoryExists(destinationDir))
                        {
                            FileSystem.CreateDirectory(destinationDir);
                        }
                        file.CopyTo(destination, true);
                    }
                    else if (file.Name.Equals("x64launcher.exe"))
                    {
                        destination = Path.Combine(steamDir, "bin", file.Name);
                        file.CopyTo(destination, true);
                    }
                    else
                    {
                        file.CopyTo(destination, true);
                    }
                }
            }
            catch { }
        }
        private static void BackupX64Launcher(string pluginPath, string steamDir)
        {
            var x64steam = new FileInfo(Path.Combine(steamDir, "bin\\x64launcher.exe"));
            var x64greenluma = new FileInfo(Path.Combine(pluginPath, "GreenLuma", "x64launcher.exe"));
            var x64backup = new FileInfo(Path.Combine(pluginPath, "Backup\\Steam\\bin\\x64launcher.exe"));

            if (!x64steam.Exists)
            {
                return;
            }

            if (!FileSystem.DirectoryExists(Path.Combine(pluginPath, "Backup\\Steam\\bin")))
            {
                FileSystem.CreateDirectory(Path.Combine(pluginPath, "Backup\\Steam\\bin"));
            }
            if (!x64backup.Exists)
            {
                x64steam.CopyTo(x64backup.FullName);
            }
            else if (x64steam.Length != x64backup.Length)
            {
                var fileinfo = FileVersionInfo.GetVersionInfo(x64steam.FullName);
                bool fromValve = fileinfo.ProductName == "Steam" ? true : false;
                if (fromValve)
                {
                    x64steam.CopyTo(x64backup.FullName, true);
                }
            }
        }
        private static void RestoreX64Launcher(string pluginpPath, string steamDir)
        {
            var x64steam = new FileInfo(Path.Combine(steamDir, "bin\\x64launcher.exe"));
            var x64greenluma = new FileInfo(Path.Combine(pluginpPath, "GreenLuma", "x64launcher.exe"));
            var x64backup = new FileInfo(Path.Combine(pluginpPath, "Backup\\Steam\\bin\\x64launcher.exe"));

            if (!x64backup.Exists || !x64greenluma.Exists)
            {
                return;
            }

            if (!x64backup.Exists)
            {
                return;
            }
            else if (!x64steam.Exists)
            {
                x64backup.CopyTo(x64steam.FullName);
            }
            else if (x64steam.Length == x64greenluma.Length)
            {
                x64backup.CopyTo(x64steam.FullName, true);
            }
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
