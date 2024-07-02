using DownloaderCommon;
using Playnite.SDK.Data;
using SteamCommon.Models;
using System.Collections.Generic;

namespace SteamCommon
{
    public class SteamAppDetails
    {
        private const string apiUrl = "https://store.steampowered.com/api/appdetails?appids=";
        public static AppDetailsInfo GetAppDetailsStore(string appid)
        {
            var response = HttpDownloader.DownloadString($"{apiUrl}{appid}");
            if (response.Length > 0)
            {
                if (Serialization.TryFromJson(response, out Dictionary<string, AppDetailsInfo> appresult))
                {
                    return appresult[appid];
                }
            }
            return null;
        }
    }
}
