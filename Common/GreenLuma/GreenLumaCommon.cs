using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamEmuUtility;

namespace GreenLumaCommon
{
    public class GreenLuma
    {
        public static List<string> GreenLumaFiles = new List<string>
        {
        "bin\\x64launcher.exe",
        "GreenLuma2023_Files\\AchievementUnlocked.wav",
        "DLLInjector.exe",
        "DLLInjector.ini",
        "GreenLuma_2023_x64.dll",
        "GreenLuma_2023_x86.dll",
        "GreenLumaSettings_2023.exe",
        "GreenLuma_2023.log",
        "Applist.log",
        "User32.dll",
        "applist",
        "GreenLuma2023_Files",
        "AppOwnershipTickets",
        "EncryptedAppTickets"
        };
        private const string normalfeature = "[SEU] Normal Mode";
        private const string stealthfeature = "[SEU] Stealth Mode";
        private const string gamefeature = "[SEU] Game Unlocking";
        private const string dlcfeature = "[SEU] DLC Unlocking";
        public const string stealth = "NoQuestion.bin";
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
        public static string BackupPath { get { return Path.Combine(CommonPath, "Backup"); } }
        public static string User32 { get { return Path.Combine(GreenLumaPath, "StealthMode", "User32.dll"); } }
        public static string CommonPath { get { return Path.Combine(pluginpath, "Common", "GreenLuma"); } }
        public static string GreenLumaPath { get { return Path.Combine(pluginpath, "GreenLuma"); } }
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
        public static bool GreenLumaInjected()
        {
            if (IsDLLInjectorRunning)
            {
                return true;
            }
            foreach (var file in GreenLumaFiles)
            {
                string path = Path.Combine(Steam.SteamDirectory, file);
                if (FileSystem.FileExists(path) && !file.Contains("x64launcher"))
                {
                    try
                    {
                        using (Stream stream = new FileStream(path, FileMode.Open)) { }
                    }
                    catch
                    {
                        return true;
                    }
                }
            }
            if (GreenLumaFilesOnSteam)
            {
                return true;
            }
            return false;
        }
        public static bool ApplistConfigured(IEnumerable<string> appids)
        {
            var appidSet = new HashSet<string>(appids);
            var applist = AppList();
            if (applist != null && applist.Length > 0)
            {
                foreach (var fileInfo in applist)
                {
                    try
                    {
                        using (var reader = fileInfo.OpenText())
                        {
                            // Read each line of the file and check if any line is in appidSet
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (appidSet.Contains(line.Trim())) // Assuming trimming is appropriate
                                {
                                    return true; // At least one file contains an app ID, configured
                                }
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        // Handle or log the exception
                        Console.WriteLine($"Error reading file {fileInfo.FullName}: {ex.Message}");
                    }
                }

                // If no file contains any app ID, it is considered not configured
                return false;
            }

            // No files in the applist, considered not configured
            return false;
        }
        public static IEnumerable<string> GetDLC(string appid)
        {
            string path = Path.Combine(CommonPath, appid + ".txt");
            return FileSystem.ReadStringLinesFromFile(path);
        }
        public static bool DLCExists(string appid)
        {
            return FileSystem.FileExists(Path.Combine(Path.Combine(CommonPath, $"{appid}.txt")));
        }
        public static FileInfo[] AppList()
        {
            if (FileSystem.DirectoryExists(Path.Combine(Steam.SteamDirectory, "applist")))
            {
                string path = FileSystem.FixPathLength(Path.Combine(Steam.SteamDirectory, "applist"));
                return new DirectoryInfo(path).GetFiles("*.txt", SearchOption.TopDirectoryOnly);
            }
            else
            {
                return null;
            }
        }
        static string GetRelativePath(string basePath, string targetPath)
        {
            Uri baseUri = new Uri($"{basePath}\\");
            Uri targetUri = new Uri(targetPath);
            logger.Debug(baseUri.ToString());
            logger.Debug(targetUri.ToString());
            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace('/', '\\');
        }
        public static bool GreenLumaFilesExists(out List<string> missingFiles)
        {
            missingFiles = new List<string>();
            var GreenLumaFiles = new List<string>
            {
            $"{GreenLumaPath}\\NormalMode\\AchievementUnlocked.wav",
            $"{GreenLumaPath}\\NormalMode\\DLLInjector.exe",
            $"{GreenLumaPath}\\NormalMode\\GreenLuma_2023_x64.dll",
            $"{GreenLumaPath}\\NormalMode\\x64launcher.exe",
            $"{GreenLumaPath}\\StealthMode\\User32.dll",
            };
            foreach (string file in GreenLumaFiles)
            {
                if (!FileSystem.FileExists(file))
                {
                    // If the file doesn't exist, add it to the list of missing files
                    missingFiles.Add(GetRelativePath(GreenLumaPath, file));
                }
            }

            // Return true if there are no missing files, otherwise return false
            return missingFiles.Count == 0;
        }

        public static bool IsDLLInjectorRunning
        {
            get
            {
                var processes = Process.GetProcessesByName("dllinjector");
                return processes.Length > 0;
            }
        }
        public static bool GreenLumaFilesOnSteam
        {
            get
            {
                foreach (var file in GreenLumaFiles)
                {
                    string path = Path.Combine(Steam.SteamDirectory, file);
                    if (FileSystem.DirectoryExists(path))
                    {
                        DirectoryInfo dirinfo = new DirectoryInfo(path);
                        if (dirinfo.GetFiles().Length >= 1)
                        {
                            return true;
                        }
                    }
                    if (FileSystem.FileExists(path))
                    {
                        if (Path.GetFileName(path).Equals("x64launcher.exe"))
                        {
                            FileInfo fileinfo = new FileInfo(path);
                            FileInfo fileinfo2 = new FileInfo(Path.Combine(GreenLumaPath, "NormalMode", Path.GetFileName(file)));
                            if (fileinfo.Length == fileinfo2.Length)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }
    }
}
