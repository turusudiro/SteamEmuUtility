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
using PluginsCommon;
using ProcessCommon;
using SteamCommon;
using static GreenLumaCommon.GreenLuma;

namespace GreenLumaCommon
{
    public class GreenLumaTasks
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        /// <summary>
        /// Clean GreenLuma Files
        /// <para>Clean all GreenLuma Steam directory, including restore the original files of Steam.</para>
        /// </summary>
        public static async Task CleanGreenLuma()
        {
            while (Steam.IsSteamRunning)
            {
                await Task.Delay(1000);
            }
            while (IsDLLInjectorRunning)
            {
                await Task.Delay(1000);
            }
            await Task.Run(() =>
            {
                try
                {
                    foreach (var file in GreenLumaFiles)
                    {
                        string path = Path.Combine(Steam.SteamDirectory, file);
                        if (FileSystem.DirectoryExists(path))
                        {
                            DirectoryInfo dirinfo = new DirectoryInfo(path);
                            try { dirinfo.Delete(true); }
                            catch { }
                        }
                        if (Path.GetFileName(file).Equals("x64launcher.exe") && FileSystem.FileExists(path))
                        {
                            FileInfo x64steam = new FileInfo(Path.Combine(Steam.SteamDirectory, file));
                            FileInfo x64greenluma = new FileInfo(Path.Combine(GreenLumaPath, "NormalMode", Path.GetFileName(file)));
                            if (x64steam.Length == x64greenluma.Length)
                            {
                                try
                                {
                                    x64steam.Delete();
                                    FileSystem.CopyFile(Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe"), Path.Combine(Steam.SteamDirectory, "bin\\x64launcher.exe"), true);
                                }
                                catch { }
                            }
                            continue;
                        }
                        if (FileSystem.FileExists(path))
                        {
                            try { FileSystem.DeleteFile(Path.Combine(Steam.SteamDirectory, file)); }
                            catch { }
                        }
                    }
                }
                catch { }
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
            try
            {
                foreach (string file in appcache)
                {
                    if (FileSystem.FileExists(file))
                    {
                        FileSystem.DeleteFile(file);
                    }
                }
            }
            catch
            {

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
        public static void CopyGreenLumaNormalMode()
        {
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "DLLInjector.exe"), Path.Combine(Steam.SteamDirectory, "DLLInjector.exe"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "GreenLuma_2023_x64.dll"), Path.Combine(Steam.SteamDirectory, "GreenLuma_2023_x64.dll"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "GreenLuma_2023_x86.dll"), Path.Combine(Steam.SteamDirectory, "GreenLuma_2023_x86.dll"), true);
            FileSystem.CreateDirectory(Path.Combine(Steam.SteamDirectory, "GreenLuma2023_Files"));
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "AchievementUnlocked.wav"), Path.Combine(Steam.SteamDirectory, "GreenLuma2023_Files", "AchievementUnlocked.wav"), true);
            FileSystem.CopyFile(Path.Combine(GreenLumaPath, "NormalMode", "x64launcher.exe"), Path.Combine(Steam.SteamDirectory, "bin", "x64launcher.exe"), true);
        }
        /// <summary>
        /// Copy GreenLuma NormalMode files
        /// <para>Copy GreenLuma NormalMode files into Steam directory, also create NoQuestion.bin</para>
        /// </summary>
        public static void GreenLumaNormalMode(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            if (ProcessUtilities.IsProcessRunning("dllinjector"))
            {
                while (ProcessUtilities.IsProcessRunning("dllinjector"))
                {
                    ProcessUtilities.ProcessKill("dllinjector");
                    Thread.Sleep(GreenLumaSettings.MillisecondsToWait);
                }
            }
            if (!FileSystem.FileExists(Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe")))
            {
                BackupX64Launcher().Wait();
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
            if (GreenLumaSettings.CleanAppCache)
            {
                CleanAppCache();
            }
            try
            {
                CopyGreenLumaNormalMode();
                GreenLumaGenerator.CreateDLLInjectorIni();
                FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, stealth), null);
            }
            catch (Exception ex) { PlayniteApi.Dialogs.ShowErrorMessage(ex.Message); args.CancelStartup = true; return; }
            if (!StartInjector(args, GreenLumaSettings.MaxAttemptDLLInjector, GreenLumaSettings.MillisecondsToWait))
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
            try
            {
                if (!FileSystem.DirectoryExists(Path.Combine(Steam.SteamDirectory, "applist")))
                {
                    FileSystem.CreateDirectory(Path.Combine(Path.Combine(Steam.SteamDirectory, "applist")));
                }
                FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, "applist", stealth), null);
                FileSystem.CopyFile(User32, Path.Combine(Steam.SteamDirectory, Path.GetFileName(User32)), true);
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
        public static void CopyGreenLumaStealthMode(IPlayniteAPI PlayniteApi)
        {
            try
            {
                if (!FileSystem.DirectoryExists(Path.Combine(Steam.SteamDirectory, "applist")))
                {
                    FileSystem.CreateDirectory(Path.Combine(Path.Combine(Steam.SteamDirectory, "applist")));
                }
                FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, "applist", stealth), null);
                FileSystem.CopyFile(User32, Path.Combine(Steam.SteamDirectory, Path.GetFileName(User32)), true);
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
        }
        public static void RunSteamWIthGreenLumaStealthMode(Game Game, IPlayniteAPI PlayniteApi)
        {
            if (Steam.IsSteamRunning)
            {
                if (PlayniteApi.Dialogs.ShowMessage("Steam is running! Restart steam with Injector?", "ERROR!", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                {
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
        public static Task BackupX64Launcher()
        {
            try
            {
                string x64steam = Path.Combine(Steam.SteamDirectory, "bin\\x64launcher.exe");
                string x64backup = Path.Combine(BackupPath, "Steam\\bin\\x64launcher.exe");
                string x64greenluma = Path.Combine(GreenLumaPath, "NormalMode", "x64launcher.exe");
                if (!FileSystem.FileExists(x64greenluma))
                {
                    return Task.CompletedTask;
                }
                if (new FileInfo(x64greenluma).Length == new FileInfo(x64steam).Length)
                {
                    return Task.CompletedTask;
                }
                if (!FileSystem.DirectoryExists(Path.Combine(BackupPath, "Steam\\bin")))
                {
                    FileSystem.CreateDirectory(Path.Combine(BackupPath, "Steam\\bin"));
                }
                if (!FileSystem.FileExists(x64backup))
                {
                    FileSystem.CopyFile(x64steam, x64backup, true);
                    return Task.CompletedTask;
                }
                if (new FileInfo(x64steam).Length != new FileInfo(x64backup).Length)
                {
                    FileSystem.CopyFile(x64steam, x64backup, true);
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
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
                    progress.Text = "Copying " + Path.GetFileName(file) + " to " + destinationFile;
                    progress.CurrentProgressValue++;
                    if (FileSystem.FileExists(destinationFile))
                    {
                        if (PlayniteApi.Dialogs.ShowMessage($"The file {Path.GetFileName(file)} already exists. Do you want to overwrite it?", "Steam Emu Utility", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                        {
                            progress.Text = "Skipping " + Path.GetFileName(file);
                            progress.CurrentProgressValue++;
                            continue;
                        }
                    }
                    FileSystem.CopyFile(file, destinationFile, true);
                }
                foreach (string file in EncryptedAppTickets)
                {
                    string destinationFile = Path.Combine(EncryptedAppTicketsPath, Path.GetFileName(file));
                    progress.Text = "Copying " + Path.GetFileName(file) + " to " + destinationFile;
                    progress.CurrentProgressValue++;
                    if (FileSystem.FileExists(destinationFile))
                    {
                        if (PlayniteApi.Dialogs.ShowMessage($"The file {Path.GetFileName(file)} already exists. Do you want to overwrite it?", "Steam Emu Utility", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                        {
                            progress.Text = "Skipping " + Path.GetFileName(file);
                            progress.CurrentProgressValue++;
                            continue;
                        }
                    }
                    FileSystem.CopyFile(file, destinationFile, true);
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
