using Playnite.SDK;
using SteamCommon.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamCommon
{
    public class SteamUtilities
    {
        public static IEnumerable<uint> GetDLC(string appid, SteamService steam, Action<object> callback = null, string apiKey = null, string pluginPath = null)
        {
            int addedCount = 0;

            var DLCs = new List<uint>();
            string gameName = string.Empty;

            var appdetailsdlc = SteamAppDetails.GetAppDetailsStore(appid);
            if (appdetailsdlc.Success)
            {
                if (!string.IsNullOrEmpty(appdetailsdlc?.Data?.Name))
                {
                    gameName = appdetailsdlc?.Data?.Name;
                }
                if (appdetailsdlc?.Data?.DLC?.Count >= 1)
                {
                    DLCs.AddMissing(appdetailsdlc.Data.DLC.Select(x => uint.Parse(x)));
                    addedCount = DLCs.Count;
                    callback?.Invoke(string.Format(ResourceProvider.GetString("LOCSEU_Adding"), addedCount + " " + "DLC"));
                }
            }

            uint uintAppid = uint.Parse(appid);
            var PICS = steam.GetAppInfo(uintAppid);
            var app = ParsePICSProductInfo(PICS[uintAppid].KeyValues);

            if (app == null)
            {
                app = SteamCMDApi.GetAppInfo(appid);
            }

            if (app != null)
            {
                if (app?.DLC?.Count >= 1)
                {
                    DLCs.AddMissing(app.DLC);
                    addedCount = DLCs.Count;
                    callback?.Invoke(string.Format(ResourceProvider.GetString("LOCSEU_Adding"), addedCount + " " + "DLC"));
                }
                if (string.IsNullOrEmpty(gameName) && !string.IsNullOrEmpty(app.Name))
                {
                    gameName = app.Name;
                }
            }

            bool apiKeyExists = !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(pluginPath);

            if (!string.IsNullOrEmpty(gameName) && apiKeyExists)
            {
                string AlphaNumOnlyRegex = "[^0-9a-zA-Z]+";
                if (!string.IsNullOrEmpty(apiKey) && !IStoreService.CacheExists(pluginPath))
                {
                    callback?.Invoke(ResourceProvider.GetString("LOCSEU_DownloadingDLCCache"));
                    IStoreService.GenerateCache(pluginPath, apiKey);
                }
                if (!string.IsNullOrEmpty(apiKey) && IStoreService.Cache1Day(pluginPath))
                {
                    callback?.Invoke(ResourceProvider.GetString("LOCSEU_UpdatingDLCCache"));
                    IStoreService.UpdateCache(pluginPath, apiKey);
                }
                var applistDetails = IStoreService.GetApplistDetails(pluginPath);
                if (applistDetails.Applist.Apps.Any(x => x.Name.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    var filter = applistDetails.Applist.Apps.Where(x => Regex.Replace(x.Name, AlphaNumOnlyRegex, "")
                    .Contains(Regex.Replace(gameName, AlphaNumOnlyRegex, ""))).ToList();
                    DLCs.AddMissing(filter.Select(x => (uint)x.Appid));
                    addedCount = DLCs.Count;
                    callback?.Invoke(string.Format(ResourceProvider.GetString("LOCSEU_Adding"), addedCount + " " + "DLC"));
                }
            }

            return DLCs;
        }

        public static IEnumerable<App> GetApp(IEnumerable<uint> appids, SteamService steam, Action<object> eventHandler = null)
        {
            var applist = new List<App>();
            HashSet<uint> appidsSet = new HashSet<uint>(appids);
            Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PICSProductInfo = steam.GetAppInfo(appids);
            if (PICSProductInfo != null)
            {
                foreach (var kvp in PICSProductInfo)
                {
                    uint appid = kvp.Key;
                    if (appidsSet.Contains(appid))
                    {
                        var app = ParsePICSProductInfo(kvp.Value.KeyValues);
                        eventHandler?.Invoke(app);

                        applist.Add(app);
                    }
                }
            }
            if (applist.Count != appids.Count())
            {
                var existingAppIds = applist.Select(app => app.Appid).ToHashSet();
                var missingAppIds = appids.Where(x => !existingAppIds.Contains(x)).ToList();
                var missingApps = missingAppIds.Select(x =>
                {
                    var app = SteamCMDApi.GetAppInfo(x.ToString());
                    if (app != null)
                    {
                        eventHandler?.Invoke(app);
                        return app;
                    }
                    return null;
                }).ToList();

                applist.AddMissing(missingApps);
            }
            return applist;
        }

        public static App GetApp(uint appid, SteamService steam)
        {
            App app = null;
            var pics = steam.GetAppInfo(appid);

            if (pics != null)
            {
                app = ParsePICSProductInfo(pics[appid].KeyValues);
                if (app != null)
                {
                    return app;
                }
                else
                {
                    app = SteamCMDApi.GetAppInfo(appid.ToString());
                }
            }

            return app;
        }

        private static App ParsePICSProductInfo(KeyValue keyValue)
        {
            App app = new App()
            {
                Appid = uint.TryParse(keyValue["appid"].Value, out uint appid) ? appid : 0,
                Name = keyValue["common"]["name"].Value == string.Empty ? string.Empty : keyValue["common"]["name"].Value,
                DLC = new List<uint>(),
                SupportedLanguages = new List<string>(),
                Depots = new List<uint>(),
                SteamControllerConfigDetails = new Dictionary<uint, Controller> { },
                SteamControllerTouchConfigDetails = new Dictionary<uint, Controller> { },
                Launch = new List<Launch>()
            };

            string listofdlc = keyValue["extended"]["listofdlc"].Value;
            if (!string.IsNullOrEmpty(listofdlc))
            {
                app.DLC.AddMissing(listofdlc.Split(',').Select(x => uint.Parse(x)).ToList());
            }

            var languages = keyValue["common"]["supported_languages"];
            if (languages.Children.Count >= 1)
            {
                foreach (var lang in languages.Children)
                {
                    app.SupportedLanguages.AddMissing(lang.Name);
                }
            }

            var depots = keyValue["depots"];
            if (depots.Children.Count >= 1)
            {
                foreach (var depot in depots.Children)
                {
                    if (uint.TryParse(depot.Name, out uint depotid))
                    {
                        app.Depots.AddMissing(depotid);
                        if (uint.TryParse(depot["dlcappid"].Value, out uint dlcappid))
                        {
                            app.DLC.AddMissing(dlcappid);
                        }
                    }
                    if (depot.Name.Equals("branches") && uint.TryParse(depot["public"]["buildid"].Value, out uint builid))
                    {
                        app.BuildID = builid;
                    }
                }
            }

            var controllerSupport = keyValue["common"]["controller_support"];
            if (!string.IsNullOrEmpty(controllerSupport.Value))
            {
                app.ControllerSupport = true;

                var SteamControllerConfigDetails = keyValue["config"]["steamcontrollerconfigdetails"];
                if (SteamControllerConfigDetails.Children.Any())
                {
                    foreach (var controllerID in SteamControllerConfigDetails.Children)
                    {
                        if (uint.TryParse(controllerID.Name, out uint id))
                        {
                            app.SteamControllerConfigDetails.Add(id, new Controller() { ControllerType = controllerID["controller_type"].Value, EnabledBranches = controllerID["enabled_branches"].Value });
                        }
                    }
                }

                var SteamControllerTouchConfigDetails = keyValue["config"]["steamcontrollertouchconfigdetails"];
                if (SteamControllerTouchConfigDetails.Children.Any())
                {
                    foreach (var controllerID in SteamControllerTouchConfigDetails.Children)
                    {
                        if (uint.TryParse(controllerID.Name, out uint id))
                        {
                            app.SteamControllerTouchConfigDetails.Add(id, new Controller() { ControllerType = controllerID["controller_type"].Value, EnabledBranches = controllerID["enabled_branches"].Value });
                        }
                    }
                }
            }

            var launch = keyValue["config"]["launch"];
            if (launch.Children.Any())
            {
                foreach (var launchConfig in launch.Children)
                {
                    if (!string.IsNullOrEmpty(launchConfig["executable"].Value))
                    {
                        app.Launch.AddMissing(new Launch() { Executable = launchConfig["executable"].Value, OSList = launchConfig["config"]["oslist"].Value });
                    }
                }
            }

            var imageLink = keyValue["common"]["small_capsule"];
            if (imageLink.Children.Any())
            {
                if (!string.IsNullOrEmpty(imageLink["english"].Value))
                {
                    app.SmallCapsuleImage = new Language() { English = imageLink["english"].Value };
                }
            }

            var ufs = keyValue["ufs"];
            app.CloudSaveAvailable = ufs.Children.Any();
            app.CloudSaveConfigured = ufs["savefiles"].Children.Any();

            return app;
        }
    }
}
