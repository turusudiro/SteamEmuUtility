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
                    string path = Path.Combine(SteamUtilities.SteamDirectory, file);
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
