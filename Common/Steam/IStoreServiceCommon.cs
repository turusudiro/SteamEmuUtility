using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using GoldbergCommon;
using Playnite.SDK;
using Playnite.SDK.Data;
using PluginsCommon;
using SteamCommon.Models;

namespace SteamCommon
{
    public class IStoreService
    {
        private const string getapplistUrl = "https://api.steampowered.com/IStoreService/GetAppList/v1/?max_results=50000&include_games=0&include_dlc=1&key=";
        public static ApplistDetails GetApplistDetails(string pluginpath)
        {
            string path = Path.Combine(pluginpath, "Common", "DLCCache.json");
            return Serialization.FromJsonFile<ApplistDetails>(path);
        }
        public static bool CacheExists(string pluginpath)
        {
            string path = Path.Combine(pluginpath, "Common", "DLCCache.json");
            return FileSystem.FileExists(path);
        }
        public static bool Cache1Day(string pluginpath)
        {
            string path = Path.Combine(pluginpath, "Common", "DLCCache.json");
            if (FileSystem.FileExists(path))
            {
                FileInfo fileInfo = new FileInfo(path);
                TimeSpan difference = DateTime.Now - fileInfo.LastWriteTime;
                if (difference.TotalDays > 1)
                {
                    return true;
                }
            }
            return false;
        }
        public static void UpdateCache(string pluginpath, GlobalProgressActionArgs progressOptions, string apikey)
        {
            progressOptions.IsIndeterminate = true;
            progressOptions.Text = "Updating DLC Cache...";
            var client = new HttpClient();
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            string path = Path.Combine(pluginpath, "Common", "DLCCache.json");
            string json;
            int lastappid;
            ApplistDetails applist;
            if (FileSystem.FileExists(path)) ///Check if file exists, update it.
            {
                FileInfo fileInfo = new FileInfo(path);
                fileInfo.LastWriteTime = DateTime.Now;
                applist = Serialization.FromJsonFile<ApplistDetails>(path);
                lastappid = applist.Applist.LastAppid;
                do
                {
                    var response = lastappid > 0 ? client.GetAsync(getapplistUrl + apikey + "&last_appid=" + lastappid.ToString()).Result :
                        client.GetAsync(getapplistUrl + apikey).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStreamAsync().Result;
                        using (var decompressionStream = new GZipStream(content, CompressionMode.Decompress))
                        using (var reader = new StreamReader(decompressionStream, Encoding.UTF8))
                        {
                            json = reader.ReadToEnd();
                        }
                        var app = Serialization.FromJson<ApplistDetails>(json);
                        if (app.Applist.Apps == null)
                        {
                            return;
                        }
                        applist.Applist.HaveMoreResults = app.Applist.HaveMoreResults;
                        applist.Applist.LastAppid = app.Applist.LastAppid;
                        applist.Applist.Apps.AddRange(app.Applist.Apps);
                        applist.Applist.LastAppid = app.Applist.LastAppid;
                        applist.Applist.HaveMoreResults = app.Applist.HaveMoreResults;
                        if (!app.Applist.HaveMoreResults)
                        {
                            applist.Applist.LastAppid = app.Applist.Apps.Last().Appid;
                            break;
                        }
                    }
                } while (true);
                if (!FileSystem.DirectoryExists(Path.GetDirectoryName(path)))
                {
                    FileSystem.CreateDirectory(Path.GetDirectoryName(path));
                }
                FileSystem.WriteStringToFile(path, Serialization.ToJson(applist, true));
                return;
            }
        }
        public static void GenerateCache(string pluginpath, GlobalProgressActionArgs progressOptions, string apikey)
        {
            progressOptions.IsIndeterminate = true;
            progressOptions.Text = "Downloading DLC Cache...";
            var client = new HttpClient();
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            string path = Path.Combine(pluginpath, "Common", "DLCCache.json");
            string json;
            int lastappid = 0;
            ApplistDetails applist;
            {
                applist = new ApplistDetails();
                if (applist.Applist == null)
                {
                    applist.Applist = new ApplistDetails.ApplistData();
                }

                // Check if applist.Applist.Apps is null and initialize if necessary
                if (applist.Applist.Apps == null)
                {
                    applist.Applist.Apps = new List<ApplistDetails.ApplistData.AppsData>();
                }
                do
                {
                    var response = lastappid > 0 ? client.GetAsync(getapplistUrl + apikey + "&last_appid=" + lastappid.ToString()).Result :
                        client.GetAsync(getapplistUrl + Goldberg.SteamWebAPIKey).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var content = response.Content.ReadAsStreamAsync().Result;
                        using (var decompressionStream = new GZipStream(content, CompressionMode.Decompress))
                        using (var reader = new StreamReader(decompressionStream, Encoding.UTF8))
                        {
                            json = reader.ReadToEnd();
                        }
                        var app = Serialization.FromJson<ApplistDetails>(json);
                        applist.Applist.HaveMoreResults = app.Applist.HaveMoreResults;
                        applist.Applist.LastAppid = app.Applist.LastAppid;
                        applist.Applist.Apps.AddRange(app.Applist.Apps);
                        if (app.Applist.Apps == null)
                        {
                            return;
                        }
                        lastappid = app.Applist.LastAppid;
                        if (!app.Applist.HaveMoreResults)
                        {
                            applist.Applist.LastAppid = app.Applist.Apps.Last().Appid;
                            break;
                        }
                    }
                } while (true);
                FileSystem.WriteStringToFile(path, Serialization.ToJson(applist, true));
            }
        }
    }
}
