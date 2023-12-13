using System.Collections.Generic;
using System.Net.Http;
using DownloaderCommon;
using Newtonsoft.Json;
using Playnite.SDK;
using SteamCommon.Models;

namespace SteamCommon
{
    public class SteamCMDApi
    {
        private const string Uri = "https://api.steamcmd.net/v1/info/";
        public static AppInfo GetAppInfo(IEnumerable<string> appids, GlobalProgressActionArgs progress)
        {
            return GetAppInfo(appids, progress, null);
        }
        public static AppInfo GetAppInfo(IEnumerable<string> appids, GlobalProgressActionArgs progress, AppInfo appinfo)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            foreach (var appid in appids)
            {
                progress.Text = $"Configuring Appid {appid}";
                var response = HttpDownloader.DownloadString(Uri + appid, progress.CancelToken);
                if (response.Length > 0)
                {
                    var appinforesult = JsonConvert.DeserializeObject<AppInfo>(response);
                    if (appinfo == null)
                    {
                        if (appinforesult.Status)
                        {
                            appinfo = appinforesult;
                            continue;
                        }
                    }
                    if (appinforesult.Status)
                    {
                        appinfo.Data.Add(appid, appinforesult.Data[appid]);
                    }
                }
            }
            return appinfo;
        }
    }
}
