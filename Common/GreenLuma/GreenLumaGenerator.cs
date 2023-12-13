using System.Collections.Generic;
using System.IO;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Events;
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
            File.WriteAllLines(Path.Combine(SteamUtilities.SteamDirectory, "DLLInjector.ini"), injectorConfig);
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
            if (!Directory.Exists(Path.Combine(SteamUtilities.SteamDirectory, "applist")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(SteamUtilities.SteamDirectory, "applist")));
            }
            try
            {
                foreach (var appid in appids)
                {
                    if (count == appids.Count)
                    {
                        break;
                    }
                    File.WriteAllText(Path.Combine(SteamUtilities.SteamDirectory, "applist", $"{count}.txt"), appid);
                    count++;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static void GameAndDLCUnlocking(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            var game = args.Game;
            var appids = new List<string> { game.GameId };
            string dlcpath = Path.Combine(CommonPath, $"{game.GameId}.txt");
            if (!File.Exists(dlcpath))
            {
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                {
                    GenerateDLC(appids, progressOptions);
                }, progress);
            }
            appids.AddRange(File.ReadAllLines(dlcpath).ToList());
            WriteAppList(appids);
        }
        public static void DLCUnlocking(OnGameStartingEventArgs args, IPlayniteAPI PlayniteApi)
        {
            var game = args.Game;
            var appids = new List<string> { game.GameId };
            string dlcpath = Path.Combine(CommonPath, $"{game.GameId}.txt");
            if (!File.Exists(dlcpath))
            {
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                {
                    GenerateDLC(appids, progressOptions);
                }, progress);
            }
            appids = File.ReadAllLines(dlcpath).ToList();
            WriteAppList(appids);
        }
        public static bool GenerateDLC(List<string> appids, GlobalProgressActionArgs progressOptions)
        {
            progressOptions.CurrentProgressValue = 0;
            progressOptions.ProgressMaxValue = appids.Count;
            int count = 0;
            var steam = new SteamService();
            bool LoggedIn = false;
            foreach (var appid in appids)
            {
                if (progressOptions.CancelToken.IsCancellationRequested)
                {
                    return false;
                }
                List<int> dlcid = new List<int>();
                int app = int.Parse(appid);
                var appDetails = SteamAppDetails.GetAppDetailsStore(new List<int> { app }, progressOptions).Result;
                var appinfo = SteamCMDApi.GetAppInfo(new List<string> { appid }, progressOptions);
                progressOptions.Text = $"Getting DLC Info for {appid}";
                if (appDetails[app]?.Data?.DLC?.Count >= 1)
                {
                    dlcid.AddMissing(appDetails[app].Data.DLC);
                }
                if (!appinfo.Status)
                {
                    if (!LoggedIn && !steam.AnonymousLogin(progressOptions))
                    {
                        return false;
                    }
                    LoggedIn = true;
                    appinfo = steam.GetAppInfo(new List<uint>(app), progressOptions);
                }
                if (appinfo?.Data[appid]?.Extended?.ListOfDLC?.Count >= 1)
                {
                    dlcid.AddMissing(appinfo.Data[appid].Extended.ListOfDLC);
                }
                if (appinfo.Data[appid]?.Depots?.depots != null && appinfo.Data[appid].Depots.depots.Any(x => x.Value.dlcappid != null))
                {
                    var dlcappid = appinfo.Data[appid].Depots.depots
                        .Where(x => x.Value.dlcappid != null).Select(x => int.TryParse(x.Value.dlcappid, out int result) ? result : 0).ToList();
                    dlcid.AddMissing(dlcappid);
                }
                try
                {
                    if (!Directory.Exists(CommonPath))
                    {
                        Directory.CreateDirectory(CommonPath);
                    }
                    File.WriteAllLines(Path.Combine(CommonPath, $"{appid}.txt"), dlcid.Select(x => x.ToString()));
                }
                catch
                {
                    continue;
                };
                progressOptions.CurrentProgressValue++;
                count++;
            }
            steam.LogOff(progressOptions);
            return true;
        }
    }
}
