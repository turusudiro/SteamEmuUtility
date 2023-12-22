using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DownloaderCommon;
using Newtonsoft.Json;
using Playnite.SDK;
using SteamCommon.Models;

namespace SteamCommon
{
    public class SteamAppDetails
    {
        private const string apiUrl = "https://store.steampowered.com/api/appdetails?appids=";
        public static async Task<SteamAppDetails<string, AppDetailsInfo>> GetAppDetailsStore(IEnumerable<string> appids, GlobalProgressActionArgs progressOptions)
        {
            return await GetAppDetailsStore(appids, progressOptions, null);
        }
        public static async Task<SteamAppDetails<string, AppDetailsInfo>> GetAppDetailsStore(IEnumerable<string> appids, GlobalProgressActionArgs progressOptions, SteamAppDetails<string, AppDetailsInfo> appDetails)
        {
            progressOptions.IsIndeterminate = false;
            progressOptions.ProgressMaxValue = appids.Count();
            if (appDetails == null)
            {
                appDetails = new SteamAppDetails<string, AppDetailsInfo>();
            }
            foreach (var appid in appids)
            {
                progressOptions.Text = $"Downloading info from appdetails for {appid}";
                var response = HttpDownloader.DownloadString($"{apiUrl}{appid}");
                if (response.Length > 0)
                {
                    var appresult = JsonConvert.DeserializeObject<Dictionary<string, AppDetailsInfo>>(response);
                    appDetails.Add(appid, appresult[appid]);
                }
            }
            return appDetails;
        }
    }
}
