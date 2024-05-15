using DownloaderCommon;
using Newtonsoft.Json;
using Playnite.SDK;
using SteamCommon.Models;

namespace SteamCommon
{
    public class SteamCMDApi
    {
        private const string Uri = "https://api.steamcmd.net/v1/info/";
        public static AppInfo GetAppInfo(string appid, GlobalProgressActionArgs progress, AppInfo appinfo = null)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_DownloadInfo"), appid);
            var response = HttpDownloader.DownloadString(Uri + appid, progress.CancelToken);
            if (response.Length > 0)
            {
                var appinforesult = JsonConvert.DeserializeObject<AppInfo>(response);
                if (appinfo == null)
                {
                    if (appinforesult.Status)
                    {
                        return appinforesult;
                    }
                }
                if (appinforesult.Status)
                {
                    appinfo.Data.Add(appid, appinforesult.Data[appid]);
                }
            }
            return appinfo;
        }
        public static AppIdInfo GetAppInfo(string appid, GlobalProgressActionArgs progress)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_DownloadInfo"), appid);
            var response = HttpDownloader.DownloadString(Uri + appid, progress.CancelToken);
            if (response.Length > 0)
            {
                var appinforesult = JsonConvert.DeserializeObject<AppInfo>(response);
                if (appinforesult.Status)
                {
                    return appinforesult.Data[appid];
                }
            }
            return null;
        }
    }
}
