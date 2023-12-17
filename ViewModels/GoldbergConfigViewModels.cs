using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            var caller = name;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public GoldbergConfigViewModel(IPlayniteAPI api)
        {
            PlayniteApi = api;
            SelectedSteamGames = PlayniteApi.MainView.SelectedGames.Where(g => g.IsInstalled && SteamUtilities.IsGameSteamGame(g)).OrderBy(x => x.Name).ToList();
            GoldbergGames = GoldbergTasks.ConvertGames(SelectedSteamGames, api);
        }
        private List<GoldbergGames> goldberggames;
        public List<GoldbergGames> GoldbergGames
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
                var Game = (a as GoldbergGames)?.Game;
                ProcessCommon.ProcessUtilities.StartProcess(Goldberg.GameSettingsPath(Game));
            });
        }
        public RelayCommand<object> GenerateGames
        {
            get => new RelayCommand<object>((a) =>
            {
                var Games = (a as IList)?.Cast<GoldbergGames>().ToList();
                if (Games.Count == 0)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Please choose atleast 1 game.");
                    return;
                }
                GoldbergGenerator.GenerateGoldbergConfig(Games, PlayniteApi);
                GoldbergGames.ForEach(x =>
                {
                    string settingspath = Goldberg.GameSteamSettingPath(x.AppID);
                    x.DLCExists = FileSystem.FileExists(Path.Combine(settingspath, "DLC.txt"));
                    x.AchievementsExists = FileSystem.FileExists(Path.Combine(settingspath, "achievements.json"));
                    x.SettingsExists = FileSystem.DirectoryExists(Goldberg.GameSettingsPath(x.AppID));
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
