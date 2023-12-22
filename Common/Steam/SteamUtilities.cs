using Playnite.SDK;
using SteamCommon.Models;

namespace SteamCommon
{
    public static class SteamUtilities
    {
        public static AppIdInfo GetAppIdInfo(string appid, SteamService steam, GlobalProgressActionArgs progress)
        {
            var appIdInfo = SteamCMDApi.GetAppInfo(appid, progress);
            if (appIdInfo == null)
            {
                appIdInfo = steam.GetAppInfo(appid, progress);
            }
            return appIdInfo;
        }
        public static AppInfo GetAppInfo(string appid, SteamService steam, GlobalProgressActionArgs progress, AppInfo appinfo = null)
        {
            if (appinfo == null)
            {
                appinfo = SteamCMDApi.GetAppInfo(appid, progress, appinfo);
                if (appinfo == null || !appinfo.Data.ContainsKey(appid))
                {
                    appinfo = new AppInfo();
                    appinfo.Data.Add(appid, steam.GetAppInfo(appid, progress));
                }
                return appinfo;
            }
            else
            {
                appinfo.Data.Add(appid, SteamCMDApi.GetAppInfo(appid, progress));
                if (!appinfo.Data.ContainsKey(appid))
                {
                    appinfo.Data.Add(appid, steam.GetAppInfo(appid, progress));
                }
                return appinfo;
            }
        }
    }
}
