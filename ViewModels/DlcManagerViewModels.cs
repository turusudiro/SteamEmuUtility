using ImageCommon;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using SteamCommon.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SteamEmuUtility.ViewModels
{
    class DlcManagerViewModels : ObservableObject, IDisposable
    {
        private object _dlcListLock = new object();
        private bool disposed = false;
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
                FileSystem.WriteStringToFile(dlcPath, Serialization.ToJson(DLCList, true));

                cancellationTokenSource.Cancel();

                taskRunning = false;

                lock (_dlcListLock)
                {
                    foreach (var item in DLCList)
                    {
                        item.Dispose();
                    }
                    DLCList.Clear(); // Clear the DLCList
                }

                steam.Dispose();

                cancellationTokenSource.Dispose();
            }

            disposed = true;
        }

        ~DlcManagerViewModels()
        {
            Dispose(false);
        }

        private const string steamItemAssetsUrl = "https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/{0}";
        private CancellationTokenSource cancellationTokenSource;
        public readonly SteamService steam;
        private string dlcPath;
        private readonly Game game;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly string apiKey;
        private readonly string pluginPath;
        private bool taskRunning;
        public DlcManagerViewModels(IPlayniteAPI api, Game game, string apiKey, string pluginPath)
        {
            EmptyImage = ImageUtilites.CreateQuestionMarkImage(231, 87);
            DLCList = new ObservableCollection<DlcInfo>();
            cancellationTokenSource = new CancellationTokenSource();
            PlayniteApi = api;
            this.game = game;
            this.apiKey = apiKey;
            this.pluginPath = pluginPath;
            dlcPath = Path.Combine(pluginPath, "GamesInfo", $"{game.GameId}.json");
            steam = new SteamService(CallbackHandler);
            LoadDlcListAsync();
        }

        private bool enableAll;
        public bool EnableAll
        {
            get => enableAll;
            set
            {
                if (enableAll != value)
                {
                    enableAll = value;
                    OnPropertyChanged();
                    DLCList.ForEach(x => x.Enable = value);
                }
            }
        }
        private int totalItems;
        private int currentProgress;
        private double progressPercentage;
        private string progressText;
        public string ProgressText
        {
            get => progressText;
            set
            {
                progressText = value;
                OnPropertyChanged();
            }
        }
        private bool isProgressBarVisible = true;
        public bool IsProgressBarVisible
        {
            get => isProgressBarVisible;
            set
            {
                isProgressBarVisible = value;
                OnPropertyChanged();
            }
        }
        public int TotalItems
        {
            get => totalItems;
            set
            {
                totalItems = value;
                OnPropertyChanged();
            }
        }
        public int CurrentProgress
        {
            get => currentProgress;
            set
            {
                currentProgress = value;
                OnPropertyChanged();
                UpdateProgressPercentage();
            }
        }
        public double ProgressPercentage
        {
            get => progressPercentage;
            set
            {
                progressPercentage = value;
                OnPropertyChanged();
            }
        }
        private bool isIndeterminate = true;
        public bool IsIndeterminate
        {
            get { return isIndeterminate; }
            set
            {
                OnPropertyChanged();
                isIndeterminate = value;
            }
        }
        private BitmapImage EmptyImage { get; }
        private ObservableCollection<DlcInfo> _dlcList;
        public ObservableCollection<DlcInfo> DLCList
        {
            get => _dlcList;
            set
            {
                _dlcList = value;
                OnPropertyChanged();
            }
        }

        void ProcessDLCAsync()
        {
            taskRunning = true;

            IsIndeterminate = false;

            TotalItems = DLCList.Count;

            CurrentProgress = 0;

            IEnumerable<DlcInfo> app = null;
            lock (_dlcListLock)
            {
                app = new ObservableCollection<DlcInfo>(DLCList);
            }
            foreach (var x in app)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    taskRunning = false;
                    break;
                }

                var thedlc = DLCList.FirstOrDefault(s => s.Appid == x.Appid);


                if (string.IsNullOrEmpty(thedlc.ImageURL))
                {
                    thedlc.Image = EmptyImage;
                }
                else
                {
                    ProgressText = string.Format(ResourceProvider.GetString("LOCSEU_FetchingImages"), thedlc.Name);

                    string imagePath = Path.Combine(pluginPath, "GamesInfo", "Assets", game.GameId, $"{thedlc.Appid.ToString()}.jpg");

                    if (FileSystem.FileExists(imagePath))
                    {
                        // dont call UIDispatcher to invoke, since LoadImageFromFile do bitmap.Freeze which thread-safe
                        thedlc.Image = ImageUtilites.LoadImageFromFile(imagePath);
                        continue;
                    }
                    else
                    {
                        string url = string.Format(steamItemAssetsUrl, thedlc.Appid + "//" + thedlc.ImageURL);
                        var image = ImageUtilites.DownloadImage(url);
                        ImageUtilites.SaveImage(imagePath, image);

                        thedlc.Image = image;
                        thedlc.ImagePath = imagePath;
                    }
                }
                CurrentProgress++;
            }
            IsProgressBarVisible = false;
            taskRunning = false;
        }

        void CallbackHandler(object obj)
        {
            if (obj is string text)
            {
                ProgressText = text;
            }
            else if (obj is App app)
            {
                string name = !string.IsNullOrEmpty(app?.Name) ? app.Name : "Unknown App";
                var newDlc = new DlcInfo
                {
                    Appid = app.Appid,
                    Name = name,
                    Image = EmptyImage
                };

                PlayniteApi.MainView.UIDispatcher.Invoke(() =>
                {
                    lock (_dlcListLock)
                    {
                        DLCList.Add(newDlc);
                        ProgressText = string.Format(ResourceProvider.GetString("LOCSEU_Adding"), name);
                    }
                });

                if (!string.IsNullOrEmpty(app?.SmallCapsuleImage?.English))
                {
                    string filePath = Path.Combine(pluginPath, "GamesInfo", "Assets", game.GameId, $"{app.Appid}.jpg");
                    newDlc.ImageURL = app.SmallCapsuleImage?.English;
                    newDlc.ImagePath = filePath;
                }
            }
        }

        private void LoadDlcListAsync()
        {
            Task.Run(() =>
            {
                if (FileSystem.FileExists(dlcPath) && Serialization.TryFromJsonFile(dlcPath, out ObservableCollection<DlcInfo> result) && result?.Count >= 1)
                {
                    result.ForEach(x => x.Image = EmptyImage);
                    DLCList = result;

                    ProcessDLCAsync();
                }
                else
                {
                    GetDLCList();
                }
            });
        }

        private void GetDLCList()
        {
            taskRunning = true;

            IsProgressBarVisible = true;

            IsIndeterminate = true;

            uint appid = uint.Parse(game.GameId);

            ProgressText = ResourceProvider.GetString("LOCSEU_GettingDlcList");

            var DLCs = SteamUtilities.GetDLC(game.GameId, steam, CallbackHandler, apiKey: apiKey, pluginPath: pluginPath).ToList();

            TotalItems = DLCs.Count;
            CurrentProgress = 0;

            ProgressText = ResourceProvider.GetString("LOCSEU_GettingDlcInfo");

            SteamUtilities.GetApp(DLCs, steam, CallbackHandler);

            FileSystem.WriteStringToFile(dlcPath, Serialization.ToJson(DLCList, true));

            ProcessDLCAsync();
        }

        private void UpdateProgressPercentage()
        {
            if (TotalItems > 0)
            {
                ProgressPercentage = Math.Round((double)CurrentProgress / TotalItems * 100, 2);
            }
            else
            {
                ProgressPercentage = 0;
            }
        }

        public RelayCommand Refresh
        {
            get => new RelayCommand(async () =>
            {
                IsProgressBarVisible = false;

                cancellationTokenSource.Cancel();

                while (taskRunning)
                {
                    await Task.Delay(2000);
                }

                cancellationTokenSource = new CancellationTokenSource();

                DLCList?.Clear();

                await Task.Run(() =>
                {
                    GetDLCList();
                }, cancellationTokenSource.Token);
            });
        }
    }
}
