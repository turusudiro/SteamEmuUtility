using System.ComponentModel;
using Playnite.SDK.Models;

namespace GoldbergCommon.Models
{
    public partial class GoldbergGames
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public bool DisableOverlay { get; set; }
        public bool SettingsExists { get; set; }
        public bool AchievementsExists { get; set; }
        public bool DLCExists { get; set; }
        public bool ReconfigureAchievements { get; set; }
        public bool ReconfigureDLC { get; set; }
        public bool RunAsAdmin { get; set; }
        public bool OfflineModeSteam { get; set; }
        public bool DisableNetworking { get; set; }
        public bool DisableLANOnly { get; set; }
        public string CustomBroadcastAddress { get; set; }
        public bool CustomBroadcast { get; set; }
        public bool DisableOverlaylocalsave { get; set; }
        public bool DisableOverlayFriend { get; set; }
        public bool DisableOverlayAchievement { get; set; }
        public string Name { get; set; }
        public string CoverImage { get; set; }
        public Game Game { get; set; }
    }
}
