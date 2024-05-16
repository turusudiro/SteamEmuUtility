using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamEmuUtility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GreenLumaCommon
{
    public partial class GreenLumaVersion
    {
        public string Version { get; set; }
        public string Year { get; set; }
    }

    public class GreenLuma
    {
        public const string Url = @"https://cs.rin.ru/forum/viewtopic.php?f=29&t=103709";
        private static string version;
        public static string Version
        {
            get
            {
                if (version == null)
                {
                    try
                    {
                        version = Serialization.FromJsonFile<GreenLumaVersion>(Path
                    .Combine(GreenLumaPath, "Version.json")).Version;
                    }
                    catch { version = "0.0.0"; }
                }
                return version;
            }
            set
            {
                version = value;
            }
        }
        private static string year;
        public static string Year
        {
            get
            {
                if (year == null)
                {
                    try
                    {
                        year = Serialization.FromJsonFile<GreenLumaVersion>(Path
                    .Combine(GreenLumaPath, "Version.json")).Year;
                    }
                    catch { year = DateTime.Now.Year.ToString(); }
                }
                return year;
            }
            set
            {
                year = value;
            }
        }

        public static List<string> GreenLumaFilesNormalMode = new List<string>()
        {
        "bin\\x64launcher.exe",
        $"GreenLuma{Year}.txt",
        $"GreenLuma{Year}_Files",
        $"GreenLuma{Year}_Files\\AchievementUnlocked.wav",
        "DLLInjector.exe",
        "DLLInjector.ini",
        $"GreenLuma_{Year}_x64.dll",
        $"GreenLuma_{Year}_x86.dll",
        "AppOwnershipTickets",
        "EncryptedAppTickets",
        $"GreenLuma_{Year}.log"
        };
        public static List<string> GreenLumaFiles = new List<string>
        {
        "bin\\x64launcher.exe",
        $"GreenLuma{Year}_Files\\AchievementUnlocked.wav",
        "DLLInjector.exe",
        "DLLInjector.ini",
        $"GreenLuma_{Year}_x64.dll",
        $"GreenLuma_{Year}_x86.dll",
        $"GreenLumaSettings_{Year}.exe",
        $"GreenLuma_{Year}.log",
        "Applist.log",
        "user32.dll",
        "applist",
        $"GreenLuma{Year}_Files",
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
        public static string user32 { get { return Path.Combine(GreenLumaPath, "StealthMode", "user32.dll"); } }
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
        public static bool GreenLumaNormalInjected
        {
            get
            {
                foreach (var file in GreenLumaFilesNormalMode)
                {
                    if (file.Equals("AppOwnershipTickets") || file.Equals("EncryptedAppTickets"))
                    {
                        continue;
                    }
                    string path = Path.Combine(Steam.SteamDirectory, file);
                    if (file.Equals($"GreenLuma{Year}_Files"))
                    {
                        if (!FileSystem.DirectoryExists(path))
                        {
                            return false;
                        }
                        continue;
                    }
                    if (path.Contains("x64launcher"))
                    {
                        if (new FileInfo(path).Length != new FileInfo(Path.Combine(GreenLumaPath, "NormalMode", "x64launcher.exe")).Length)
                        {
                            return false;
                        }
                    }
                    if (!FileSystem.FileExists(path))
                    {
                        return false;
                    }
                }
                return true;
            }
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
                            bool fileContainsAppId = false;

                            // Read each line of the file and check if any line is in appidSet
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                if (appidSet.Contains(line.Trim())) // Assuming trimming is appropriate
                                {
                                    fileContainsAppId = true;
                                    break; // Exit the loop if at least one app ID is found in the file
                                }
                            }

                            if (!fileContainsAppId)
                            {
                                return false; // If no app ID is found in the file, return false
                            }
                        }
                    }
                    catch (IOException ex)
                    {
                        // Handle or log the exception
                        Console.WriteLine($"Error reading file {fileInfo.FullName}: {ex.Message}");
                    }
                }

                // If the loop completes without returning, it means all files contain at least one app ID
                return true;
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
        public static bool GreenLumaFilesExists(out List<string> missingFiles)
        {
            var GreenLumaFiles = new List<string>()
            {
                $"{GreenLumaPath}\\NormalMode\\AchievementUnlocked.wav",
                $"{GreenLumaPath}\\NormalMode\\DLLInjector.exe",
                $"{GreenLumaPath}\\NormalMode\\DLLInjector.ini",
                $"{GreenLumaPath}\\NormalMode\\GreenLuma_2024_x64.dll",
                $"{GreenLumaPath}\\NormalMode\\GreenLuma_2024_x86.dll",
                $"{GreenLumaPath}\\NormalMode\\x64launcher.exe",
                $"{GreenLumaPath}\\StealthMode\\user32.dll",
            };
            missingFiles = new List<string>();

            foreach (string file in GreenLumaFiles)
            {
                if (!FileSystem.FileExists(file))
                {
                    // If the file doesn't exist, add it to the list of missing files
                    missingFiles.Add(Path.GetFileName(file));
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
    }
}
