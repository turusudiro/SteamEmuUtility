using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Playnite.SDK;
using SteamCommon.Models;
using SteamKit2;

namespace SteamCommon
{
    public interface ISteamService
    {
        AppInfo GetAppInfo(IEnumerable<uint> appid, GlobalProgressActionArgs progressOptions);
    }
    public class SteamService : ISteamService
    {
        private int attemptReconnect = 0;
        private bool LoggedOn = false;
        private bool isRunning = true;
        public SteamClient steamClient;
        private CallbackManager manager;
        public SteamUser steamUser;
        private SteamApps steamApps;
        public SteamService()
        {
            steamClient = new SteamClient();
            steamApps = steamClient.GetHandler<SteamApps>();
            steamUser = steamClient.GetHandler<SteamUser>();
            manager = new CallbackManager(steamClient);
        }
        public AppIdInfo ParseAppInfo(KeyValue keyValue, GlobalProgressActionArgs progressOptions)
        {
            using (Stream stream = new MemoryStream())
            {
                keyValue.SaveToStream(stream, false);
                stream.Seek(0, SeekOrigin.Begin);
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    var vdf = streamReader.ReadToEnd();
                    var vdftojson = VdfConvert.Deserialize(vdf).ToJson().FirstOrDefault();
                    var appinfo = vdftojson.ToObject<AppIdInfo>();
                    return appinfo;
                }
            }
        }
        public AppInfo GetAppInfo(IEnumerable<uint> appids, GlobalProgressActionArgs progressOptions)
        {
            return GetAppInfo(appids, progressOptions, null);
        }
        public AppInfo GetAppInfo(IEnumerable<uint> appids, GlobalProgressActionArgs progressOptions, AppInfo AppInfo)
        {
            if (AppInfo == null)
            {
                AppInfo = new AppInfo();
            }
            if (AppInfo.Data == null)
            {
                AppInfo.Data = new Dictionary<string, AppIdInfo>();
            }
            if (!LoggedOn)
            {
                if (!AnonymousLogin(progressOptions))
                {
                    return null;
                }
                LoggedOn = true;
            }
            progressOptions.IsIndeterminate = true;
            foreach (uint appid in appids)
            {
                if (progressOptions.CancelToken.IsCancellationRequested)
                {
                    return null;
                }
                var pics = steamApps.PICSGetProductInfo(appid, null).ToTask().Result;
                if (!pics.Failed)
                {
                    foreach (var item in pics.Results.Where(x => x.Apps.ContainsKey(appid)))
                    {
                        progressOptions.Text = $"Configuring {item.Apps[appid].KeyValues["common"]["name"].Value}";
                        AppInfo.Data.Add(appid.ToString(), ParseAppInfo(item.Apps[appid].KeyValues, progressOptions));
                    }
                }
            }
            return AppInfo;
        }

        public bool AnonymousLogin(GlobalProgressActionArgs progressOptions)
        {
            manager.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                OnConnected(callback, progressOptions);
            });
            manager.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                OnDisconnected(callback, progressOptions);
            });
            manager.Subscribe<SteamUser.LoggedOnCallback>(callback =>
            {
                OnLoggedOn(callback, progressOptions);
            });
            manager.Subscribe<SteamUser.LoggedOffCallback>(callback =>
            {
                OnLoggedOff(callback, progressOptions);
            });
            steamClient.Connect();
            progressOptions.Text = ("Connecting to Steam...");
            while (!steamClient.IsConnected)
            {
                if (progressOptions.CancelToken.IsCancellationRequested)
                {
                    return false;
                }
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(3));
            }
            steamUser.LogOnAnonymous(new SteamUser.AnonymousLogOnDetails());
            progressOptions.Text = ("Logging in anonymously");
            while (!LoggedOn)
            {
                if (progressOptions.CancelToken.IsCancellationRequested)
                {
                    return false;
                }
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(3));
            }
            progressOptions.IsIndeterminate = false;
            return true;
        }
        void OnConnected(SteamClient.ConnectedCallback callback, GlobalProgressActionArgs a)
        {
            if (callback.Result != EResult.OK)
            {
                a.Text = $"Unable to connect to Steam: {callback.Result}";
                return;
            }
        }

        void OnDisconnected(SteamClient.DisconnectedCallback callback, GlobalProgressActionArgs a)
        {
            if (attemptReconnect == 2 || callback.UserInitiated)
            {
                isRunning = false;
                return;
            }
            attemptReconnect++;
            // after recieving an AccountLogonDenied, we'll be disconnected from steam
            // so after we read an authcode from the user, we need to reconnect to begin the logon flow again
            a.Text = "Disconnected from Steam, reconnecting in 3...";
            Thread.Sleep(TimeSpan.FromSeconds(3));
            if (a.CancelToken.IsCancellationRequested)
            {
                isRunning = false;
                return;
            }
            steamClient.Connect();
        }

        void OnLoggedOn(SteamUser.LoggedOnCallback callback, GlobalProgressActionArgs a)
        {
            if (callback.Result != EResult.OK)
            {
                a.Text = ($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");

                return;
            }
            a.Text = ("Successfully logged on!");
            LoggedOn = true;
        }

        void OnLoggedOff(SteamUser.LoggedOffCallback callback, GlobalProgressActionArgs a)
        {
            a.Text = $"Logged off of Steam: {callback.Result}";
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }
    }

}
