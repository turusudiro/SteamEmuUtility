using DownloaderCommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using ProcessCommon;
using SteamCommon;
using SteamEmuUtility.Common.Goldberg.Models;
using SteamEmuUtility.Common.Serialization;
using SteamEmuUtility.ViewModels.Goldberg.Dialogs;
using SteamEmuUtility.Views.Goldberg.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SteamEmuUtility.ViewModels
{
    public class GoldbergModManagerViewModels : ObservableObject
    {
        private readonly IPlayniteAPI PlayniteApi;
        private static readonly ILogger logger = LogManager.GetLogger();


        private readonly SteamEmuUtility plugin;
        private readonly Game game;
        private readonly string steamSettingsPath;
        private readonly string modPath;
        private readonly string jsonPath;
        private bool _canExecuteOpenModDir;
        private bool _modsJsonExists;
        private HashSet<string> modsJson;
        private HashSet<string> availableModsDir;
        private Dictionary<string, GoldbergModViewModel> modsKv;


        public bool CanExecuteOpenModDir { get => _canExecuteOpenModDir; set => SetValue(ref _canExecuteOpenModDir, value); }
        public bool ModsJsonExists { get => _modsJsonExists; set => SetValue(ref _modsJsonExists, value); }


        public Action RequestClose { get; set; }
        public ObservableCollection<GoldbergModViewModel> Mods { get; set; }
        public ICollectionView ModsView { get; }

        public GoldbergModManagerViewModels(SteamEmuUtility plugin, Game game)
        {
            PlayniteApi = plugin.PlayniteApi;
            this.plugin = plugin;
            this.game = game;
            LogManager.GetLogger();

            AddToJsonCommand = new RelayCommand<GoldbergModViewModel>(AddToJson);
            AddModCommand = new RelayCommand(AddMod);
            ScanCommand = new RelayCommand(Scan);
            RefreshCommand = new RelayCommand(Refresh);
            OpenModDirCommand = new RelayCommand(() => OpenPath(modPath));
            ApplyCommand = new RelayCommand(Apply);
            CancelCommand = new RelayCommand(() => RequestClose?.Invoke());
            OpenModPathCommand = new RelayCommand<string>(OpenPath);
            EditCommand = new RelayCommand<GoldbergModViewModel>(Edit);
            RemoveCommand = new RelayCommand<GoldbergModViewModel>(Remove);
            Mods = new ObservableCollection<GoldbergModViewModel>();
            ModsView = CollectionViewSource.GetDefaultView(Mods);
            ModsView.SortDescriptions.Add(new SortDescription("ID", ListSortDirection.Ascending));

            steamSettingsPath = Path.Combine(plugin.GetPluginUserDataPath(), "GamesInfo", game.GameId, "steam_settings");
            modPath = Path.Combine(steamSettingsPath, "mods");
            jsonPath = Path.Combine(steamSettingsPath, "mods.json");

            CanExecuteOpenModDir = FileSystem.DirectoryExists(modPath);
            ModsJsonExists = FileSystem.FileExists(jsonPath);
            modsJson = new HashSet<string>();
            availableModsDir = new HashSet<string>();

            Initialize();
        }
        void Initialize()
        {
            Dictionary<string, GoldbergMod> result = null;
            try
            {
                using (var file = File.OpenText(jsonPath))
                using (var reader = new JsonTextReader(file))
                {
                    var serializer = new JsonSerializer();
                    serializer.Converters.Add(new GoldbergModDictionaryConverter());
                    result = serializer.Deserialize<Dictionary<string, GoldbergMod>>(reader);
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
            }
            modsKv = result != null
                ? result.ToDictionary(x => x.Key, x => new GoldbergModViewModel(x.Value))
                : new Dictionary<string, GoldbergModViewModel>();

            foreach (var mod in modsKv.Values)
            {
                mod.IsEnabled = true;
                mod.InModsJson = true;
                mod.CanCreateSymlink = true;
                modsJson.Add(mod.ID);
                Mods.Add(mod);
            }

            var installedMods = FileSystem.GetDirectories(Path.Combine(steamSettingsPath, "mods"));

            foreach (var dir in installedMods)
            {
                string id = dir.Name;

                availableModsDir.Add(id);

                if (!modsKv.TryGetValue(id, out var mod))
                {
                    mod = new GoldbergModViewModel()
                    {
                        ID = id,
                        IsEnabled = !ModsJsonExists,
                        ModDirExists = true
                    };
                    modsKv[id] = mod;
                    Mods.Add(mod);
                }
                mod.ModPath = dir.FullName;
                mod.ModDirExists = true;
                mod.CanCreateSymlink = false;
            }
        }
        public RelayCommand<GoldbergModViewModel> AddToJsonCommand { get; }
        public RelayCommand AddModCommand { get; }
        public RelayCommand ScanCommand { get; }
        public RelayCommand RefreshCommand { get; }
        public RelayCommand OpenModDirCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand<string> OpenModPathCommand { get; }
        public RelayCommand<GoldbergModViewModel> EditCommand { get; }
        public RelayCommand<GoldbergModViewModel> RemoveCommand { get; }
        void AddToJson(GoldbergModViewModel mod)
        {
            if (!ModsJsonExists)
            {
                ModsJsonExists = true;
                foreach (var m in Mods)
                {
                    m.IsEnabled = false;
                }
            }
            mod.IsEnabled = true;
            mod.InModsJson = true;
        }
        void AddMod()
        {
            void OnApply(GoldbergModViewModel m)
            {
                var mod = Mods.FirstOrDefault(x => x.ID == m.ID);
                if (mod == null)
                {
                    m.IsEnabled = true;
                    m.InModsJson = true;
                    Mods.Add(m);
                }
                else
                {
                    if (PlayniteApi.Dialogs.ShowMessage($"{ResourceProvider.GetString("LOCItemAlreadyExists")} {ResourceProvider.GetString("LOCSEU_Update")}?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        var index = Mods.IndexOf(mod);
                        if (index != -1)
                        {
                            Mods[index] = m;
                        }
                    }
                }
            }

            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            var viewModel = new GoldbergModEditorDialogViewModel(new GoldbergModViewModel(), plugin, window)
            {
                OnApply = OnApply
            };
            window.Height = 440;
            window.Width = 780;
            window.Title = "Goldberg Mod Editor";
            window.Content = new GoldbergModEditorDialogView();
            window.DataContext = viewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.ShowDialog();
        }
        void Scan()
        {
            var installedWorkshop = Steam.GetInstalledWorkshop(game.GameId);

            if (installedWorkshop.Count == 0)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GBScannerNoWorkshop"));
                return;
            }

            HashSet<string> addedId = new HashSet<string>();

            var importedId = Mods.Select(x => x.ID).ToHashSet();

            var modsVM = new ObservableCollection<GoldbergModViewModel>();

            foreach (var workhop in installedWorkshop)
            {
                var id = workhop.Key;

                if (!addedId.Contains(id))
                {
                    addedId.Add(id);
                    var mod = new GoldbergModViewModel()
                    {
                        ID = id,
                        IsEnabled = true,
                        CreateSymlink = true,
                        CanImport = !importedId.Contains(id),
                        TimeUpdated = workhop.Value.TimeUpdated,
                        PrimaryFileSize = workhop.Value.Size
                    };
                    mod.ModDirExists = availableModsDir.Contains(id);
                    modsVM.Add(mod);
                }
            }

            void ApplyHandler(IEnumerable<GoldbergModViewModel> mods)
            {
                RefreshModsInfo(mods.Where(x => x.CanImport && x.IsEnabled));
                foreach (var scannedMod in mods)
                {
                    if (scannedMod.CanImport && scannedMod.IsEnabled)
                    {
                        scannedMod.InModsJson = true;
                        modsKv[scannedMod.ID] = scannedMod;
                        Mods.Add(scannedMod);
                    }
                    if (scannedMod.CreateSymlink)
                    {
                        if (!modsKv.TryGetValue(scannedMod.ID, out var mod))
                        {
                            mod = scannedMod;
                        }
                        var workshopPath = Steam.GetWorkshopPath(game.GameId);

                        var targetPath = Path.Combine(workshopPath, mod.ID);
                        if (!FileSystem.DirectoryExists(targetPath))
                        {
                            return;
                        }

                        var linkPath = Path.Combine(modPath, mod.ID);

                        try
                        {
                            if (FileSystem.CreateSymbolicLink(linkPath, targetPath))
                            {
                                mod.ModDirExists = true;
                                mod.ModPath = targetPath;
                            }
                        }
                        catch (Exception ex)
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
                        }
                    }
                }
                CanExecuteOpenModDir = FileSystem.DirectoryExists(modPath);
            }

            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            var viewModel = new WorkshopImportDialogViewModel()
            {
                Mods = modsVM,
                RequestClose = () => { window.Close(); },
                ApplyHandler = ApplyHandler
            };
            window.Height = 440;
            window.Width = 450;
            window.Content = new WorkshopImportDialogView();
            window.DataContext = viewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.ShowDialog();
        }
        void Refresh()
        {
            var mods = Mods.Where(x => x.IsEnabled && x.InModsJson);
            var count = mods.Count();
            if (count == 0)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GBRefreshWorkshopError"));
            }
            else if (PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_GBRefreshEnabledWorkshop"), count), "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                RefreshModsInfo(Mods.Where(x => x.IsEnabled));
            }
        }
        void Apply()
        {
            try
            {
                var enabledMods = Mods.Where(x => x.IsEnabled && x.InModsJson).ToList();

                if (enabledMods.Count == 0 && !ModsJsonExists)
                {
                    return;
                }
                var data = enabledMods.ToDictionary(x => x.ID, x => new GoldbergMod(x));
                var settings = new JsonSerializerSettings()
                {
                    ContractResolver = FilterEmptyStringsResolver.Instance,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    Formatting = Formatting.Indented
                };
                var json = JsonConvert.SerializeObject(data, settings);
                FileSystem.WriteStringToFile(jsonPath, json);
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
            finally
            {
                RequestClose?.Invoke();
            }
        }
        void OpenPath(string path)
        {
            try
            {
                ProcessUtilities.StartProcess(path);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }
        void Edit(GoldbergModViewModel mod)
        {
            void OnApply(GoldbergModViewModel m)
            {
                var index = Mods.IndexOf(mod);
                Mods[index] = m;
            }

            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            var viewModel = new GoldbergModEditorDialogViewModel(mod, plugin, window)
            {
                OnApply = OnApply
            };
            window.Height = 440;
            window.Width = 780;
            window.Content = new GoldbergModEditorDialogView();
            window.DataContext = viewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.ShowDialog();
        }
        void Remove(GoldbergModViewModel mod)
        {
            if (!mod.ModDirExists)
            {
                Mods.Remove(mod);
            }
            else
            {
                mod.IsEnabled = false;
                mod.InModsJson = false;
            }
        }
        void RefreshModsInfo(IEnumerable<GoldbergModViewModel> mods)
        {
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Downloading Info", true);
            progressOptions.IsIndeterminate = false;
            void OnProgressRefresh(GlobalProgressActionArgs args)
            {
                args.CurrentProgressValue = 0;
                args.ProgressMaxValue = 100;

                string url = @"https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

                void UpdateProgress(int progress)
                {
                    args.Text = $"Downloading {progress}%";
                    args.CurrentProgressValue = progress;
                }

                try
                {
                    int batchSize = 100;
                    var modChunks = mods.Select((x, i) => new { Index = i, Value = x })
                                        .GroupBy(x => x.Index / batchSize)
                                        .Select(x => x.Select(v => v.Value).ToList());

                    foreach (var chunk in modChunks)
                    {
                        var formData = new Dictionary<string, string>();
                        formData.Add("itemcount", chunk.Count.ToString());

                        for (int i = 0; i < chunk.Count; i++)
                        {
                            formData.Add($"publishedfileids[{i}]", chunk[i].ID);
                        }

                        var response = HttpDownloader.DownloadStringPost(url, formData, UpdateProgress, args.CancelToken);
                        var json = Serialization.FromJson<JObject>(response);
                        var details = json["response"]["publishedfiledetails"];

                        if (details == null) continue;

                        foreach (var data in details)
                        {
                            string publishedFileId = data["publishedfileid"]?.Value<string>();
                            var mod = chunk.FirstOrDefault(m => m.ID == publishedFileId);

                            if (mod != null && data["result"].Value<int>() == 1)
                            {
                                mod.Title = data["title"]?.Value<string>();
                                mod.Description = data["description"]?.Value<string>();
                                mod.SteamIDOwner = data["creator"]?.Value<ulong>();
                                mod.TimeCreated = data["time_created"]?.Value<int>();

                                // handle when failed parse for some reason
                                // use the available data from appworkshop.acf from scan feature
                                // the appworkshop.acf has some basic data like time updated and size
                                mod.TimeUpdated = data["time_updateda"]?.Value<int>() ?? mod.TimeUpdated;
                                mod.PrimaryFileSize = data["file_size"]?.Value<int>() ?? mod.PrimaryFileSize;

                                if (data["tags"] != null)
                                {
                                    List<string> tags = new List<string>();
                                    foreach (var tag in data["tags"])
                                    {
                                        tags.Add(tag["tag"]?.Value<string>());
                                    }
                                    mod.Tags = string.Join(",", tags);
                                }

                                mod.PreviewURL = data["preview_urla"]?.Value<string>();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
                }
            }
            PlayniteApi.Dialogs.ActivateGlobalProgress(OnProgressRefresh, progressOptions);
        }
    }
}
