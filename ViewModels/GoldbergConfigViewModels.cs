using GoldbergCommon;
using GoldbergCommon.Configs;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using PluginsCommon;
using SteamCommon;
using SteamCommon.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;


namespace SteamEmuUtility.ViewModels
{
    class GoldbergConfigViewModel : ObservableObject, IDisposable
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

        ~GoldbergConfigViewModel()
        {
            Dispose(false);
        }

        private readonly IPlayniteAPI PlayniteApi;
        private readonly SteamEmuUtilitySettings settings;
        private SteamService steam;
        private GlobalProgressActionArgs progress;
        private readonly string pluginPath;
        private readonly string steamID;
        private readonly string steamID3;
        private readonly string goldbergPath;
        private readonly string steamDir;
        public int GamesCount => goldberggames?.Count() ?? 1;

        private IEnumerable<GoldbergGame> goldberggames;
        public IEnumerable<GoldbergGame> GoldbergGames
        {
            get => goldberggames;
            set
            {
                goldberggames = value;
                OnPropertyChanged();
            }
        }
        public GoldbergConfigViewModel(IPlayniteAPI api, SteamEmuUtilitySettings settings, string pluginPath)
        {
            PlayniteApi = api;
            this.settings = settings;
            this.pluginPath = pluginPath;
            goldbergPath = Goldberg.GetGoldbergAppData();
            string goldbergSettingsPath = Path.Combine(goldbergPath, "settings");

            steamDir = Steam.GetSteamDirectory();
            steamID = (string)ConfigsCommon.GetValue(Path.Combine(goldbergSettingsPath, "configs.user.ini"), "user::general", "account_steamid", 76561197960287930);
            steamID3 = Steam.GetUserSteamID3(steamID).ToString();

            var selectedSteamGames = PlayniteApi.MainView.SelectedGames.Where(g => g.IsInstalled && Steam.IsGameSteamGame(g)).OrderBy(x => x.Name).ToList();
            GoldbergGames = GoldbergTasks.ConvertGames(pluginPath, selectedSteamGames);
            ProcessGames(goldbergPath);
        }
        private void ProcessGames(string goldbergPath)
        {
            GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility", true);
            progressOptions.IsIndeterminate = false;

            PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
            {
                progress.ProgressMaxValue = GamesCount;

                GoldbergGames.ForEach(x =>
                {
                    progress.Text = string.Format(ResourceProvider.GetString("LOCSEU_Processing"), x.Name);

                    string steamUserDataGameDir = Path.Combine(steamDir, "userdata", steamID3.ToString(), x.Appid);

                    if (!ConfigsCommon.ContainsKey(x.ConfigsEmu.IniPath, "Main", "CloudSaveAvailable"))
                    {
                        if (progress.CancelToken.IsCancellationRequested)
                        {
                            return;
                        }
                        CheckSteam();
                        x.AppInfo = SteamUtilities.GetApp(uint.Parse(x.Appid), steam);

                        x.ConfigsEmu.CloudSaveAvailable = x.AppInfo.CloudSaveAvailable && !x.AppInfo.CloudSaveConfigured;

                        progress.IsIndeterminate = false;
                    }

                    if (FileSystem.FileExists(Path.Combine(x.gameSteamSettingsPath, "branches.json")))
                    {
                        x.Branches = Serialization.FromJsonFile<IEnumerable<Branches>>(Path.Combine(x.gameSteamSettingsPath, "branches.json")).Select(b => b.Name);

                        x.SelectedBranch = x.Branches.Any(b => b.Equals(x.ConfigsApp.BranchName)) ? x.ConfigsApp.BranchName : x.Branches.FirstOrDefault();
                    }

                    x.EnableCloudSave = IsCloudSaveEnabled(x.Appid);

                    x.IsCloudSaveAvailable = !FileSystem.IsDirectoryEmpty(steamUserDataGameDir) && x.ConfigsEmu.CloudSaveAvailable;

                    progress.CurrentProgressValue++;
                });

            }, progressOptions);
        }
        private void CallbackHandler(object obj)
        {
            if (progress != null)
            {
                if (obj is int count)
                {
                    progress.CurrentProgressValue = count;
                }
                else if (obj is string text)
                {
                    progress.Text = text;
                }
            }
        }
        private bool IsCloudSaveEnabled(string appid)
        {
            string goldbergGameDataPath = Path.Combine(goldbergPath, appid);

            string steamUserDataGamePath = Path.Combine(steamDir, "userdata", steamID3.ToString(), appid);

            try
            {
                var userdatappid = Directory.GetDirectories(steamUserDataGamePath, "*", SearchOption.TopDirectoryOnly);
                if (!userdatappid.Any())
                {
                    return false;
                }
                foreach (var dir in userdatappid)
                {
                    string rootdirectory = Path.GetFileName(dir);
                    string goldbergtarget = Path.Combine(goldbergGameDataPath, rootdirectory);
                    if (!FileSystem.DirectoryExists(goldbergtarget))
                    {
                        return false;
                    }
                    if (!FileSystem.IsSymbolicLink(goldbergtarget))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch { return false; }
        }
        public RelayCommand<object> GenerateInfo
        {
            get => new RelayCommand<object>((a) =>
            {
                var game = a as GoldbergGame;

                if (!InternetCommon.Internet.IsInternetAvailable())
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_ConnectionUnavailable"));
                    return;
                }

                CheckSteam();

                string gameSettingsPath = Path.Combine(pluginPath, "GamesInfo", game.Appid);
                string gameSteamSettingsPath = Path.Combine(gameSettingsPath, "steam_settings");

                GoldbergGenerator.GenerateInfo(game, settings.SteamWebApi, PlayniteApi, steam);
            });
        }
        public RelayCommand<GoldbergGame> UpdateBranch
        {
            get => new RelayCommand<GoldbergGame>((a) =>
            {
                a.ConfigsApp.BranchName = a.SelectedBranch;
            });
        }
        private void CheckSteam()
        {
            if (steam == null)
            {
                steam = new SteamService(CallbackHandler);
            }
        }
        public RelayCommand<object> OpenSettingsPath
        {
            get => new RelayCommand<object>((a) =>
            {
                var appid = (a as GoldbergGame)?.Appid;
                string path = Path.Combine(pluginPath, "GamesInfo", appid);
                if (!FileSystem.DirectoryExists(path))
                {
                    FileSystem.CreateDirectory(path);
                }
                ProcessCommon.ProcessUtilities.StartProcess(path);
            });
        }
        public RelayCommand<GoldbergGame> SetCloudSave
        {
            get => new RelayCommand<GoldbergGame>((game) =>
            {
                GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility", false);
                progressOptions.IsIndeterminate = true;
                PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
                {
                    string goldbergPath = Goldberg.GetGoldbergAppData();
                    string steamDir = Steam.GetSteamDirectory();
                    string gamesteamSettingsDir = Path.Combine(pluginPath, "GamesInfo", game.Appid);
                    string goldbergGameDataPath = Path.Combine(goldbergPath, game.Appid);

                    string Appid = game.Appid;

                    var steamID3 = Steam.GetUserSteamID3(steamID);

                    string steamUserDataGamePath = Path.Combine(steamDir, "UserData", steamID3.ToString(), Appid);

                    if (!FileSystem.DirectoryExists(goldbergGameDataPath))
                    {
                        FileSystem.CreateDirectory(goldbergGameDataPath);
                    }

                    var savesPath = Directory.GetDirectories(goldbergGameDataPath, "*", SearchOption.TopDirectoryOnly);

                    if (!game.EnableCloudSave)
                    {
                        foreach (var dir in savesPath)
                        {
                            if (FileSystem.IsSymbolicLink(dir))
                            {
                                FileSystem.DeleteDirectory(dir);
                            }
                        }
                        return;
                    }
                    var userDataGamePath = Directory.GetDirectories(steamUserDataGamePath, "*", SearchOption.TopDirectoryOnly);
                    foreach (var dir in userDataGamePath)
                    {
                        string rootdirectory = Path.GetFileName(dir);
                        string goldbergtarget = Path.Combine(goldbergGameDataPath, rootdirectory);
                        if (FileSystem.DirectoryExists(goldbergtarget))
                        {
                            var dialog = PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_ReplaceAppDataSymbolicWarning"), goldbergtarget, game.Name),
                                "Steam Emu Utility", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                            if (dialog == MessageBoxResult.Yes)
                            {
                                string backupPath = Path.Combine(gamesteamSettingsDir, "Saves", steamID, Path.GetFileName(goldbergtarget));
                                try
                                {
                                    FileSystem.CopyDirectory(goldbergtarget, backupPath);
                                    FileSystem.DeleteDirectory(goldbergtarget);
                                }
                                catch { continue; }
                            }
                            else if (dialog == MessageBoxResult.No)
                            {
                                return;
                            }
                        }
                        if (!FileSystem.CreateSymbolicLink(goldbergtarget, dir))
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_SymbolicDeveloperOffError"));
                            return;
                        }
                    }
                }, progressOptions);
            });
        }
    }
}
