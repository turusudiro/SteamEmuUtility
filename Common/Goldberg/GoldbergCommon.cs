using GreenLumaCommon;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteCommon;
using PluginsCommon;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoldbergCommon
{
    public class Goldberg
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        public const string GoldbergFeature = "[SEU] Goldberg";
        public static string GetAchievementWatcherAppData()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Achievement Watcher");
        }
        public static string GetGoldbergAppData()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GSE Saves");
        }
        public static bool ColdClientExists(string path, out List<string> missingFiles)
        {
            var ColdClientFiles = new List<string>
            {
            $"{path}\\steamclient.dll",
            $"{path}\\extra_dlls\\steamclient_extra_x32.dll",
            $"{path}\\extra_dlls\\steamclient_extra_x64.dll",
            $"{path}\\steamclient_loader_x32.exe",
            $"{path}\\steamclient_loader_x64.exe",
            $"{path}\\steamclient64.dll",
            };
            missingFiles = new List<string>();

            foreach (string file in ColdClientFiles)
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
        public static int AddGoldbergFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            var removefeature = new List<GameFeature>()
            {
                GreenLuma.NormalFeature(playniteAPI),
                GreenLuma.StealthFeature(playniteAPI),
                GreenLuma.FamilySharingFeature(playniteAPI),
                GreenLuma.GameFeature(playniteAPI),
                GreenLuma.DLCFeature(playniteAPI),
            };
            PlayniteUtilities.RemoveFeatures(games, removefeature);

            return PlayniteUtilities.AddFeatures(games, Feature(playniteAPI));
        }
        public static int RemoveGoldbergFeature(IEnumerable<Game> games, IPlayniteAPI playniteAPI)
        {
            return PlayniteUtilities.RemoveFeatures(games, Feature(playniteAPI));
        }
        public static GameFeature Feature(IPlayniteAPI PlayniteApi)
        {
            return PlayniteApi.Database.Features.Add(GoldbergFeature);
        }
    }
}
