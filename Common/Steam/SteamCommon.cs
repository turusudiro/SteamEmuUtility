using Microsoft.Win32;
using Playnite.SDK.Models;
using ProcessCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SteamCommon
{
    public static class Steam
    {
        private static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        private const string defaultexe = @"C:\Program Files (x86)\Steam\steam.exe";
        private const string defaultdir = @"C:\Program Files (x86)\Steam";
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
