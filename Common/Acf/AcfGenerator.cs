using System;
using System.Collections.Generic;
using System.Linq;
using SteamCommon;
using SteamKit2;

namespace AcfGenerator
{
    public class AcfGenerator
    {
        public static void GenerateAcf(uint appid)
        {
            var app = SteamUtilities.GetApp(appid, new SteamService());
            //var steam = new SteamService();
            //steam.AnonymousLogin();
            //uint appid = 1020790;
            //var appInfo = steam.GetAppInfo(appid);
            //if (!appInfo.ContainsKey(appid))
            //{
            //    Console.WriteLine("App info not found.");
            //    return;
            //}

            //var depots = appInfo[appid].KeyValues["depots"];
            var depots = new KeyValue();
            depots.ReadFileAsText("C:\\Users\\LAMAR\\source\\repos\\SteamTesting\\SteamTesting\\bin\\Debug\\tes.txt");
            depots = depots["depots"];
            var sharedDepots = new KeyValue("SharedDepots");
            var installedDepots = new KeyValue("InstalledDepots");

            foreach (var depot in depots.Children)
            {

                if (depot.Children.Any(x => x.Name.Contains("sharedinstall")))
                {
                    sharedDepots.Children.Add(new KeyValue(depot.Name, depot["depotfromapp"].Value));
                    Console.WriteLine($"share dari {depot.Name} dengan app {depot["depotfromapp"].Value}");
                }

                // Check if the depot contains "manifests"
                var manifests = depot.Children.FirstOrDefault(x => x.Name == "manifests");
                if (manifests != null)
                {
                    // Check if "manifests" contains the "public" key
                    var publicManifest = manifests.Children.FirstOrDefault(x => x.Name == "public");
                    if (publicManifest != null)
                    {
                        // Extract the "gid" value if it exists
                        var gid = publicManifest.Children.FirstOrDefault(x => x.Name == "gid")?.Value;
                        var size = publicManifest.Children.FirstOrDefault(x => x.Name == "size")?.Value;
                        var dlcappid = depot.Children.FirstOrDefault(x => x.Name == "dlcappid")?.Value;

                        installedDepots.Children.Add(new KeyValue(depot.Name));
                        if (!string.IsNullOrEmpty(gid))
                        {
                            installedDepots[depot.Name].Children.Add(new KeyValue("manifest", gid));
                        }
                        if (!string.IsNullOrEmpty(size))
                        {
                            installedDepots[depot.Name].Children.Add(new KeyValue("size", size));
                        }
                        if (!string.IsNullOrEmpty(dlcappid))
                        {
                            installedDepots[depot.Name].Children.Add(new KeyValue("dlcappid", dlcappid));
                        }
                    }
                }
            }

            Console.WriteLine("stop");
            var appState = new Dictionary<string, object>
            {
                { "appid", app.Appid },
                { "Universe", "1" },
                { "LauncherPath", Steam.GetSteamExecutable() },
                { "StateFlags", "4" },
                { "LastUpdated", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { "LastPlayed", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() },
                { "SizeOnDisk", "0" },
                { "StagingSize", "0" },
                { "UpdateResult", "0" },
                { "BytesToDownload", "0" },
                { "BytesDownloaded", "0" },
                { "BytesToStage", "0" },
                { "BytesStaged", "0" },
                { "AutoUpdateBehavior", "0" },
                { "AllowOtherDownloadsWhileRunning", "0" },
                { "ScheduledAutoUpdate", "0" },
                { "StagingFolder", "0" },
            };
            // Convert to Steam format (KV format)
            var kv = new KeyValue("AppState");
            foreach (var entry in appState)
            {
                var child = new KeyValue(entry.Key); // Create a new KeyValue node
                child.Value = entry.Value.ToString(); // Assign value
                kv.Children.Add(child); // Add the node to the root
            }
            if (installedDepots.Children.Any())
            {
                kv.Children.Add(installedDepots);
            }
            if (sharedDepots.Children.Any())
            {
                kv.Children.Add(sharedDepots);
            }
            kv.SaveToFile("E:\\steam_appstate.txt", false);
            Console.WriteLine("File saved successfully.");
        }
    }
}
