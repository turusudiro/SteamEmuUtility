using Playnite.SDK;
using SteamKit2;
using SteamKit2.Unified.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SteamCommon.Models;

namespace SteamCommon
{
    public class SteamService : IDisposable
    {
        public SteamCallback Callbacks { get; private set; }

        private Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> PICSProductInfo;
        private bool isRunning = false;
        private bool disposed = false;
        private int attemptReconnect = 0;
        private bool loggedOn = false;
        private CallbackManager manager;
        private SteamApps steamApps;
        private SteamClient steamClient;
        private SteamUser steamUser;
        private bool connecting = false;
        public bool connected = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (Callbacks != null)
                {
                    Callbacks.OnProgressUpdate -= Callbacks.InvokeProgressUpdate;
                    Callbacks.OnLoggedOn -= Callbacks.InvokeLoggedOn;
                    Callbacks.OnAppCallback -= Callbacks.InvokeAppCallback;

                    Callbacks = null;
                }


                if (steamClient != null)
                {
                    steamClient.Disconnect();
                }

                manager = null;
                steamApps = null;
                steamUser = null;
                steamClient = null;
            }

            disposed = true;
        }
        public SteamService()
        {
            Callbacks = new SteamCallback();

            steamClient = new SteamClient();
            steamApps = steamClient.GetHandler<SteamApps>();
            steamUser = steamClient.GetHandler<SteamUser>();
            manager = new CallbackManager(steamClient);
            manager.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                OnConnected(callback);
            });
            manager.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                OnDisconnected(callback);
            });
            manager.Subscribe<SteamUser.LoggedOnCallback>(callback =>
            {
                OnLoggedOn(callback);
            });
        }
        public void AnonymousLogin()
        {
            steamClient.Connect();

            Callbacks.InvokeProgressUpdate(ResourceProvider.GetResource("LOCSEU_SteamConnecting").ToString());
            connecting = true;

            while (!connected)
            {
                while (connecting)
                {
                    RunWaitCallbacks();
                }

                if (connected)
                {
                    connected = true;
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }
            steamUser.LogOnAnonymous(new SteamUser.AnonymousLogOnDetails());


            Callbacks.InvokeProgressUpdate(ResourceProvider.GetResource("LOCSEU_SteamAnonymousLogging").ToString());
            while (!loggedOn)
            {
                RunWaitCallbacks();
            }
        }
        public PublishedFileDetails GetPublishedFileDetails(ulong publishedfileid)
        {
            if (!loggedOn)
            {
                AnonymousLogin();
            }

            Callbacks.InvokeProgressUpdate($"{ResourceProvider.GetString("LOCSEU_Downloading")} publisedfileid {publishedfileid}");
            try
            {
                PublishedFileID req = new PublishedFileID(publishedfileid);
                var pubFileRequest = new CPublishedFile_GetDetails_Request();
                pubFileRequest.publishedfileids.Add(req);
                SteamUnifiedMessages.UnifiedService<IPublishedFile> steamPublishedFile = steamClient.GetHandler<SteamUnifiedMessages>().CreateService<IPublishedFile>();
                var publishedFileDetails = steamPublishedFile.SendMessage(x => x.GetDetails(pubFileRequest)).ToTask().Result.GetDeserializedResponse<CPublishedFile_GetDetails_Response>().publishedfiledetails;
                return publishedFileDetails.FirstOrDefault();
            }
            catch { return null; }
        }
        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> GetAppInfo(IEnumerable<uint> appids)
        {
            if (!loggedOn)
            {
                AnonymousLogin();
            }

            var task = manager.Subscribe<SteamApps.PICSProductInfoCallback>(OnPICSProductInfo);

            steamApps.PICSGetProductInfo(appids, new List<uint>());

            isRunning = true;

            while (isRunning)
            {
                RunWaitCallbacks();
            }
            task.Dispose();

            return PICSProductInfo;
        }
        public Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> GetAppInfo(uint appid)
        {
            if (!loggedOn)
            {
                AnonymousLogin();
            }

            var task = manager.Subscribe<SteamApps.PICSProductInfoCallback>(callback =>
            {
                OnPICSProductInfo(callback);
            });
            steamApps.PICSGetProductInfo(appid, null);

            isRunning = true;

            while (isRunning)
            {
                RunWaitCallbacks();
            }
            task.Dispose();

            return PICSProductInfo;
        }
        private void RunWaitCallbacks()
        {
            manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }
        private void OnPICSProductInfo(SteamApps.PICSProductInfoCallback callback)
        {
            if (PICSProductInfo == null)
            {
                PICSProductInfo = new Dictionary<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo>();
            }
            foreach (var apps in callback.Apps)
            {
                PICSProductInfo[apps.Key] = apps.Value;
            }
            if (!callback.ResponsePending)
            {
                isRunning = false;
            }
        }
        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                connecting = false;
                return;
            }
            connecting = false;
            connected = true;
        }
        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            if (attemptReconnect == 2 || callback.UserInitiated)
            {
                return;
            }
            attemptReconnect++;
            for (int i = 3; i >= 0; i--)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            steamClient.Connect();
        }
        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                return;
            }
            Callbacks.InvokeProgressUpdate(ResourceProvider.GetResource("LOCSEU_SteamLoggedOn").ToString());
            Callbacks.InvokeLoggedOn(true);
            loggedOn = true;
        }
    }
}