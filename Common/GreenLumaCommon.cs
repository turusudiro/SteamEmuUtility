using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using ProcessCommon;
using ServiceCommon;
using SteamCommon;

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
        private const string user32 = "User32.dll";
        private const string stealth = "NoQuestion.bin";
        private static readonly ILogger logger = LogManager.GetLogger();

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
        public static bool NormalMode(SteamEmuUtility.SteamEmuUtility plugin)
        {
            try
            {
                CopyDirectory(Path.Combine(plugin.GetPluginUserDataPath(), "GreenLuma\\NormalMode"), SteamUtilities.SteamDirectory);
                File.WriteAllText(Path.Combine(SteamUtilities.SteamDirectory, stealth), null);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Copy GreenLuma StealthMode files
        /// <para>Copy User32.dll file into Steam directory, also create NoQuestion.bin in applist folder.</para>
        /// </summary>
        public static Task StealthMode(string dll)
        {
            if (!Directory.Exists(Path.Combine(SteamUtilities.SteamDirectory, "applist")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(SteamUtilities.SteamDirectory, "applist")));
            }
            try
            {
                File.WriteAllText(Path.Combine(SteamUtilities.SteamDirectory, "applist", stealth), null);
                File.Copy(dll, Path.Combine(SteamUtilities.SteamDirectory, user32), true);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
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
        public static bool GenerateDLC(List<string> appids, GlobalProgressActionArgs progressOptions, string CommonPath)
        {
            int count = 0;
            var Steamservice = new SteamService();
            logger.Info("Getting DLC Info...");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            foreach (var appid in appids)
            {
                progressOptions.Text = $"Getting DLC Info for {appid}";
                var job = Steamservice.GetDLCStore(int.Parse(appid), CommonPath);
                if (job.GetAwaiter().GetResult() == true)
                {
                    progressOptions.CurrentProgressValue++;
                    count++;
                }
            }
            if (count == appids.Count)
            {
                return true;
            }
            return false;
        }
        public static bool GenerateDLCSteamKit(List<uint> appids, GlobalProgressActionArgs progressOptions, string CommonPath)
        {
            var Steamservice = new SteamService();
            logger.Info("Getting DLC Info using SteamKit...");
            Thread.Sleep(TimeSpan.FromSeconds(1));
            if (Steamservice.GetDLCSteamKit2(appids, progressOptions, CommonPath))
            {
                return true;
            }
            return false;
        }
        public static Task BackupX64Launcher(string backup)
        {
            try
            {
                if (!Directory.Exists(Path.Combine(backup, "backup\\Steam\\bin")))
                {
                    Directory.CreateDirectory(Path.Combine(backup, "backup\\Steam\\bin"));
                }
                File.Copy(Path.Combine(SteamUtilities.SteamDirectory, "bin\\x64launcher.exe"), Path.Combine(backup, "backup\\Steam\\bin\\x64launcher.exe"), true);
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
                File.Delete(Path.Combine(SteamUtilities.SteamDirectory, user32));
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

    }
}
