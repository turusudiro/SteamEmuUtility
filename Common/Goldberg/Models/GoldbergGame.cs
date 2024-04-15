using SteamCommon.Models;
using System.Collections.Generic;

namespace GoldbergCommon.Models
{
    public partial class GoldbergGame : ObservableObject
    {
        public AppIdInfo AppInfo { get; set; }
        private bool _goldbergexists;
        public bool GoldbergExists { get => _goldbergexists; set { _goldbergexists = value; OnPropertyChanged(); } }
        private bool _reconfiguregolberg;
        public bool ReconfigureGoldberg { get => _reconfiguregolberg; set { _reconfiguregolberg = value; OnPropertyChanged(); } }
        private bool _enableoverlay;
        public bool EnableOverlay { get => _enableoverlay; set { _enableoverlay = value; OnPropertyChanged(); } }
        private bool _delayhook;
        public bool DelayHook { get => _delayhook; set { _delayhook = value; OnPropertyChanged(); } }
        public string DelayHookInSec { get; set; }

        private bool _settingsexists;
        public bool SettingsExists { get => _settingsexists; set { _settingsexists = value; OnPropertyChanged(); } }
        public bool PatchSteamStub { get; set; }
        public int Architecture { get; set; }
        public bool ReconfigureAchievements { get; set; }
        public bool ReconfigureDLC { get; set; }
        public bool ReconfigureController { get; set; }
        public bool ReconfigureColdClient { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool OfflineModeSteam { get; set; }
        public bool DisableNetworking { get; set; }
        public bool DisableLANOnly { get; set; }
        public string CustomBroadcastAddress { get; set; }
        private bool _custombroadcast { get; set; }
        public bool CustomBroadcast { get => _custombroadcast; set { _custombroadcast = value; OnPropertyChanged(); } }
        public bool DisableOverlayFriend { get; set; }
        public bool DisableOverlayAchievement { get; set; }
        public string Name { get; set; }
        public string CoverImage { get; set; }
        public string AppID { get; set; }
        public string InstallDirectory { get; set; }
    }
}
