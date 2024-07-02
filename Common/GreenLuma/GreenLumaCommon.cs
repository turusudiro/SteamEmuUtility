using GoldbergCommon;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteCommon;
using PluginsCommon;
using SteamCommon;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GreenLumaCommon
{
    public partial class GreenLumaLastRun
    {
        public IEnumerable<string> Appids { get; set; }
        public bool FamilyMode { get; set; }
        public string ProcessID { get; set; }
        public bool StealthMode { get; set; }
    }

    public partial class GreenLumaVersion
    {
        public string Version { get; set; }
        public string Year { get; set; }
    }

    public class GreenLuma
    {
        public enum GreenLumaMode
        {
            Normal,
            Stealth,
            Family
        }
        public const string Url = @"https://cs.rin.ru/forum/viewtopic.php?f=29&t=103709";
        private const string normalfeature = "[SEU] Normal Mode";
        private const string stealthfeature = "[SEU] Stealth Mode";
        private const string familybetafeature = "[SEU] Family Beta Mode";
        private const string gamefeature = "[SEU] Game Unlocking";
        private const string dlcfeature = "[SEU] DLC Unlocking";
        private static readonly ILogger logger = LogManager.GetLogger();
        public static int AddNormalGameOnlyFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                NormalFeature(playniteAPI),
                GameFeature(playniteAPI),
            };

            var removefeature = new List<GameFeature>()
            {
                Goldberg.Feature(playniteAPI),
                StealthFeature(playniteAPI),
                FamilyBetaFeature(playniteAPI),
                DLCFeature(playniteAPI)
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddNormalDLCOnlyFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                NormalFeature(playniteAPI),
                DLCFeature(playniteAPI),
            };

            var removefeature = new List<GameFeature>()
            {
                Goldberg.Feature(playniteAPI),
                StealthFeature(playniteAPI),
                FamilyBetaFeature(playniteAPI),
                GameFeature(playniteAPI)
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddNormalGameAndDLCFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                NormalFeature(playniteAPI),
                DLCFeature(playniteAPI),
                GameFeature(playniteAPI),
            };

            var removefeature = new List<GameFeature>()
            {
                Goldberg.Feature(playniteAPI),
                StealthFeature(playniteAPI),
                FamilyBetaFeature(playniteAPI),
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddStealthGameOnlyFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                StealthFeature(playniteAPI),
                GameFeature(playniteAPI),
            };

            var removefeature = new List<GameFeature>()
            {
                FamilyBetaFeature(playniteAPI),
                Goldberg.Feature(playniteAPI),
                NormalFeature(playniteAPI),
                DLCFeature(playniteAPI)
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddStealthDLCOnlyFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                StealthFeature(playniteAPI),
                DLCFeature(playniteAPI),
            };

            var removefeature = new List<GameFeature>()
            {
                FamilyBetaFeature(playniteAPI),
                Goldberg.Feature(playniteAPI),
                NormalFeature(playniteAPI),
                GameFeature(playniteAPI)
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddStealthGameAndDLCFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                StealthFeature(playniteAPI),
                DLCFeature(playniteAPI),
                GameFeature(playniteAPI),
            };

            var removefeature = new List<GameFeature>()
            {
                FamilyBetaFeature(playniteAPI),
                Goldberg.Feature(playniteAPI),
                NormalFeature(playniteAPI),
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddFamilyGameOnlyFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                FamilyBetaFeature(playniteAPI),
                GameFeature(playniteAPI)
            };

            var removefeature = new List<GameFeature>()
            {
                StealthFeature(playniteAPI),
                Goldberg.Feature(playniteAPI),
                NormalFeature(playniteAPI),
                DLCFeature(playniteAPI)
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddFamilyDLCOnlyFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                FamilyBetaFeature(playniteAPI),
                DLCFeature(playniteAPI)
            };

            var removefeature = new List<GameFeature>()
            {
                StealthFeature(playniteAPI),
                Goldberg.Feature(playniteAPI),
                NormalFeature(playniteAPI),
                GameFeature(playniteAPI)
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int AddFamilyGameAndDLCFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var features = new List<GameFeature>()
            {
                FamilyBetaFeature(playniteAPI),
                DLCFeature(playniteAPI),
                GameFeature(playniteAPI)
            };

            var removefeature = new List<GameFeature>()
            {
                StealthFeature(playniteAPI),
                Goldberg.Feature(playniteAPI),
                NormalFeature(playniteAPI),
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            games.ForEach(x => x.IncludeLibraryPluginAction = true);

            return PlayniteUtilities.AddFeatures(games, features);
        }
        public static int DisableGreenLuma(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var removefeature = new List<GameFeature>()
            {
                NormalFeature(playniteAPI),
                StealthFeature(playniteAPI),
                FamilyBetaFeature(playniteAPI),
                GameFeature(playniteAPI),
                DLCFeature(playniteAPI),
            };

            return PlayniteUtilities.RemoveFeatures(games, removefeature);
        }
        public static GameFeature GameFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(gamefeature);
        }
        public static GameFeature DLCFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(dlcfeature);
        }
        public static GameFeature NormalFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(normalfeature);
        }
        public static GameFeature StealthFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(stealthfeature);
        }
        public static GameFeature FamilyBetaFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(familybetafeature);
        }
        public static bool ApplistConfigured(IEnumerable<string> appids, IEnumerable<string> applist)
        {
            var appidSet = new HashSet<string>(appids);

            var applistSet = applist.ToHashSet();
            if (applistSet != null && applistSet.SetEquals(appidSet))
            {
                return true;
            }
            return false;
        }
        public static bool GreenLumaFilesExists(string path, out List<string> missingFiles)
        {
            var GreenLumaFiles = new List<string>()
            {
                $"{path}\\AchievementUnlocked.wav",
                $"{path}\\DLLInjector.exe",
                $"{path}\\DLLInjector.ini",
                $"{path}\\GreenLuma_2024_x64.dll",
                $"{path}\\GreenLuma_2024_x86.dll",
                $"{path}\\x64launcher.exe",
                $"{path}\\user32.dll",
                $"{path}\\user32FamilySharing.dll",
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
        public static string GetGreenLumaYear(string greenlumaPath)
        {
            string year = string.Empty;

            if (FileSystem.DirectoryExists(greenlumaPath))
            {
                string filePattern = @"\d{4}[^ \- \d A-Z]";

                Regex fileRegex = new Regex(filePattern, RegexOptions.IgnoreCase);

                var files = new DirectoryInfo(greenlumaPath).GetFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(x => fileRegex.IsMatch(x.Name));

                if (files.Any())
                {
                    string rawString = files.FirstOrDefault().Name;
                    year = Regex.Replace(rawString, @"\D+", "");
                }
            }
            return year;
        }
        public static IEnumerable<DirectoryInfo> GetGreenLumaDirectories(string path)
        {
            if (FileSystem.DirectoryExists(path))
            {
                string dirPattern = @"GreenLuma.*.files$|applist$|AppOwnershipTickets$|EncryptedAppTickets$";

                Regex dirRegex = new Regex(dirPattern, RegexOptions.IgnoreCase);

                return new DirectoryInfo(path).GetDirectories("*", SearchOption.AllDirectories)
                    .Where(x => dirRegex.IsMatch(x.Name));
            }
            return Enumerable.Empty<DirectoryInfo>();
        }
        public static IEnumerable<FileInfo> GetGreenLumaFiles(string path)
        {
            if (FileSystem.DirectoryExists(path))
            {
                string filePattern = @"GreenLuma.*.(wav|dll|log|exe)$|applist..*|dllinjector.*|.*x64launcher.exe$|.*AchievementUnlocked\.wav$";

                Regex fileRegex = new Regex(filePattern, RegexOptions.IgnoreCase);

                return new DirectoryInfo(path).GetFiles("*", SearchOption.AllDirectories)
                    .Where(x => fileRegex.IsMatch(x.Name));
            }
            return Enumerable.Empty<FileInfo>();
        }
        public static bool IsGreenLumaPresentOnSteamDir()
        {
            string steamDir = Steam.GetSteamDirectory();

            if (FileSystem.DirectoryExists(steamDir))
            {
                var dirGreenLumaOnSteam = GetGreenLumaDirectories(steamDir);

                var fileGreenLumaOnSteam = GetGreenLumaFiles(steamDir);

                if (dirGreenLumaOnSteam.Any())
                {
                    return true;
                }
                else if (fileGreenLumaOnSteam.Any())
                {
                    var x64steam = fileGreenLumaOnSteam.FirstOrDefault(x => x.Name.Contains("x64launcher"));
                    if (x64steam != null)
                    {
                        var fileinfo = FileVersionInfo.GetVersionInfo(x64steam.FullName);
                        bool fromValve = fileinfo.ProductName == "Steam" ? true : false;
                        if (!fromValve)
                        {
                            return true;
                        }
                    }
                    else { return false; }
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        public static bool IsDLLInjectorRunning()
        {
            var processes = Process.GetProcessesByName("dllinjector");
            return processes.Length > 0;
        }
    }
}
