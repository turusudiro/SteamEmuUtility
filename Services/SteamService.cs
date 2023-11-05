using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;
using Playnite.SDK.Data;
using SteamEmuUtility.Services.Models;
using SteamKit2;

namespace ServiceCommon
{
    public interface ISteamService
    {
        Task<bool> GetDLCStore(int appid, string CommonPath);
        bool GetDLCSteamKit2(List<uint> appid, GlobalProgressActionArgs progressOptions, string CommonPath);
    }
    public class SteamService : ISteamService
    {
        private const string apiUrl = "https://store.steampowered.com/api/appdetails?appids=";
        private bool login;
        private bool isRunning = true;
        private SteamClient steamClient;
        private CallbackManager manager;
        private SteamUser steamUser;
        private SteamApps steamApps;
        private static readonly ILogger logger = LogManager.GetLogger();
        public SteamService()
        {
            steamClient = new SteamClient();
            steamApps = steamClient.GetHandler<SteamApps>();
            steamUser = steamClient.GetHandler<SteamUser>();
            manager = new CallbackManager(steamClient);
        }
        public async Task<bool> GetDLCStore(int appid, string CommonPath)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync($"{apiUrl}{appid}");
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
                        File.WriteAllLines($"{CommonPath}\\{appid}.txt", dlc);
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
        public bool GetDLCSteamKit2(List<uint> appids, GlobalProgressActionArgs progressOptions, string CommonPath)
        {
            progressOptions.IsIndeterminate = true;
            List<string> listdlc = new List<string>();
            manager.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                OnConnected(callback, progressOptions);
            });
            manager.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                OnDisconnected(callback, progressOptions);
            });

            //manager.Subscribe<SteamApps.PICSProductInfoCallback>(callback =>
            //{
            //    PICSProductInfo(callback, listdlc, progressOptions, CommonPath, appids);
            //});

            manager.Subscribe<SteamUser.LoggedOnCallback>(callback =>
            {
                OnLoggedOn(callback, progressOptions);
            });
            manager.Subscribe<SteamUser.LoggedOffCallback>(callback =>
            {
                OnLoggedOff(callback, progressOptions);
            });



            logger.Info("Connecting");
            progressOptions.Text = ("Connecting to Steam...");

            steamClient.Connect();
            while (!steamClient.IsConnected)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(3));
            }
            login = true;
            steamUser.LogOnAnonymous(new SteamUser.AnonymousLogOnDetails());
            logger.Info("Logging in anonymously");
            progressOptions.Text = ("Logging in anonymously");
            while (login)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            progressOptions.IsIndeterminate = false;
            int count = 0;
            logger.Info(appids.Count.ToString());
            foreach (uint appid in appids)
            {
                progressOptions.Text = $"Getting Product Info for {appid}";
                progressOptions.CurrentProgressValue++;
                steamApps.PICSGetProductInfo(appid, appid).ToTask().Wait();
                manager.Subscribe<SteamApps.PICSProductInfoCallback>(callback =>
                {
                    count = GenerateDLCGreenLuma(callback, progressOptions, appid, CommonPath, listdlc, count);
                    //if (callback.Apps.ContainsKey(appid))
                    //{
                    //    var productInfo = callback.Apps[appid];
                    //    if (productInfo.KeyValues["extended"]["listofdlc"] == KeyValue.Invalid)
                    //    {
                    //        logger.Error("Keyvalue is invalid, no DLC Found!");
                    //        progressOptions.Text = ("Keyvalue is invalid, no DLC Found!");
                    //    }
                    //    else
                    //    {
                    //        if (!Directory.Exists(CommonPath))
                    //        {
                    //            Directory.CreateDirectory(CommonPath);
                    //        }
                    //        var listofdlc = productInfo.KeyValues["extended"]["listofdlc"];
                    //        listdlc.AddRange(listofdlc.Value.Split(',').Select(s => s.Trim()).ToList());
                    //        File.WriteAllLines($"{CommonPath}\\{appid}.txt", listdlc);
                    //    }
                    //}
                });
            }
            steamClient.Disconnect();
            while (isRunning)
            {
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            logger.Info("Disconnected from Steam");

            if (count == appids.Count)
            {

                return true;
            }
            return false;
        }
        int GenerateDLCGreenLuma(SteamApps.PICSProductInfoCallback callback, GlobalProgressActionArgs progressOptions, uint appid, string CommonPath, List<string> listdlc, int count)
        {
            if (callback.Apps.ContainsKey(appid))
            {
                var productInfo = callback.Apps[appid];
                if (productInfo.KeyValues["extended"]["listofdlc"] == KeyValue.Invalid)
                {
                    logger.Error("Keyvalue is invalid, no DLC Found!");
                    progressOptions.Text = ("Keyvalue is invalid, no DLC Found!");
                }
                else
                {
                    if (!Directory.Exists(CommonPath))
                    {
                        Directory.CreateDirectory(CommonPath);
                    }
                    var listofdlc = productInfo.KeyValues["extended"]["listofdlc"];
                    listdlc.AddRange(listofdlc.Value.Split(',').Select(s => s.Trim()).ToList());
                    File.WriteAllLines($"{CommonPath}\\{appid}.txt", listdlc);
                    count++;
                }
            }
            return count;
        }
        //void PICSProductInfo(SteamApps.PICSProductInfoCallback callback, List<string> listdlc, GlobalProgressActionArgs a, string CommonPath, uint appid)
        //{
        //    logger.Info("Getting DLC");
        //    a.Text = "Getting Product Info";
        //    if (callback.Apps.ContainsKey(appid))
        //    {
        //        var productInfo = callback.Apps[appid];
        //        //var tes = productInfo.KeyValues["extended"].Children;
        //        //foreach (var line in tes)
        //        //{
        //        //    logger.Info(line.ToString());
        //        //}
        //        if (productInfo.KeyValues["extended"]["listofdlc"] == KeyValue.Invalid)
        //        {
        //            logger.Error("Keyvalue is invalid, no DLC Found!");
        //            a.Text = ("Keyvalue is invalid, no DLC Found!");
        //        }
        //        else
        //        {
        //            if (!Directory.Exists(CommonPath))
        //            {
        //                Directory.CreateDirectory(CommonPath);
        //            }
        //            var listofdlc = productInfo.KeyValues["extended"]["listofdlc"];
        //            listdlc.AddRange(listofdlc.Value.Split(',').Select(s => s.Trim()).ToList());
        //            //listdlc.AddRange(filter);
        //            //logger.Info(filter.Count.ToString());
        //        }
        //    }
        //}

        void OnConnected(SteamClient.ConnectedCallback callback, GlobalProgressActionArgs a)
        {
            if (callback.Result != EResult.OK)
            {
                a.Text = $"Unable to connect to Steam: {callback.Result}";
                Console.WriteLine("Unable to connect to Steam: {0}", callback.Result);
                return;
            }
        }

        void OnDisconnected(SteamClient.DisconnectedCallback callback, GlobalProgressActionArgs a)
        {
            // after recieving an AccountLogonDenied, we'll be disconnected from steam
            // so after we read an authcode from the user, we need to reconnect to begin the logon flow again
            a.Text = ("Disconnected");
            isRunning = false;
        }

        void OnLoggedOn(SteamUser.LoggedOnCallback callback, GlobalProgressActionArgs a)
        {
            if (callback.Result != EResult.OK)
            {
                logger.Error($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");
                a.Text = ($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                return;
            }
            logger.Info("logged on!");
            a.Text = ("Successfully logged on!");
            login = false;
        }

        void OnLoggedOff(SteamUser.LoggedOffCallback callback, GlobalProgressActionArgs a)
        {
            logger.Info($"Logged off of Steam: {callback.Result}");
            a.Text = $"Logged off of Steam: {callback.Result}";
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }
    }

}
