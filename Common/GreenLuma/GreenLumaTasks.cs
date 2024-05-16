using DownloaderCommon;
using Playnite.SDK;
using Playnite.SDK.Events;
using PluginsCommon;
using ProcessCommon;
using SteamCommon;
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
            if (!GreenLumaFilesExists(out _))
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

                if (versionInfo.Year == Year && versionInfo.Version == GreenLuma.Version)
                {
                    return;
                }
                else
                {
                    PlayniteApi.Notifications.Add(new NotificationMessage(plugin.Id.ToString(), string.Format(ResourceProvider.GetString("LOCSEU_UpdateAvailable"), "GreenLuma"),
                    NotificationType.Info, () => ProcessUtilities.StartUrl(Url)));
                }
            }
        }
        public static CancellationTokenSource Token { get; set; }
        private static void CleanGreenLumaStealthMode()
        {
            if (FileSystem.FileExists(Path.Combine(Steam.SteamDirectory, "user32.dll")))
            {
                FileSystem.DeleteFile(Path.Combine(Steam.SteamDirectory, "user32.dll"));
            }
        }
        private static void CleanGreenLumaNormalMode()
        {
            foreach (var file in GreenLumaFilesNormalMode)
            {
                string path = Path.Combine(Steam.SteamDirectory, file);
                if (FileSystem.DirectoryExists(path))
                {
                    DirectoryInfo dirinfo = new DirectoryInfo(path);
                    try { dirinfo.Delete(true); }
                    catch { }
                }
                if (Path.GetFileName(file).Equals("x64launcher.exe"))
                {
                    RestoreX64Launcher();
                    continue;
                }
                if (FileSystem.FileExists(path))
                {
                    FileSystem.DeleteFile(Path.Combine(Steam.SteamDirectory, file));
                }
            }
        }
        /// <summary>
        /// Clean GreenLuma Files
        /// <para>Clean all GreenLuma Steam directory, including restore the original files of Steam.</para>
        /// </summary>
        public static async Task CleanGreenLuma()
        {
            while (Steam.IsSteamRunning)
            {
                await Task.Delay(1000, Token.Token);
            }
            while (IsDLLInjectorRunning)
            {
                await Task.Delay(1000, Token.Token);
            }
            await Task.Run(() =>
            {
                string logs = Path.Combine(Steam.SteamDirectory, "logs");
                if (FileSystem.DirectoryExists(logs))
                {
                    foreach (var log in Directory.GetFiles(logs, "GreenLuma*"))
                    {
                        FileSystem.DeleteFile(log);
                    }
                }
                foreach (var file in GreenLumaFiles)
                {
                    string path = Path.Combine(Steam.SteamDirectory, file);

                    if (FileSystem.DirectoryExists(path))
                    {
                        FileSystem.DeleteDirectory(path);
                    }
                    if (Path.GetFileName(file).Equals("x64launcher.exe"))
                    {
                        RestoreX64Launcher();
                        continue;
                    }
                    if (FileSystem.FileExists(path))
                    {
                        FileSystem.DeleteFile(Path.Combine(Steam.SteamDirectory, file));
                    }
                }
            });
        }
        /// <summary>
        /// Clean Steam appcache
        /// <para>Delete Steam appcache/appinfo.vdf and appcache/packageinfo.vdf if GreenLuma not working for some reason.</para>
        /// </summary>
        public static void CleanAppCache()
        {
            string appinfo = Path.Combine(Steam.SteamDirectory, "appcache\\appinfo.vdf");
            string packageinfo = Path.Combine(Steam.SteamDirectory, "appcache\\packageinfo.vdf");
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
        public static bool StartInjector(OnGameStartingEventArgs args, int maxAttempts, int delay)
        {
            maxAttempts++;
            string FileName = Path.Combine(Steam.SteamDirectory, "DLLInjector.exe");
            string Arguments = $"-applaunch {args.Game.GameId}";
            string workingdir = Steam.SteamDirectory;
            Process process = ProcessUtilities.TryStartProcess(FileName, Arguments, workingdir, maxAttempts, delay);
            if (ProcessUtilities.IsProcessRunning(process.ProcessName))
            {
                if (ProcessUtilities.IsErrorDialog(process, process.ProcessName) && ProcessUtilities.IsProcessRunning("steam"))
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

        /// <summary>
        /// Copy GreenLuma NormalMode files
        /// <para>Copy GreenLuma NormalMode files into Steam directory, also create NoQuestion.bin</para>
        /// </summary>
        public static void GreenLumaNormalMode(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi, List<string> appids)
        {
            if (FileSystem.FileExists(Path.Combine(Steam.SteamDirectory, "user32.dll")))
            {
                if (Steam.IsSteamRunning)
                {
                    if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamInjectedDiffMode"),
                        ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        Token.Cancel();
                        while (Steam.IsSteamRunning)
                        {
                            _ = Steam.KillSteam;
                            Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                        }
                    }
                    else
                    {
                        args.CancelStartup = true;
                        return;
                    }
                }
                CleanGreenLumaStealthMode();
            }
            else if (GreenLumaNormalInjected && IsDLLInjectorRunning && Steam.IsSteamRunning && ApplistConfigured(appids))
            {
                return;
            }
            else if (GreenLumaNormalInjected && IsDLLInjectorRunning && Steam.IsSteamRunning && !ApplistConfigured(appids))
            {
                if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamWithoutConfiguredApplist"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Token.Cancel();
                    while (Steam.IsSteamRunning)
                    {
                        _ = Steam.KillSteam;
                        Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }
            else if (Steam.IsSteamRunning)
            {
                if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamIsRunning"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Token.Cancel();
                    while (Steam.IsSteamRunning)
                    {
                        _ = Steam.KillSteam;
                        Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }
            
            if (FileSystem.FileExists(Path.Combine(AppOwnershipTicketsPath, $"Ticket.{args.Game.GameId}")) && GreenLumaSettings.InjectAppOwnership)
            {
                logger.Info("Found AppOwnershipTickets, copying...");
                _ = InjectAppOwnershipTickets(Path.Combine(AppOwnershipTicketsPath, $"Ticket.{args.Game.GameId}"));
            }
            if (FileSystem.FileExists(Path.Combine(EncryptedAppTicketsPath, $"EncryptedTicket.{args.Game.GameId}")) && GreenLumaSettings.InjectEncryptedApp)
            {
                logger.Info("Found EncryptedAppTickets, copying...");
                _ = InjectEncryptedAppTickets(Path.Combine(EncryptedAppTicketsPath, $"EncryptedTicket.{args.Game.GameId}"));
            }
            BackupX64Launcher();

            if (GreenLumaSettings.CleanAppCache)
            {
                CleanAppCache();
            }
            try
            {
                if (GreenLumaSettings.CleanApplist && !FileSystem.IsDirectoryEmpty(Path.Combine(Steam.SteamDirectory, "applist")))
                {
                    FileSystem.DeleteDirectory(Path.Combine(Steam.SteamDirectory, "applist"));
                }
                if (!ApplistConfigured(appids))
                {
                    GreenLumaGenerator.WriteAppList(appids);
                }
                CopyGreenLumaNormalMode();
                GreenLumaGenerator.CreateDLLInjectorIni();
                FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, stealth), null);
            }
            catch (Exception ex) { PlayniteApi.Dialogs.ShowErrorMessage(ex.Message); args.CancelStartup = true; return; }
            if (!StartInjector(args, GreenLumaSettings.MaxAttemptDLLInjector, GreenLumaSettings.MillisecondsToWait))
            {
                PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_InjectorError"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                args.CancelStartup = true;
                return;
            }
        }
        /// <summary>
        /// Copy GreenLuma StealthMode files
        /// <para>Copy user32.dll file into Steam directory, also create NoQuestion.bin in applist folder </para>
        /// and run Steam with -inhibitbootstrap if Skip Steam Update on Stealth Mode is True
        /// </summary>
        public static void GreenLumaStealthMode(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi, List<string> appids)
        {
            if (GreenLumaNormalInjected)
            {
                if (Steam.IsSteamRunning || IsDLLInjectorRunning)
                {
                    if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamInjectedDiffMode"),
                        ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        Token.Cancel();
                        while (Steam.IsSteamRunning)
                        {
                            _ = Steam.KillSteam;
                            Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                        }
                    }
                    else
                    {
                        args.CancelStartup = true;
                        return;
                    }
                }
                CleanGreenLumaNormalMode();
            }
            else if (FileSystem.FileExists(Path.Combine(Steam.SteamDirectory, "user32.dll")) && Steam.IsSteamRunning && ApplistConfigured(appids))
            {
                return;
            }
            else if (FileSystem.FileExists(Path.Combine(Steam.SteamDirectory, "user32.dll")) && Steam.IsSteamRunning && !ApplistConfigured(appids))
            {
                if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamWithoutConfiguredApplist"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Token.Cancel();
                    while (Steam.IsSteamRunning)
                    {
                        _ = Steam.KillSteam;
                        Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }
            else if (Steam.IsSteamRunning)
            {
                if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("Steam is running, Restart steam with Injector?"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Token.Cancel();
                    while (Steam.IsSteamRunning)
                    {
                        _ = Steam.KillSteam;
                        Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }
            try
            {
                if (!FileSystem.DirectoryExists(Path.Combine(Steam.SteamDirectory, "applist")))
                {
                    FileSystem.CreateDirectory(Path.Combine(Path.Combine(Steam.SteamDirectory, "applist")));
                }
                if (GreenLumaSettings.CleanApplist && !FileSystem.IsDirectoryEmpty(Path.Combine(Steam.SteamDirectory, "applist")))
                {
                    FileSystem.DeleteDirectory(Path.Combine(Steam.SteamDirectory, "applist"));
                }
                if (!ApplistConfigured(appids))
                {
                    GreenLumaGenerator.WriteAppList(appids);
                }
                FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, "applist", stealth), null);
                FileSystem.CopyFile(user32, Path.Combine(Steam.SteamDirectory, Path.GetFileName(user32)), true);
            }
            catch (Exception ex)
            {
                args.CancelStartup = true;
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
            if (GreenLumaSettings.CleanAppCache)
            {
                CleanAppCache();
            }
            if (GreenLumaSettings.SkipUpdateStealth && !GreenLumaSettings.EnableSteamArgs)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, $"-inhibitbootstrap -applaunch {args.Game.GameId}", Steam.SteamDirectory);
            }
            else if (GreenLumaSettings.EnableSteamArgs && !GreenLumaSettings.SkipUpdateStealth)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, $"-applaunch {args.Game.GameId} {GreenLumaSettings.SteamArgs}", Steam.SteamDirectory);
            }
            else if (GreenLumaSettings.EnableSteamArgs && GreenLumaSettings.SkipUpdateStealth)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, $"-inhibitbootstrap -applaunch {args.Game.GameId} {GreenLumaSettings.SteamArgs}", Steam.SteamDirectory);
            }
        }
        private static void CopyGreenLumaNormalMode()
        {
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "DLLInjector.ini"), Path.Combine(Steam.SteamDirectory, "DLLInjector.ini"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "DLLInjector.exe"), Path.Combine(Steam.SteamDirectory, "DLLInjector.exe"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", $"GreenLuma_{Year}_x64.dll"), Path.Combine(Steam.SteamDirectory, $"GreenLuma_{Year}_x64.dll"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", $"GreenLuma_{Year}_x86.dll"), Path.Combine(Steam.SteamDirectory, $"GreenLuma_{Year}_x86.dll"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "AchievementUnlocked.wav"), Path.Combine(Steam.SteamDirectory, $"GreenLuma{Year}_Files", "AchievementUnlocked.wav"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "x64launcher.exe"), Path.Combine(Steam.SteamDirectory, "bin", "x64launcher.exe"), true);
        }
        public static void CopyGreenLumaStealthMode(IPlayniteAPI PlayniteApi)
        {
            try
            {
                if (!FileSystem.DirectoryExists(Path.Combine(Steam.SteamDirectory, "applist")))
                {
                    FileSystem.CreateDirectory(Path.Combine(Path.Combine(Steam.SteamDirectory, "applist")));
                }
                FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, "applist", stealth), null);
                FileSystem.CopyFile(user32, Path.Combine(Steam.SteamDirectory, Path.GetFileName(user32)), true);
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
        }
        public static void RunSteamWIthGreenLumaStealthMode(IPlayniteAPI PlayniteApi)
        {
            if (Steam.IsSteamRunning)
            {
                if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_SteamIsRunning"),
                    ResourceProvider.GetString("LOCSEU_Error"), MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Token.Cancel();
                    while (Steam.IsSteamRunning)
                    {
                        _ = Steam.KillSteam;
                        Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                    }
                }
                else
                {
                    return;
                }
            }
            CopyGreenLumaStealthMode(PlayniteApi);
            if (GreenLumaSettings.CleanAppCache)
            {
                CleanAppCache();
            }
            if (GreenLumaSettings.SkipUpdateStealth && !GreenLumaSettings.EnableSteamArgs)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, $"-inhibitbootstrap", Steam.SteamDirectory);
            }
            else if (GreenLumaSettings.EnableSteamArgs && !GreenLumaSettings.SkipUpdateStealth)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, $"{GreenLumaSettings.SteamArgs}", Steam.SteamDirectory);
            }
            else if (GreenLumaSettings.EnableSteamArgs && GreenLumaSettings.SkipUpdateStealth)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, $"-inhibitbootstrap {GreenLumaSettings.SteamArgs}", Steam.SteamDirectory);
            }
            else if (!GreenLumaSettings.EnableSteamArgs && !GreenLumaSettings.SkipUpdateStealth)
            {
                ProcessUtilities.StartProcess(Steam.SteamExecutable, string.Empty, Steam.SteamDirectory);
            }
            return;
        }
        private static void BackupX64Launcher()
        {
            string x64steam = Path.Combine(Steam.SteamDirectory, "bin\\x64launcher.exe");
            string x64backup = Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe");
            string x64greenluma = Path.Combine(GreenLumaPath, "NormalMode", "x64launcher.exe");
            if (!FileSystem.FileExists(x64greenluma))
            {
                return;
            }
            if (new FileInfo(x64greenluma).Length == new FileInfo(x64steam).Length)
            {
                return;
            }
            if (!FileSystem.DirectoryExists(Path.Combine(BackupPath, "Steam\\bin")))
            {
                FileSystem.CreateDirectory(Path.Combine(BackupPath, "Steam\\bin"));
            }
            if (!FileSystem.FileExists(x64backup))
            {
                FileSystem.CopyFile(x64steam, x64backup);
                return;
            }
            if (new FileInfo(x64steam).Length != new FileInfo(x64backup).Length)
            {
                FileSystem.CopyFile(x64steam, x64backup);
            }
        }
        private static void RestoreX64Launcher()
        {
            var x64steam = new FileInfo(Path.Combine(Steam.SteamDirectory, "bin\\x64launcher.exe"));
            var x64greenluma = new FileInfo(Path.Combine(GreenLumaPath, "NormalMode", "x64launcher.exe"));
            var x64backup = new FileInfo(Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe"));
            if (!x64steam.Exists)
            {
                FileSystem.CopyFile(x64backup.FullName, x64steam.FullName);
                return;
            }
            if (x64steam.Length == x64greenluma.Length)
            {
                FileSystem.CopyFile(x64backup.FullName, x64steam.FullName);
            }
        }
        public static Task InjectAppOwnershipTickets(string path)
        {
            string destinationFile = Path.Combine(Steam.SteamDirectory, "AppOwnershipTickets", Path.GetFileName(path));
            if (!FileSystem.DirectoryExists(Path.GetDirectoryName(destinationFile)))
            {
                FileSystem.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }
            try
            {
                FileSystem.CopyFile(path, destinationFile);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task InjectEncryptedAppTickets(string path)
        {
            string destinationFile = Path.Combine(Steam.SteamDirectory, "EncryptedAppTickets", Path.GetFileName(path));
            if (!FileSystem.DirectoryExists(Path.GetDirectoryName(destinationFile)))
            {
                FileSystem.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }
            try
            {
                FileSystem.CopyFile(path, destinationFile);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static void LoadTicket(IPlayniteAPI PlayniteApi)
        {
            var files = PlayniteApi.Dialogs.SelectFiles($"{ResourceProvider.GetString("LOCSEU_TicketFiles")}|EncryptedTicket.*;Ticket.*");
            if (files == null)
            {
                return;
            }
            else if (!files.Any())
            {
                return;
            }
            List<string> AppOwnershipTickets = files.Where(x => Regex.IsMatch(Path.GetFileName(x), @"^Ticket.*", RegexOptions.IgnoreCase)).ToList();
            List<string> EncryptedAppTickets = files.Where(x => Regex.IsMatch(Path.GetFileName(x), @"^EncryptedTicket.*")).ToList();
            bool availableAppOwnership = AppOwnershipTickets.Any();
            bool availableEncryptedApp = EncryptedAppTickets.Any();
            logger.Info(availableAppOwnership.ToString());
            logger.Info(availableEncryptedApp.ToString());
            if (!FileSystem.DirectoryExists(AppOwnershipTicketsPath) && availableAppOwnership)
            {
                FileSystem.CreateDirectory(AppOwnershipTicketsPath);
            }
            if (!FileSystem.DirectoryExists(EncryptedAppTicketsPath) && availableEncryptedApp)
            {
                FileSystem.CreateDirectory(EncryptedAppTicketsPath);
            }
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility");
            PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
            {
                progress.ProgressMaxValue = AppOwnershipTickets.Count + EncryptedAppTickets.Count;
                foreach (string file in AppOwnershipTickets)
                {
                    string destinationFile = Path.Combine(AppOwnershipTicketsPath, Path.GetFileName(file));
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
                foreach (string file in EncryptedAppTickets)
                {
                    string destinationFile = Path.Combine(EncryptedAppTicketsPath, Path.GetFileName(file));
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
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_CopiedFile"), AppOwnershipTickets.Count));
            }
            else if (availableEncryptedApp && !availableAppOwnership)
            {
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_CopiedFile"), EncryptedAppTickets.Count));
            }
            else if (availableEncryptedApp && availableAppOwnership)
            {
                PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_CopiedFile"), AppOwnershipTickets.Count + EncryptedAppTickets.Count));
            }
        }
    }
}
