using GoldbergCommon;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteCommon;
using PluginsCommon;
using System.Collections.Generic;
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
        public const string User32FamilyRegex = @"user32.\w*.dll";
        public const string NormalModeFeature = "[SEU] Normal Mode";
        public const string StealthModeFeature = "[SEU] Stealth Mode";
        public const string FamilySharingModeFeatuere = "[SEU] Family Sharing Mode";
        public const string GameUnlockingFeature = "[SEU] Game Unlocking";
        public const string DLCUnlockingFeature = "[SEU] DLC Unlocking";
        public const string AchievementRegex = @"ach[a-z]+\.wav";
        public const string GreenLumaDLL86Regex = @"GreenLuma\w+86\.dll";
        public const string GreenLumaDLL64Regex = @"GreenLuma\w+64\.dll";
        public const string InjectorRegex = @"injector*\.exe";
        public const string X64launcherRegex = @"x86\w+\.exe";
        public const string FamilyRegex = @"(fam|sf).*\.dll";
        public const string DeleteSteamAppCacheRegex = @"DeleteSteamAppCache\.exe";
        public const string GreenLumaDirectoriesRegex = @"GreenLuma.*.files|applist|AppOwnershipTickets|EncryptedAppTickets";
        public const string GreenLumaFilesRegex = @"GreenLuma.*.(dll|log|exe|txt)|applist*.\d\.txt|inject|ach[a-z]+\.wav|user32(\w*)?\.dll|fam.*\.dll|x86\w+\.exe|DeleteSteamAppCache\.exe";

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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
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
                FamilySharingFeature(playniteAPI),
                GameFeature(playniteAPI),
                DLCFeature(playniteAPI),
            };

            return PlayniteUtilities.RemoveFeatures(games, removefeature);
        }
        public static GameFeature GameFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(GameUnlockingFeature);
        }
        public static GameFeature DLCFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(DLCUnlockingFeature);
        }
        public static GameFeature NormalFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(NormalModeFeature);
        }
        public static GameFeature StealthFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(StealthModeFeature);
        }
        public static GameFeature FamilySharingFeature(IPlayniteAPI playniteApi)
        {
            return playniteApi.Database.Features.Add(FamilySharingModeFeatuere);
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
            missingFiles = new List<string>();

            if (!FileSystem.DirectoryExists(path))
            {
                missingFiles.Add("Achievement");
                missingFiles.Add("GreenLuma DLL x86");
                missingFiles.Add("GreenLuma DLL x64");
                missingFiles.Add("Injector");
                missingFiles.Add("X64launcher");
                missingFiles.Add("Family DLL");
                missingFiles.Add("Delete Steam App Cache");

                return false;
            }

            var files = Directory.GetFiles(path, "*");

            List<string> glFilesRegex = new List<string>()
            {
                AchievementRegex,
                GreenLumaDLL86Regex,
                GreenLumaDLL64Regex,
                InjectorRegex,
                X64launcherRegex,
                FamilyRegex,
                DeleteSteamAppCacheRegex
            };

            foreach (string pattern in glFilesRegex)
            {
                Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                bool isMatchFound = files.Any(file => regex.IsMatch(file));

                if (!isMatchFound)
                {
                    switch (pattern)
                    {
                        case AchievementRegex:
                            missingFiles.Add("Achievement");
                            break;
                        case GreenLumaDLL86Regex:
                            missingFiles.Add("GreenLuma DLL x86");
                            break;
                        case GreenLumaDLL64Regex:
                            missingFiles.Add("GreenLuma DLL x64");
                            break;
                        case InjectorRegex:
                            missingFiles.Add("Injector");
                            break;
                        case X64launcherRegex:
                            missingFiles.Add("X86launcher");
                            break;
                        case FamilyRegex:
                            missingFiles.Add("Family DLL");
                            break;
                        case DeleteSteamAppCacheRegex:
                            missingFiles.Add("Delete Steam App Cache");
                            break;
                    }
                }
            }

            return !missingFiles.Any();
        }
        /// <summary>
        /// Check if GreenLuma Files exists
        /// </summary>
        /// <param name="files">specified FileInfo to check</param>
        /// <param name="mode">GreenLuma Mode files to check</param>
        /// <returns>True if specified GreenLuma mode files exists</returns>
        public static bool GreenLumaFilesExists(IEnumerable<FileInfo> files, GreenLumaMode mode)
        {
            if (!files.Any())
            {
                return false;
            }

            switch (mode)
            {
                case GreenLumaMode.Normal:
                    string[] patternsNormal = { AchievementRegex, GreenLumaDLL86Regex, GreenLumaDLL64Regex, InjectorRegex, X64launcherRegex };

                    foreach (string pattern in patternsNormal)
                    {
                        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                        if (!files.Any(file => regex.IsMatch(file.Name)))
                        {
                            return false;
                        }
                    }

                    return true;

                case GreenLumaMode.Stealth:
                    string[] patternsStealth = { InjectorRegex, GreenLumaDLL86Regex };

                    foreach (string pattern in patternsStealth)
                    {
                        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                        if (!files.Any(file => regex.IsMatch(file.Name)))
                        {
                            return false;
                        }
                    }

                    return true;

                case GreenLumaMode.Family:
                    string[] patternsFamily = { InjectorRegex, FamilyRegex, DeleteSteamAppCacheRegex };

                    foreach (string pattern in patternsFamily)
                    {
                        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

                        if (!files.Any(file => regex.IsMatch(file.Name)))
                        {
                            return false;
                        }
                    }

                    return true;

                default:
                    return false;
            }
        }
    }
}
