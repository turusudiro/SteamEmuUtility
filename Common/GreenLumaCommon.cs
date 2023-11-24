using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using ProcessCommon;
using SteamCommon;
using SteamEmuUtility;

namespace GreenLumaCommon
{
    public class GreenLuma
    {
        public static List<string> GreenLumaNormalModeFiles = new List<string>
            {
            "bin/x64launcher.exe",
            "GreenLuma2023_Files/AchievementUnlocked.wav",
            "DLLInjector.exe",
            "DLLInjector.ini",
            "GreenLuma_2023_x64.dll",
            "GreenLuma_2023_x86.dll",
            "GreenLumaSettings_2023.exe",
            };
        private const string normalfeature = "[SEU] Normal Mode";
        private const string stealthfeature = "[SEU] Stealth Mode";
        private const string gamefeature = "[SEU] Game Unlocking";
        private const string dlcfeature = "[SEU] DLC Unlocking";
        private const string stealth = "NoQuestion.bin";
        private static readonly ILogger logger = LogManager.GetLogger();
        private static SteamEmuUtilitySettings settings;
        public static SteamEmuUtilitySettings GreenLumaSettings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = value;
            }
        }
        public static string EncryptedAppTicketsPath { get { return Path.Combine(CommonPath, "EncryptedAppTickets"); } }
        public static string AppOwnershipTicketsPath { get { return Path.Combine(CommonPath, "AppOwnershipTickets"); } }
        private static string BackupPath { get { return Path.Combine(CommonPath, "Backup"); } }
        private static string User32 { get { return Path.Combine(pluginpath, "GreenLuma", "StealthMode", "User32.dll"); } }
        private static string CommonPath { get { return Path.Combine(pluginpath, "Common", "GreenLuma"); } }
        private static string pluginpath;
        public static string PluginPath
        {
            get
            {
                return pluginpath;
            }
            set
            {
                pluginpath = value;
            }
        }

        /// <summary>
        /// Clean GreenLuma Files
        /// <para>Clean all GreenLuma normal mode files on Steam directory, including restore the original files of Steam.</para>
        /// </summary>
        /// <param name="plugindir">Backup directory of Steam original files</param>
        public static void CleanGreenLumaNormalMode(string plugindir)
        {
            DeleteAppList();
            DeleteNormalModeFiles();
            RestoreX64Launcher(plugindir);
            DeleteAppOwnershipTickets();
            DeleteEncryptedAppTickets();
        }
        /// <summary>
        /// Clean GreenLuma Files
        /// <para>Clean GreenLuma StealthMode files on Steam directory, usually its just applist folder and User32.dll</para>
        /// </summary>
        /// <param name="plugindir">Backup directory of Steam original files</param>
        public static void CleanGreenLumaStealthMode()
        {
            DeleteAppList();
            DeleteStealthModeFiles();
        }
        /// <summary>
        /// Clean Steam appcache
        /// <para>Dekete Steam appcache/appinfo.vdf and appcache/packageinfo.vdf if GreenLuma not working</para>
        /// </summary>
        /// <param name="plugindir">Backup directory of Steam original files</param>
        public static void CleanAppCache()
        {
            string appinfo = Path.Combine(SteamUtilities.SteamDirectory, "appcache\\appinfo.vdf");
            string packageinfo = Path.Combine(SteamUtilities.SteamDirectory, "appcache\\packageinfo.vdf");
            string[] appcache = new string[]
            {
                appinfo,
                packageinfo
            };
            try
            {
                foreach (string file in appcache)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {

            }
        }
        static void CopyDirectory(string sourceDir, string targetDir)
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir));
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(sourceDir, targetDir), true);
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
            string FileName = Path.Combine(SteamUtilities.SteamDirectory, "DLLInjector.exe");
            logger.Info(FileName);
            string Arguments = $"-applaunch {args.Game.GameId}";
            string workingdir = SteamUtilities.SteamDirectory;
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
        public static void GreenLumaNormalMode(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            if (ProcessUtilities.IsProcessRunning("steam") && !ProcessUtilities.IsProcessRunning("dllinjector"))
            {
                if (PlayniteApi.Dialogs.ShowMessage("Steam is running! Restart steam with Injector?", "ERROR!", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                {
                    while (ProcessUtilities.IsProcessRunning("steam"))
                    {
                        ProcessUtilities.ProcessKill("steam");
                        Thread.Sleep(settings.MillisecondsToWait);
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }
            if (!File.Exists(Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe")))
            {
                GreenLuma.BackupX64Launcher();
            }
            if (File.Exists(Path.Combine(AppOwnershipTicketsPath, $"Ticket.{args.Game.GameId}")) && settings.InjectAppOwnership)
            {
                logger.Info("Found AppOwnershipTickets, copying...");
                InjectAppOwnershipTickets(Path.Combine(AppOwnershipTicketsPath, $"Ticket.{args.Game.GameId}"));
            }
            if (File.Exists(Path.Combine(EncryptedAppTicketsPath, $"EncryptedTicket.{args.Game.GameId}")) && settings.InjectEncryptedApp)
            {
                logger.Info("Found EncryptedAppTickets, copying...");
                GreenLuma.InjectEncryptedAppTickets(Path.Combine(EncryptedAppTicketsPath, $"EncryptedTicket.{args.Game.GameId}"));
            }
            if (settings.CleanAppCache)
            {
                GreenLuma.CleanAppCache();
            }
            try
            {
                CopyDirectory(Path.Combine(PluginPath, "GreenLuma\\NormalMode"), SteamUtilities.SteamDirectory);
                File.WriteAllText(Path.Combine(SteamUtilities.SteamDirectory, stealth), null);
            }
            catch (Exception ex) { PlayniteApi.Dialogs.ShowErrorMessage(ex.Message); args.CancelStartup = true; return; }
            if (!GreenLuma.StartInjector(args, settings.MaxAttemptDLLInjector, settings.MillisecondsToWait))
            {
                PlayniteApi.Dialogs.ShowMessage("An Error occured! Cannot run Steam with injector!", "ERROR!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                args.CancelStartup = true;
                return;
            }
        }
        /// <summary>
        /// Copy GreenLuma StealthMode files
        /// <para>Copy User32.dll file into Steam directory, also create NoQuestion.bin in applist folder </para>
        /// and run Steam with -inhibitbootstrap if Skip Steam Update on Stealth Mode is True
        /// </summary>
        public static void GreenLumaStealthMode(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            if (!Directory.Exists(Path.Combine(SteamUtilities.SteamDirectory, "applist")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(SteamUtilities.SteamDirectory, "applist")));
            }
            if (ProcessUtilities.IsProcessRunning("steam"))
            {
                if (PlayniteApi.Dialogs.ShowMessage("Steam is running! Restart steam with Injector?", "ERROR!", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                {
                    while (ProcessUtilities.IsProcessRunning("steam"))
                    {
                        ProcessUtilities.ProcessKill("steam");
                        Thread.Sleep(settings.MillisecondsToWait);
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }
            if (settings.CleanAppCache)
            {
                CleanAppCache();
            }
            if (settings.SkipUpdateStealth)
            {
                ProcessUtilities.TryStartProcess(SteamUtilities.SteamExecutable, $"-inhibitbootstrap -applaunch {args.Game.GameId}", SteamUtilities.SteamDirectory);
            }
            try
            {
                File.WriteAllText(Path.Combine(SteamUtilities.SteamDirectory, "applist", stealth), null);
                File.Copy(User32, Path.Combine(SteamUtilities.SteamDirectory, Path.GetFileName(User32)), true);
            }
            catch (Exception ex)
            {
                args.CancelStartup = true;
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
        }
        /// <summary>
        /// Create txt files in applist Steam directory
        /// </summary>
        /// <para>Create txt files in applist Steam directory like "0.txt", "1.txt". and more.</para>
        /// <param name="appids">List appids to write into applist</param>
        /// <returns></returns>
        public static bool WriteAppList(List<string> appids)
        {
            int count = 0;
            if (!Directory.Exists(Path.Combine(SteamUtilities.SteamDirectory, "applist")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(SteamUtilities.SteamDirectory, "applist")));
            }
            try
            {
                foreach (var appid in appids)
                {
                    if (count == appids.Count)
                    {
                        break;
                    }
                    File.WriteAllText(Path.Combine(SteamUtilities.SteamDirectory, "applist", $"{count}.txt"), appid);
                    count++;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static void GameAndDLCUnlocking(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            var game = args.Game;
            var appids = new List<string> { game.GameId };
            string dlcpath = Path.Combine(CommonPath, $"{game.GameId}.txt");
            if (!File.Exists(dlcpath))
            {
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                {
                    GenerateDLC(appids, progressOptions);
                }, progress);
            }
            appids.AddRange(File.ReadAllLines(dlcpath).ToList());
            WriteAppList(appids);
        }
        public static void DLCUnlocking(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            var game = args.Game;
            var appids = new List<string> { game.GameId };
            string dlcpath = Path.Combine(CommonPath, $"{game.GameId}.txt");
            if (!File.Exists(dlcpath))
            {
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                {
                    GenerateDLC(appids, progressOptions);
                }, progress);
            }
            appids = File.ReadAllLines(dlcpath).ToList();
            WriteAppList(appids);
        }
        public static bool GenerateDLC(List<string> appids, GlobalProgressActionArgs progressOptions)
        {
            progressOptions.CurrentProgressValue = 0;
            progressOptions.ProgressMaxValue = appids.Count;
            List<uint> FailedAppids = new List<uint>();
            int count = 0;
            var Steamservice = new SteamService();
            foreach (var appid in appids)
            {
                if (progressOptions.CancelToken.IsCancellationRequested)
                {
                    return false;
                }
                progressOptions.Text = $"Getting DLC Info for {appid}";
                var job = Steamservice.GetAppDetailsStore(int.Parse(appid));
                if (job.Result != null)
                {
                    try
                    {
                        logger.Debug(Path.Combine(CommonPath, $"{appid}.txt"));
                        File.WriteAllLines(Path.Combine(CommonPath, $"{appid}.txt"), job.Result.data.dlc.Select(x => x.ToString()).ToArray());
                    }
                    catch { continue; };
                    progressOptions.CurrentProgressValue++;
                    count++;
                }
                else
                {
                    FailedAppids.Add(uint.Parse(appid));
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            if (count != appids.Count)
            {
                count = 0;
                progressOptions.Text = $"Getting DLC Info for {FailedAppids.Count} appids using SteamKit2";
                var info = Steamservice.GetAppInfos(FailedAppids, progressOptions);
                foreach (int appid in FailedAppids)
                {
                    if (progressOptions.CancelToken.IsCancellationRequested)
                    {
                        return false;
                    }
                    try
                    {
                        File.WriteAllLines(Path.Combine(CommonPath, $"{appid}.txt"), info.AppId[appid].Extended.listofdlc.Select(x => x.ToString()).ToArray());
                    }
                    catch { continue; };
                    progressOptions.CurrentProgressValue++;
                    count++;
                }
                if (count != FailedAppids.Count)
                {
                    return false;
                }
                return true;
            }
            return true;
        }
        public static Task BackupX64Launcher()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(BackupPath, "Steam\\bin")))
                {
                    Directory.CreateDirectory(Path.Combine(BackupPath, "Steam\\bin"));
                }
                File.Copy(Path.Combine(SteamUtilities.SteamDirectory, "bin\\x64launcher.exe"), Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe"), true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        static Task RestoreX64Launcher(string backup)
        {
            try
            {
                File.Copy(Path.Combine(backup, "backup\\Steam\\bin\\x64launcher.exe"), Path.Combine(SteamUtilities.SteamDirectory, "bin\\x64launcher.exe"), true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static GameFeature GameFeature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(gamefeature);
        }
        public static GameFeature DLCFeature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(dlcfeature);
        }
        public static GameFeature NormalFeature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(normalfeature);
        }
        public static GameFeature StealthFeature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(stealthfeature);
        }
        public static Task InjectAppOwnershipTickets(string path)
        {
            string destinationFile = Path.Combine(SteamUtilities.SteamDirectory, "AppOwnershipTickets", Path.GetFileName(path));
            if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }
            try
            {
                File.Copy(path, destinationFile);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task InjectEncryptedAppTickets(string path)
        {
            string destinationFile = Path.Combine(SteamUtilities.SteamDirectory, "EncryptedAppTickets", Path.GetFileName(path));
            if (!Directory.Exists(Path.GetDirectoryName(destinationFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
            }
            try
            {
                File.Copy(path, destinationFile);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static Task DeleteAppList()
        {
            try
            {
                var dir = new DirectoryInfo(Path.Combine(SteamUtilities.SteamDirectory, "applist"));
                dir.Delete(true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        static Task DeleteNormalModeFiles()
        {
            try
            {
                foreach (string files in GreenLumaNormalModeFiles)
                {
                    File.Delete(Path.Combine(SteamUtilities.SteamDirectory, files));
                }
                var dir = new DirectoryInfo(Path.Combine(SteamUtilities.SteamDirectory, "GreenLuma2023_Files"));
                dir.Delete(true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        static Task DeleteAppOwnershipTickets()
        {
            try
            {
                var dir = new DirectoryInfo(Path.Combine(SteamUtilities.SteamDirectory, "AppOwnershipTickets"));
                dir.Delete(true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        static Task DeleteEncryptedAppTickets()
        {
            try
            {
                var dir = new DirectoryInfo(Path.Combine(SteamUtilities.SteamDirectory, "EncryptedAppTickets"));
                dir.Delete(true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        static Task DeleteStealthModeFiles()
        {
            try
            {
                File.Delete(Path.Combine(SteamUtilities.SteamDirectory, Path.GetFileName(User32)));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }
        public static void LoadTicket(IPlayniteAPI PlayniteApi)
        {
            var files = PlayniteApi.Dialogs.SelectFiles("Tickets Files|EncryptedTicket.*;Ticket.*");
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
            if (!Directory.Exists(AppOwnershipTicketsPath) && availableAppOwnership)
            {
                Directory.CreateDirectory(AppOwnershipTicketsPath);
            }
            if (!Directory.Exists(EncryptedAppTicketsPath) && availableEncryptedApp)
            {
                Directory.CreateDirectory(EncryptedAppTicketsPath);
            }
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility");
            PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
            {
                progress.ProgressMaxValue = AppOwnershipTickets.Count + EncryptedAppTickets.Count;
                foreach (string file in AppOwnershipTickets)
                {
                    string destinationFile = Path.Combine(AppOwnershipTicketsPath, Path.GetFileName(file));
                    progress.Text = "Copying " + Path.GetFileName(file) + " to " + destinationFile;
                    progress.CurrentProgressValue++;
                    if (File.Exists(destinationFile))
                    {
                        if (PlayniteApi.Dialogs.ShowMessage($"The file {Path.GetFileName(file)} already exists. Do you want to overwrite it?", "Steam Emu Utility", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                        {
                            progress.Text = "Skipping " + Path.GetFileName(file);
                            progress.CurrentProgressValue++;
                            continue;
                        }
                    }
                    File.Copy(file, destinationFile, true);
                }
                foreach (string file in EncryptedAppTickets)
                {
                    string destinationFile = Path.Combine(EncryptedAppTicketsPath, Path.GetFileName(file));
                    progress.Text = "Copying " + Path.GetFileName(file) + " to " + destinationFile;
                    progress.CurrentProgressValue++;
                    if (File.Exists(destinationFile))
                    {
                        if (PlayniteApi.Dialogs.ShowMessage($"The file {Path.GetFileName(file)} already exists. Do you want to overwrite it?", "Steam Emu Utility", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                        {
                            progress.Text = "Skipping " + Path.GetFileName(file);
                            progress.CurrentProgressValue++;
                            continue;
                        }
                    }
                    File.Copy(file, destinationFile, true);
                }
            }, progressOptions);
            if (availableAppOwnership && !availableEncryptedApp)
            {
                PlayniteApi.Dialogs.ShowMessage($"Copied {AppOwnershipTickets.Count} AppOwnershipTickets");
            }
            else if (availableEncryptedApp && !availableAppOwnership)
            {
                PlayniteApi.Dialogs.ShowMessage($"Copied {EncryptedAppTickets.Count} EncryptedAppTickets");
            }
            else if (availableEncryptedApp && availableAppOwnership)
            {
                PlayniteApi.Dialogs.ShowMessage($"Copied {AppOwnershipTickets.Count} AppOwnershipTickets and {EncryptedAppTickets.Count} EncryptedAppTickets");
            }
        }
    }
}
