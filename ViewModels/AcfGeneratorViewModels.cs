using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AcfGenerator.Models;
using Playnite.SDK;
using PluginsCommon;
using ProcessCommon;
using SteamCommon;
using SteamCommon.Models;
using SteamEmuUtility.Views;
using SteamKit2;

namespace SteamEmuUtility.ViewModels
{
    class AcfGeneratorViewModels : ObservableObject, IDisposable
    {
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
                if (steam != null)
                {
                    steam.Dispose();
                }
            }

            disposed = true;
        }

        ~AcfGeneratorViewModels()
        {
            Dispose(false);
        }

        private readonly IPlayniteAPI PlayniteApi;
        private readonly SteamService steam;
        public AcfGeneratorViewModels(IPlayniteAPI api)
        {
            PlayniteApi = api;
            Games = new ObservableCollection<Acf>();
            SteamLibraryFolders = Steam.GetSteamLibraryFolders()
            .Select(x => Path.Combine(x, "steamapps")).Where(FileSystem.DirectoryExists).ToList();

            steam = new SteamService();
            steam.Callbacks.OnProgressUpdate += OnProgressUpdate;
            steam.Callbacks.OnAppCallback += OnAppCallback;
            steam.Callbacks.OnLoggedOn += OnLoggedOn;
            Games.Add(new Acf() { SelectedTarget = SteamLibraryFolders.FirstOrDefault(), AppID = 480 });
            IsProgressBarVisible = false;
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
        private string steamText;
        public string SteamText
        {
            get => steamText;
            set
            {
                if (steamText != value)
                {
                    steamText = value;
                    OnPropertyChanged();
                }
            }
        }
        public IEnumerable<string> SteamLibraryFolders { get; }
        private ObservableCollection<Acf> games;
        public ObservableCollection<Acf> Games
        {
            get => games;
            set
            {
                if (games != value)
                {
                    games = value;
                    OnPropertyChanged();
                }
            }
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
                }
            }
        }
        public RelayCommand AddItem => new RelayCommand(() => Games.Add(new Acf() { SelectedTarget = SteamLibraryFolders.FirstOrDefault(), AppID = 480 }));
        public RelayCommand<Acf> RemoveItem => new RelayCommand<Acf>((a) => { Games.Remove(a); });
        public RelayCommand<Acf> RefreshButton => new RelayCommand<Acf>((a) => { Refresh(); });
        public RelayCommand Generate => new RelayCommand(() =>
        {
            if (generateTask.IsCompleted)
            {
                generateTask = GenerateAcfAsync();
            }
        });
        private Task generateTask = Task.CompletedTask;
        public RelayCommand<string> OpenDir
        {
            get => new RelayCommand<string>((dir) =>
            {
                ProcessUtilities.StartProcess(dir);
            });
        }
        private void OnLoggedOn(bool loggedOn)
        {
            if (loggedOn)
            {
                IsProgressBarVisible = false;
            }
        }
        private void OnProgressUpdate(string text)
        {
            IsProgressBarVisible = true;
            SteamText = text;
        }
        private async void OnAppCallback(App app)
        {
            Acf game = Games.FirstOrDefault(x => x.AppID.Equals(app.Appid));

            if (game == null)
            {
                game = new Acf() { AppID = app.Appid };
                Games.Add(game);
                await Task.Delay(500);
            }

            if (string.IsNullOrEmpty(app.Installdir))
            {
                game.AppID = 0;
                game.InstallDir = string.Empty;
                game.Status = ResourceProvider.GetString("LOCSEU_Invalid");
                return;
            }

            game.InstallDir = app.Installdir;
            game.Status = ResourceProvider.GetString("LOCSEU_ParsingToAcfKV");
            await Task.Delay(500);

            var kv = ParseToAcf(app);
            string destinationPath = Path.Combine(game.SelectedTarget, $"appmanifest_{game.AppID}.acf");
            string destinationGamePath = Path.Combine(game.SelectedTarget, "common", app.Installdir);

            game.Status = ResourceProvider.GetString("LOCSEU_SavingACF");
            await Task.Delay(500);
            kv.SaveToFile(destinationPath, false);
            game.Status = FileSystem.DirectoryExists(destinationGamePath) ? ResourceProvider.GetString("LOCSEU_Ready") :
                ResourceProvider.GetString("LOCSEU_MissingInstallDir");
        }
        private void Refresh()
        {
            foreach (var game in Games)
            {
                if (string.IsNullOrEmpty(game.InstallDir)) return;
                string destinationGamePath = Path.Combine(game.SelectedTarget, "common", game.InstallDir);

                game.Status = FileSystem.DirectoryExists(destinationGamePath) ? ResourceProvider.GetString("LOCSEU_Ready") :
                ResourceProvider.GetString("LOCSEU_MissingInstallDir");
            }
        }
        KeyValue ParseToAcf(App app)
        {
            var kv = new KeyValue("AppState")
            {
                Children =
                {
                    new KeyValue("appid") { Value = app.Appid.ToString() ?? "0" },
                    new KeyValue("universe") { Value = "1" },
                    new KeyValue("LauncherPath") { Value = Steam.GetSteamExecutable() },
                    new KeyValue("name") { Value = app.Name },
                    new KeyValue("StateFlags") { Value = "4" },
                    new KeyValue("installdir") { Value = app.Installdir },
                    new KeyValue("LastUpdated") { Value = "0" },
                    new KeyValue("LastPlayed") { Value = "0" },
                    new KeyValue("SizeOnDisk") { Value = app.InstallSize },
                    new KeyValue("StagingSize") { Value = "0" },
                    new KeyValue("buildid") { Value = app.BuildID?.ToString() ?? "0" },
                    new KeyValue("LastOwner") { Value = "0" },
                    new KeyValue("UpdateResult") { Value = "0" },
                    new KeyValue("BytesToDownload") { Value = "0" },
                    new KeyValue("BytesDownloaded") { Value = "0" },
                    new KeyValue("BytesToStage") { Value = "0" },
                    new KeyValue("BytesStaged") { Value = "0" },
                    new KeyValue("TargetBuildID") { Value = "0" },
                    new KeyValue("AutoUpdateBehavior") { Value = "0" },
                    new KeyValue("AllowOtherDownloadsWhileRunning") { Value = "0" },
                    new KeyValue("ScheduledAutoUpdate") { Value = "0" },
                    new KeyValue("StagingFolder") { Value = "0" }
                }
            };

            var nowUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            kv["LastUpdated"].Value = nowUnixTime;
            kv["LastPlayed"].Value = nowUnixTime;

            if (!string.IsNullOrEmpty(app.InstallSize))
            {
                kv["SizeOnDisk"].Value = app.InstallSize;
            }
            if (app.Depots.Any(x => !string.IsNullOrEmpty(x.Value.Manifest)))
            {
                kv.Children.Add(new KeyValue("InstalledDepots"));
                foreach (var installedDepots in app.Depots.Where(x => !string.IsNullOrEmpty(x.Value.Manifest)))
                {
                    kv["InstalledDepots"].Children.Add(new KeyValue(installedDepots.Key.ToString()));
                    kv["InstalledDepots"][installedDepots.Key.ToString()].Children
                    .Add(new KeyValue("manifest", installedDepots.Value.Manifest));
                    kv["InstalledDepots"][installedDepots.Key.ToString()].Children
                    .Add(new KeyValue("size", installedDepots.Value.Size));
                    if (installedDepots.Value.DlcAppID != 0)
                    {
                        kv["InstalledDepots"][installedDepots.Key.ToString()].Children
                    .Add(new KeyValue("dlcappid", installedDepots.Value.DlcAppID.ToString()));
                    }
                }
            }
            if (app.Depots.Any(x => x.Value.SharedInstall))
            {
                kv.Children.Add(new KeyValue("SharedDepots"));
                foreach (var sharedDepots in app.Depots.Where(x => x.Value.SharedInstall))
                {
                    kv["SharedDepots"].Children.Add(new KeyValue(sharedDepots.Key.ToString(), sharedDepots.Value.DepotFromApp.ToString()));
                }
            }
            return kv;
        }
        public async Task GenerateAcfAsync()
        {
            if (!Games.Any()) return;

            await Task.Run(() => SteamUtilities.GetApp(Games.Select(x => x.AppID).Distinct(), steam));
        }
        public RelayCommand<Acf> SearchAppid
        {
            get => new RelayCommand<Acf>((obj) =>
            {
                var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowMinimizeButton = false
                });
                var viewModel = new AcfGeneratorAppidFinderViewModels(PlayniteApi, obj, () => window.Close());

                window.Height = 400;
                window.Width = 780;
                window.Content = new AcfGeneratorAppidFinderView();
                window.DataContext = viewModel;
                window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                window.ShowDialog();
            });
        }
    }
}
