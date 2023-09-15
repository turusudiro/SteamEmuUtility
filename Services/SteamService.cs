using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SteamEmuUtility.Services.Models;

namespace SteamEmuUtility.Services
{
    public interface ISteamService
    {
        Task<bool> GetDLCStore(Game game, GlobalProgressActionArgs a);
    }
    public class SteamService : ISteamService
    {
        private const string apiUrl = "https://store.steampowered.com/api/appdetails?appids=";
        private string CommonPath { get; set; }
        private readonly ILogger logger;
        private readonly SteamEmuUtility plugin;
        public SteamService(SteamEmuUtility plugin, ILogger logger)
        {
            this.logger = logger;
            this.plugin = plugin;
            CommonPath = Path.Combine(plugin.GetPluginUserDataPath(), "Common");
        }
        public async Task<bool> GetDLCStore(Game game, GlobalProgressActionArgs a)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{apiUrl}{game.GameId}");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var json = Serialization.FromJson<Dictionary<string, SteamAppDetails>>(jsonResponse);
                    var appDetails = new SteamAppDetails();
                    appDetails = json[json.Keys.First()];
                    if (appDetails.data.dlc != null)
                    {
                        logger.Info($"Found {appDetails.data.dlc.Count} DLC");
                        string[] dlc = appDetails.data.dlc.Select(i => i.ToString()).ToArray();
                        if (!Directory.Exists(CommonPath))
                        {
                            Directory.CreateDirectory(CommonPath);
                        }
                        File.WriteAllLines($"{CommonPath}\\{game.GameId}.txt", dlc);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }

}
