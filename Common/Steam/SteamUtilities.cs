using Playnite.SDK;
using SteamCommon.Models;
using SteamKit2;
using SteamStoreQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SteamCommon
{
    public class SteamUtilities
    {
        public static IEnumerable<Listing> GetAppStoreInfoByName(string name)
        {
            string gameName = name;
            List<Listing> results;
            while (!string.IsNullOrEmpty(gameName))
            {
                results = Query.Search(gameName);
                if (results.Any())
                {
                    return results;
                }
                int lastSpaceIndex = gameName.LastIndexOf(' ');
                if (lastSpaceIndex == -1) break;

                gameName = gameName.Substring(0, lastSpaceIndex);
            }
            return null;
        }
        public static IEnumerable<uint> GetDLC(string appid, SteamService steam, string apiKey = null, string pluginPath = null)
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
                    steam.Callbacks.InvokeProgressUpdate(string.Format(ResourceProvider.GetString("LOCSEU_Adding"), addedCount + " " + "DLC"));
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
                    steam.Callbacks.InvokeProgressUpdate(string.Format(ResourceProvider.GetString("LOCSEU_Adding"), addedCount + " " + "DLC"));
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
                    steam.Callbacks.InvokeProgressUpdate(ResourceProvider.GetString("LOCSEU_DownloadingDLCCache"));
                    IStoreService.GenerateCache(pluginPath, apiKey);
                }
                if (!string.IsNullOrEmpty(apiKey) && IStoreService.Cache1Day(pluginPath))
                {
                    steam.Callbacks.InvokeProgressUpdate(ResourceProvider.GetString("LOCSEU_UpdatingDLCCache"));
                    IStoreService.UpdateCache(pluginPath, apiKey);
                }
                var applistDetails = IStoreService.GetApplistDetails(pluginPath);
                if (applistDetails.Applist.Apps.Any(x => x.Name.IndexOf(gameName, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    var filter = applistDetails.Applist.Apps.Where(x => Regex.Replace(x.Name, AlphaNumOnlyRegex, "")
                    .Contains(Regex.Replace(gameName, AlphaNumOnlyRegex, ""))).ToList();
                    DLCs.AddMissing(filter.Select(x => (uint)x.Appid));
                    addedCount = DLCs.Count;
                    steam.Callbacks.InvokeProgressUpdate(string.Format(ResourceProvider.GetString("LOCSEU_Adding"), addedCount + " " + "DLC"));
                }
            }

            return DLCs;
        }

        public static IEnumerable<App> GetApp(IEnumerable<uint> appids, SteamService steam)
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

                        steam.Callbacks.InvokeAppCallback(app);

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
                        steam.Callbacks.InvokeAppCallback(app);
                        return app;
                    }
                    return null;
                }).Where(app => app != null).ToList();

                applist.AddRange(missingApps);
            }

            return applist;
        }


        public static App GetApp(uint appId)
        {
            return SteamCMDApi.GetAppInfo(appId.ToString());
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
                Branches = Enumerable.Empty<Branches>(),
                BuildID = 0,
                Name = keyValue["common"]["name"].Value == string.Empty ? string.Empty : keyValue["common"]["name"].Value,
                DLC = new List<uint>(),
                SupportedLanguages = new List<string>(),
                Depots = new Dictionary<uint, Depots>(),
                SteamControllerConfigDetails = new Dictionary<uint, Controller> { },
                SteamControllerTouchConfigDetails = new Dictionary<uint, Controller> { },
                Launch = new List<Launch>(),
                InstallSize = "0"
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

            if (keyValue.Children.Any(x => x.Name.Contains("depots")))
            {
                long sizeTotal = 0;
                foreach (var dep in keyValue["depots"].Children)
                {
                    if (uint.TryParse(dep.Name, out uint id))
                    {
                        app.Depots.Add(id, new Depots());
                        if (uint.TryParse(dep["dlcappid"].Value, out uint dlcappid))
                        {
                            app.DLC.AddMissing(dlcappid);
                        }
                        if (dep["manifests"] != null)
                        {
                            if (dep["manifests"]["public"]["gid"].Value != null)
                            {
                                app.Depots[id].Manifest = dep["manifests"]["public"]["gid"].Value;
                            }
                            if (dep["manifests"]["public"]["size"].Value != null)
                            {
                                app.Depots[id].Size = dep["manifests"]["public"]["size"].Value;
                                if (long.TryParse(app.Depots[id].Size, out long value))
                                {
                                    sizeTotal += value;
                                }
                            }
                        }
                        if (dep.Children.Any(x => x.Name.Contains("sharedinstall")))
                        {
                            app.Depots[id].SharedInstall = true;
                        }
                        if (dep.Children.Any(x => x.Name.Contains("depotfromapp")) && int.TryParse(dep["depotfromapp"].Value, out int fromAppID))
                        {
                            app.Depots[id].DepotFromApp = fromAppID;
                        }
                    }

                    if (dep.Name.Equals("branches"))
                    {
                        var branches = new List<Branches>();
                        foreach (var branch in keyValue["depots"]["branches"].Children)
                        {
                            var branchinfo = new Branches()
                            {
                                Name = branch.Name,
                                Description = string.Empty,
                                Protected = false,
                                BuildID = 0,
                                TimeUpdated = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                            };

                            if (branch.Children.Any(x => x.Name.Equals("description")))
                            {
                                branchinfo.Description = branch["description"].Value;
                            }

                            if (branch.Children.Any(x => x.Name.Equals("pwdrequired")))
                            {
                                branchinfo.Protected = int.TryParse(branch["pwdrequired"].Value, out int value) && (value == 1 ? true : false);
                            }

                            if (branch.Children.Any(x => x.Name.Equals("buildid")))
                            {
                                branchinfo.BuildID = uint.Parse(branch["buildid"].Value);
                            }

                            if (branch.Children.Any(x => x.Name.Equals("timeupdated")))
                            {
                                branchinfo.TimeUpdated = int.Parse(branch["timeupdated"].Value);
                            }

                            branches.Add(branchinfo);

                            app.Branches = branches;
                        }

                        if (app.Branches.Any(x => x.Name.Equals("public")))
                        {
                            app.BuildID = app.Branches.FirstOrDefault(x => x.Name.Equals("public")).BuildID;
                        }
                    }


                    if (dep.Name.Equals("branches") && uint.TryParse(dep["public"]["buildid"].Value, out uint builid))
                    {
                        app.BuildID = builid;
                    }
                }
                app.InstallSize = sizeTotal.ToString();
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

            var installDir = keyValue["config"]["installdir"]?.Value;
            app.Installdir = string.IsNullOrEmpty(installDir) ? string.Empty : installDir;

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
