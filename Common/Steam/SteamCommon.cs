using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using Playnite.SDK.Models;
using ProcessCommon;

namespace SteamCommon
{
    public static class Steam
    {
        private static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        private const string defaultexe = @"C:\Program Files (x86)\Steam\steam.exe";
        private const string defaultdir = @"C:\Program Files (x86)\Steam";
        private static string GetSteamDir()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                if (key?.GetValueNames().Contains("SteamPath") == true)
                {
                    return key.GetValue("SteamPath")?.ToString().Replace('/', '\\') ?? defaultdir;
                }
            }

            return defaultdir;
        }
        private static string GetSteamExe()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                if (key?.GetValueNames().Contains("SteamExe") == true)
                {
                    return key.GetValue("SteamExe")?.ToString().Replace('/', '\\') ?? defaultexe;
                }
            }

            return defaultexe;
        }
        /// <summary>
        /// Get Steam executable path.
        /// </summary>
        /// <returns> Steam executable path</returns>
        public static string SteamExecutable
        {
            get
            {
                return GetSteamExe();
            }
        }
        /// <summary>
        /// Get Steam directory path.
        /// </summary>
        /// <returns> Steam directory path</returns>
        public static string SteamDirectory
        {
            get
            {
                return GetSteamDir();
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
        public static bool IsSteamRunning
        {
            get
            {
                var steamProcesses = Process.GetProcessesByName("steam");
                return steamProcesses.Length > 0;
            }
        }
        /// <summary>
        /// Kill Steam process.
        /// </summary>
        /// <returns>
        /// <c>true</c> if Steam process successfully kill; otherwise <c>false</c>
        /// </returns>
        public static bool KillSteam
        {
            get
            {
                if (ProcessUtilities.ProcessKill("steam"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
