using Gameloop.Vdf;
using Gameloop.Vdf.JsonConverter;
using Playnite.SDK;
using SteamCommon.Models;
using SteamKit2;
using SteamKit2.Unified.Internal;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace SteamCommon
{
    public class SteamService : IDisposable
    {
        private int attemptReconnect = 0;
        public bool LoggedOn = false;
        private SteamClient steamClient;
        private CallbackManager manager;
        private SteamUser steamUser;
        private SteamApps steamApps;
        private bool disposed = false;

        public SteamService()
        {
            steamClient = new SteamClient();
            steamApps = steamClient.GetHandler<SteamApps>();
            steamUser = steamClient.GetHandler<SteamUser>();
            manager = new CallbackManager(steamClient);
        }
        public PublishedFileDetails GetPublishedFileDetails(ulong publishedfileid, GlobalProgressActionArgs progress)
        {
            if (!LoggedOn)
            {
                if (!AnonymousLogin(progress))
                {
                    return null;
                }
                LoggedOn = true;
            }
            progress.Text = $"{ResourceProvider.GetString("LOCSEU_Downloading")} publisedfileid {publishedfileid}";
            try
            {
                PublishedFileID req = new PublishedFileID(publishedfileid);
                var pubFileRequest = new CPublishedFile_GetDetails_Request();
                pubFileRequest.publishedfileids.Add(req);
                SteamUnifiedMessages.UnifiedService<IPublishedFile> steamPublishedFile = steamClient.GetHandler<SteamUnifiedMessages>().CreateService<IPublishedFile>();
                progress.CancelToken.ThrowIfCancellationRequested();
                var publishedFileDetails = steamPublishedFile.SendMessage(x => x.GetDetails(pubFileRequest)).ToTask().Result.GetDeserializedResponse<CPublishedFile_GetDetails_Response>().publishedfiledetails;
                return publishedFileDetails.FirstOrDefault();
            }
            catch { return null; }
        }
        private AppIdInfo ParseAppInfo(KeyValue keyValue)
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
        public AppIdInfo GetAppInfo(string id, GlobalProgressActionArgs progress)
        {
            AppIdInfo app = new AppIdInfo();
            if (!LoggedOn)
            {
                if (!AnonymousLogin(progress))
                {
                    return app;
                }
                LoggedOn = true;
            }
            uint appid = uint.Parse(id);
            var pics = steamApps.PICSGetProductInfo(appid, null).ToTask().Result;
            if (!pics.Failed && pics.Results.Any(x => x.Apps.ContainsKey(appid)))
            {
                app = ParseAppInfo(pics.Results.FirstOrDefault(x => x.Apps.ContainsKey(appid)).Apps[appid].KeyValues);
            }
            return app;
        }
        private bool AnonymousLogin(GlobalProgressActionArgs progress)
        {
            manager.Subscribe<SteamClient.ConnectedCallback>(callback =>
            {
                OnConnected(callback, progress);
            });
            manager.Subscribe<SteamClient.DisconnectedCallback>(callback =>
            {
                OnDisconnected(callback, progress);
            });
            manager.Subscribe<SteamUser.LoggedOnCallback>(callback =>
            {
                OnLoggedOn(callback, progress);
            });
            manager.Subscribe<SteamUser.LoggedOffCallback>(callback =>
            {
                OnLoggedOff(callback, progress);
            });
            steamClient.Connect();

            while (!LoggedOn)
            {
                if (progress.CancelToken.IsCancellationRequested)
                {
                    LoggedOn = false;
                    Dispose();
                }
                if (!steamClient.IsConnected)
                {
                    progress.Text = ResourceProvider.GetString("LOCSEU_SteamConnecting");
                    while (!steamClient.IsConnected)
                    {
                        if (progress.CancelToken.IsCancellationRequested)
                        {
                            Dispose();
                            LoggedOn = false;
                        }
                        manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
                    }
                }
                progress.Text = ResourceProvider.GetString("LOCSEU_SteamAnonymousLogging");

                steamUser.LogOnAnonymous(new SteamUser.AnonymousLogOnDetails());
                manager.RunWaitCallbacks(TimeSpan.FromSeconds(3));
            }
            progress.IsIndeterminate = false;
            return LoggedOn;

        }
        void OnConnected(SteamClient.ConnectedCallback callback, GlobalProgressActionArgs a)
        {
            if (callback.Result != EResult.OK)
            {
                a.Text = $"{ResourceProvider.GetString("LOCSEU_SteamUnableToConnect")}: {callback.Result}";
                return;
            }
        }

        void OnDisconnected(SteamClient.DisconnectedCallback callback, GlobalProgressActionArgs a)
        {
            if (attemptReconnect == 2 || callback.UserInitiated)
            {
                return;
            }
            attemptReconnect++;
            for (int i = 3; i >= 0; i--)
            {
                a.Text = string.Format(ResourceProvider.GetString("LOCSEU_SteamReconnecting"), i);
                if (a.CancelToken.IsCancellationRequested)
                {
                    return;
                }
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            steamClient.Connect();
        }

        void OnLoggedOn(SteamUser.LoggedOnCallback callback, GlobalProgressActionArgs a)
        {
            if (callback.Result != EResult.OK)
            {
                a.Text = $"{ResourceProvider.GetString("LOCSEU_SteamUnableToLogon")}: {callback.Result} / {callback.ExtendedResult}";

                return;
            }
            a.Text = ResourceProvider.GetString("LOCSEU_SteamLoggedOn");
            LoggedOn = true;
        }

        void OnLoggedOff(SteamUser.LoggedOffCallback callback, GlobalProgressActionArgs a)
        {
            a.Text = $"{ResourceProvider.GetString("LOCSEU_SteamLoggedOff")}: {callback.Result}";
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose of managed resources
                    steamClient.Disconnect();
                }

                // Dispose of unmanaged resources

                disposed = true;
            }
        }

        ~SteamService()
        {
            Dispose(false);
        }
    }

}
