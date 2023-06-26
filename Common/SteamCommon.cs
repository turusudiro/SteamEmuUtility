using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SteamEmuUtility.Common
{
    public class SteamCommon
    {
        private static ILogger logger = LogManager.GetLogger();
        private static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        private const string defaultexe = @"C:\Program Files (x86)\Steam\steam.exe";
        private const string defaultdir = @"C:\Program Files (x86)\Steam";
        public static string GetSteamExe()
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
        public static string SteamExe()
        {
            return Path.GetDirectoryName(GetSteamExe());
        }
        public static string GetSteamDir()
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
        public static string SteamDir()
        {
            return Path.GetDirectoryName(GetSteamDir());
        }
        public static bool IsGameSteamGame(Game game)
        {
            return game.PluginId == steamPluginId;
        }
        public static bool IsSteamRunning()
        {
            foreach (Process clsProcess in Process.GetProcesses())
            {
                if (clsProcess.ProcessName.Contains("steam"))
                {
                    return true;
                }
            }
            return false;
        }
        public static void KillSteam()
        {
            foreach (var process in Process.GetProcessesByName("steam"))
            {
                process.Kill();
            }
        }
        public static void RunSteam()
        {
            Process.Start(GetSteamExe());
        }
    }
}
