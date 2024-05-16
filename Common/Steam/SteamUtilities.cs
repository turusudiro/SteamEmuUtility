using Playnite.SDK;
using SteamCommon.Models;

namespace SteamCommon
{
    public static class SteamUtilities
    {
        public static AppIdInfo GetAppIdInfo(string appid, SteamService steam, GlobalProgressActionArgs progress)
        {
            if (SteamCMDApi.GetAppInfo(appid, progress, out AppIdInfo info))
            {
                return info;
            }
            return steam.GetAppInfo(appid, progress);
        }
    }
}
