using System.Collections.Generic;
using SteamCommon.Models;

namespace GoldbergCommon.Models
{
    public partial class GoldbergGame : ObservableObject
    {
        public AppIdInfo AppInfo { get; set; }
        private bool _goldbergexists;
        public bool GoldbergExists { get => _goldbergexists; set { _goldbergexists = value; OnPropertyChanged(); } }
        private bool _reconfiguregolberg;
        public bool ReconfigureGoldberg { get => _reconfiguregolberg; set { _reconfiguregolberg = value; OnPropertyChanged(); } }
        private bool _disableoverlay;
        public bool DisableOverlay { get => _disableoverlay; set { _disableoverlay = value; OnPropertyChanged(); } }

        private bool _settingsexists;
        public bool SettingsExists { get => _settingsexists; set { _settingsexists = value; OnPropertyChanged(); } }
        public bool ReconfigureAchievements { get; set; }
        public bool ReconfigureDLC { get; set; }
        public bool ReconfigureController { get; set; }
        public bool ReconfigureColdClient { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool OfflineModeSteam { get; set; }
        public bool DisableNetworking { get; set; }
        public bool DisableLANOnly { get; set; }
        public string CustomBroadcastAddress { get; set; }
        public bool _custombroadcast { get; set; }
        public bool CustomBroadcast { get => _custombroadcast; set { _custombroadcast = value; OnPropertyChanged(); } }
        public bool DisableOverlaylocalsave { get; set; }
        public bool DisableOverlayFriend { get; set; }
        public bool DisableOverlayAchievement { get; set; }
        public string Name { get; set; }
        public string CoverImage { get; set; }
        public string AppID { get; set; }
        public string InstallDirectory { get; set; }
    }
}
