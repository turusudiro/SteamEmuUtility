using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GoldbergCommon;
using GoldbergCommon.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginsCommon;
using SteamCommon;


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
        public RelayCommand<object> OpenSettingsPath
        {
            get => new RelayCommand<object>((a) =>
            {
                var appid = (a as GoldbergGame)?.AppID;
                if (FileSystem.DirectoryExists(Goldberg.GameSettingsPath(appid)))
                {
                    ProcessCommon.ProcessUtilities.StartProcess(Goldberg.GameSettingsPath(appid));
                }
            });
        }
        public RelayCommand<object> GenerateGames
        {
            get => new RelayCommand<object>((a) =>
            {
                var Games = (a as IList)?.Cast<GoldbergGame>().ToList();
                if (Games.Count == 0)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Please choose atleast 1 game.");
                    return;
                }
                GoldbergGenerator.GenerateGoldbergConfig(Games, PlayniteApi, settings.SteamWebApi);
                Games.ForEach(game =>
                {
                    string steamsettingspath = Goldberg.GameSteamSettingPath(game.AppID);
                    string settingspath = Goldberg.GameSettingsPath(game.AppID);
                    var x = GoldbergGames.FirstOrDefault(g => g.Equals(game));
                    x.SettingsExists = FileSystem.DirectoryExists(settingspath);
                    x.GoldbergExists = GoldbergTasks.IsGoldbergExists(game.AppID);
                });
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
