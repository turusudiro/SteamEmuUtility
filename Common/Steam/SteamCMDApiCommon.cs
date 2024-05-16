using DownloaderCommon;
using Newtonsoft.Json;
using Playnite.SDK;
using SteamCommon.Models;

namespace SteamCommon
{
    public class SteamCMDApi
    {
        private const string Uri = "https://api.steamcmd.net/v1/info/";
        public static bool GetAppInfo(string appid, GlobalProgressActionArgs progress, out AppIdInfo info)
        {
            progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_DownloadInfo"), appid);
            var response = HttpDownloader.DownloadString(Uri + appid, progress.CancelToken);
            if (response.Length > 0)
            {
                var appinforesult = JsonConvert.DeserializeObject<AppInfo>(response);
                if (appinforesult.Status)
                {
                    info = appinforesult.Data[appid];
                    return true;
                }
            }
            info = null;
            return false;
        }
    }
}
