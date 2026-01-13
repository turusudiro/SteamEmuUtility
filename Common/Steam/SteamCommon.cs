using GoldbergCommon;
using Microsoft.Win32;
using Playnite.SDK.Models;
using PlayniteCommon;
using PluginsCommon;
using ProcessCommon;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace SteamCommon
{
    public static class Steam
    {
        private static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        private static readonly Regex appIdPattern = new Regex(@"^https:\/\/(?:store\.steampowered\.com|steamcommunity\.com)\/app\/(\d+)", RegexOptions.IgnoreCase);
        private const string defaultexe = @"C:\Program Files (x86)\Steam\steam.exe";
        private const string defaultdir = @"C:\Program Files (x86)\Steam";
        /// <summary>
        /// Get all Steam library folder paths from libraryfolders.vdf using SteamKit2.
        /// </summary>
        /// <returns>List of Steam library paths.</returns>
        public static List<string> GetSteamLibraryFolders()
        {
            List<string> libraryPaths = new List<string>();

            // Get Steam directory
            string steamDir = GetSteamDirectory();
            if (string.IsNullOrEmpty(steamDir))
            {
                return libraryPaths; // Return empty if Steam directory not found
            }

            // Path to libraryfolders.vdf
            string vdfPath = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
            if (!FileSystem.FileExists(vdfPath))
            {
                return libraryPaths; // Return empty if file not found
            }

            try
            {
                // Load and parse the VDF file using SteamKit2
                KeyValue root = new KeyValue();
                root.ReadFileAsText(vdfPath);

                foreach (var child in root.Children)
                {
                    if (int.TryParse(child.Name, out _)) // Steam library keys are numerical
                    {
                        string path = child["path"].Value;
                        if (!string.IsNullOrEmpty(path))
                        {
                            libraryPaths.Add(path.Replace("\\\\", "\\")); // Normalize slashes
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing libraryfolders.vdf: {ex.Message}");
            }

            return libraryPaths;
        }
        /// <summary>
        /// Get Steam executable path.
        /// </summary>
        /// <returns> Steam executable path</returns>
        public static string GetSteamExecutable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                if (key?.GetValueNames().Contains("SteamExe") == true)
                {
                    return key.GetValue("SteamExe")?.ToString().Replace('/', '\\') ?? defaultexe;
                }
                else
                {
                    return defaultexe;
                }
            }
        }
        /// <summary>
        /// Get Steam directory path.
        /// </summary>
        /// <returns> Steam directory path</returns>
        public static string GetSteamDirectory()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                if (key?.GetValueNames().Contains("SteamPath") == true)
                {
                    return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? defaultdir;
                }
                else
                {
                    return defaultdir;
                }
            }
        }
        /// <summary>
        /// Extract app id from steam links.
        /// </summary>
        /// <param name="game">The game to extract app id for</param>
        /// <returns>
        ///  Steam's app id
        /// </returns>
        public static string GetGameSteamAppIdFromLink(Game game)
        {
            var match = game.Links.FirstOrDefault(l => appIdPattern.IsMatch(l.Url));
            var appId = string.Empty;
            if (match != null)
            {
                appId = appIdPattern.Match(match.Url).Groups[1].Value;
            }
            return appId;
        }
        /// <summary>
        /// Checks if the game has Steam link to extract app id from.
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>
        /// <c>true</c> if the game has a valid steam link; otherwise <c>false</c>
        /// </returns>
        public static bool IsGameSteamLinked(Game game)
        {
            var appId = GetGameSteamAppIdFromLink(game);
            return appId != string.Empty;
        }
        /// <summary>
        /// Checks if the game is Steam game.
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>
        /// <c>true</c> if the game is Steam game; otherwise <c>false</c>
        /// </returns>
        public static bool IsGameSteamGame(Game game)
        {
            return game.PluginId == steamPluginId;
        }
        /// <summary>
        /// Checks if the game is Steam game or has enabled goldberg feature.
        /// </summary>
        /// <param name="game">The game to check</param>
        /// <returns>
        /// <c>true</c> if the game is Steam game or has enabled goldberg feature; otherwise <c>false</c>
        /// </returns>
        public static bool IsGameSteamGameOrHasGoldbergFeature(Game game)
        {
            return game.PluginId == steamPluginId || PlayniteUtilities.HasFeature(game, Goldberg.GoldbergFeature);
        }
        /// <summary>
        /// Checks if Steam running.
        /// </summary>
        /// <returns>
        /// <c>true</c> if Steam running; otherwise <c>false</c>
        /// </returns>
        public static bool IsSteamRunning()
        {
            var steamProcesses = Process.GetProcessesByName("steam");
            return steamProcesses.Length > 0;
        }
        /// <summary>
        /// Shutdown Steam respectfully.
        /// </summary>
        public static void ShutdownSteam()
        {
            ProcessUtilities.StartProcess(GetSteamExecutable(), "-shutdown");
            bool Closed = false;
            for (int i = 0; i < 8; i++)
            {
                Thread.Sleep(2000);
                Closed = !IsSteamRunning();
                if (Closed)
                {
                    break;
                }
            }
        }
        /// <summary>
        /// Kill Steam process.
        /// </summary>
        public static void KillSteam()
        {
            ProcessUtilities.ProcessKill("steam");
        }
        /// <summary>
        /// Get Steam ProcessID in string.
        /// </summary>
        public static string GetSteamProcessId()
        {
            Process[] processes = Process.GetProcessesByName("steam");

            foreach (Process process in processes)
            {
                try
                {
                    return process.Id.ToString();
                }
                catch { return string.Empty; }
            }
            return string.Empty;
        }
        /// <summary>
        /// Get SteamID3 with SteamID64.
        /// </summary>
        /// <param name="userSteamID">The SteamID64. example: 76561197960287930</param>
        public static ulong GetUserSteamID3(string userSteamID)
        {
            if (ulong.TryParse(userSteamID, out ulong userSteamID3))
            {
                return userSteamID3 - 76561197960265728;
            }
            else
            {
                return 0;
            }
        }
    }
}
