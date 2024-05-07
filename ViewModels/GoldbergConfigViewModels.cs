using GoldbergCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;


namespace SteamEmuUtility.ViewModels
{
    class GoldbergConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly IPlayniteAPI PlayniteApi;
        private readonly SteamEmuUtilitySettings settings;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var caller = name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public GoldbergConfigViewModel(IPlayniteAPI api, SteamEmuUtilitySettings settings)
        {
            PlayniteApi = api;
            this.settings = settings;
            SelectedSteamGames = PlayniteApi.MainView.SelectedGames.Where(g => g.IsInstalled && Steam.IsGameSteamGame(g)).OrderBy(x => x.Name).ToList();
            GoldbergGames = GoldbergTasks.ConvertGames(SelectedSteamGames, api);
        }
        private List<GoldbergGame> goldberggames;
        public List<GoldbergGame> GoldbergGames
        {
            get => goldberggames;
            set
            {
                goldberggames = value;
                OnPropertyChanged();
            }
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
                GoldbergGenerator.GenerateInfo(game, settings.SteamWebApi, PlayniteApi);
            });
        }

        public RelayCommand<object> OpenSettingsPath
        {
            get => new RelayCommand<object>((a) =>
            {
                var appid = (a as GoldbergGame)?.AppID;
                string path = Goldberg.GameSettingsPath(appid);
                if (!FileSystem.DirectoryExists(path))
                {
                    FileSystem.CreateDirectory(path);
                }
                ProcessCommon.ProcessUtilities.StartProcess(Goldberg.GameSettingsPath(appid));
            });
        }
        private List<Game> selectedSteamGames;
        public List<Game> SelectedSteamGames
        {
            get => selectedSteamGames;
            set
            {
                selectedSteamGames = value;
                OnPropertyChanged();
            }
        }
    }
}
