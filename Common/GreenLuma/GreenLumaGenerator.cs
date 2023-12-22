﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using static GreenLumaCommon.GreenLuma;

namespace GreenLumaCommon
{
    public class GreenLumaGenerator
    {
        public static void CreateDLLInjectorIni()
        {
            List<string> injectorConfig = new List<string>
            {
                "[DllInjector]",
                "AllowMultipleInstancesOfDLLInjector = 0",
                "UseFullPathsFromIni = 0",
                "Exe = Steam.exe",
                "CommandLine = -inhibitbootstrap",
                "Dll = GreenLuma_2023_x86.dll",
                "WaitForProcessTermination = 1",
                "EnableFakeParentProcess = 0",
                "FakeParentProcess = explorer.exe",
                "EnableMitigationsOnChildProcess = 0",
                "DEP = 1",
                "SEHOP = 1",
                "HeapTerminate = 1",
                "ForceRelocateImages = 1",
                "BottomUpASLR = 1",
                "HighEntropyASLR = 1",
                "RelocationsRequired = 1",
                "StrictHandleChecks = 0",
                "Win32kSystemCallDisable = 0",
                "ExtensionPointDisable = 1",
                "CFG = 1",
                "CFGExportSuppression = 1",
                "StrictCFG = 1",
                "DynamicCodeDisable = 0",
                "DynamicCodeAllowOptOut = 0",
                "BlockNonMicrosoftBinaries = 0",
                "FontDisable = 1",
                "NoRemoteImages = 1",
                "NoLowLabelImages = 1",
                "PreferSystem32 = 0",
                "RestrictIndirectBranchPrediction = 1",
                "SpeculativeStoreBypassDisable = 0",
                "ShadowStack = 0",
                "ContextIPValidation = 0",
                "BlockNonCETEHCONT = 0",
                "CreateFiles = 0",
                "FileToCreate_1 =",
                "FileToCreate_2 =",
                "Use4GBPatch = 0",
                "FileToPatch_1 ="
            };
            if (GreenLumaSettings.EnableSteamArgs)
            {
                int index = injectorConfig.FindIndex(line => line.StartsWith("CommandLine"));
                if (index != -1)
                {
                    // Modify the line with the new value
                    injectorConfig[index] = $"CommandLine = -inhibitbootstrap {GreenLumaSettings.SteamArgs}";
                }
            }
            FileSystem.WriteStringLinesToFile(Path.Combine(Steam.SteamDirectory, "DLLInjector.ini"), injectorConfig);
        }
        /// <summary>
        /// Create txt files in applist Steam directory
        /// </summary>
        /// <para>Create txt files in applist Steam directory like "0.txt", "1.txt". and more.</para>
        /// <param name="appids">List appids to write into applist</param>
        /// <returns></returns>
        public static bool WriteAppList(List<string> appids)
        {
            int count = 0;
            var applist = AppList();
            if (applist != null && applist.Count() > 0)
            {
                foreach (var file in applist)
                {
                    string content = file.OpenText().ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        appids.RemoveAll(x => x.Contains(content));
                    }
                }
                count = applist.Count();
            }
            if (!FileSystem.DirectoryExists(Path.Combine(Steam.SteamDirectory, "applist")))
            {
                FileSystem.CreateDirectory(Path.Combine(Path.Combine(Steam.SteamDirectory, "applist")));
            }
            try
            {
                foreach (var appid in appids)
                {
                    FileSystem.WriteStringToFile(Path.Combine(Steam.SteamDirectory, "applist", $"{count}.txt"), appid);
                    count++;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void GenerateDLC(Game game, SteamService steam, GlobalProgressActionArgs progress, string apikey)
        {
            string AlphaNumOnlyRegex = "[^0-9a-zA-Z]+";
            string appid = game.GameId;
            string gameName = game.Name;
            List<string> dlcid = new List<string>();
            var appdetailsdlc = SteamAppDetails.GetAppDetailsStore(new List<string> { game.GameId }, progress).Result;
            var appinfo = SteamUtilities.GetAppIdInfo(appid, steam, progress);
            if (!IStoreService.CacheExists(PluginPath))
            {
                IStoreService.GenerateCache(PluginPath, progress, apikey);
            }
            if (IStoreService.Cache1Day(PluginPath))
            {
                IStoreService.UpdateCache(PluginPath, progress, apikey);
            }
            var applistdetails = IStoreService.GetApplistDetails(PluginPath);
            if (appinfo.Depots?.depots != null && appinfo.Depots.depots.Any(x => x.Value.dlcappid != null))
            {
                var dlcappid = appinfo.Depots.depots
                    .Where(x => x.Value.dlcappid != null).Select(x => string.IsNullOrEmpty(x.Value?.dlcappid) ? string.Empty : x.Value?.dlcappid).ToList();

                dlcid.AddMissing(dlcappid.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            if (appinfo.Extended?.ListOfDLC != null)
            {
                dlcid.AddMissing(appinfo.Extended.ListOfDLC);
            }
            if (appdetailsdlc[appid]?.Data?.DLC?.Count >= 1)
            {
                dlcid.AddMissing(appdetailsdlc[appid].Data.DLC);
            }
            if (applistdetails.Applist.Apps.Any(x => x.Name.IndexOf(appinfo.Common.Name, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var filter = applistdetails.Applist.Apps.Where(x => Regex.Replace(x.Name, AlphaNumOnlyRegex, "")
                .Contains(Regex.Replace(gameName, AlphaNumOnlyRegex, ""))).ToList();
                dlcid.AddMissing(filter.Select(x => x.Appid.ToString()));
            }
            try
            {
                if (!FileSystem.DirectoryExists(CommonPath))
                {
                    FileSystem.CreateDirectory(CommonPath);
                }
                FileSystem.WriteStringLinesToFile(Path.Combine(CommonPath, $"{appid}.txt"), dlcid.Select(x => x.ToString()));
            }
            catch { }
        }
    }
}
