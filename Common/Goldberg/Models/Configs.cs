using GoldbergCommon.Configs;
using System.Collections.Generic;
using System.IO;


namespace GoldbergCommon.Models
{
    public partial class ConfigsEmu
    {
        public string IniPath { get; }

        private const string SectionMain = "Main";
        public ConfigsEmu(string iniDirectory)
        {
            IniPath = Path.Combine(iniDirectory, "configs.emu.ini");
        }
        public string Architecture
        {
            get { return (string)ConfigsCommon.GetValue(IniPath, SectionMain, "Architecture", string.Empty); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionMain, "Architecture");
            }
        }
        public bool RunAsAdmin
        {
            get { return (bool)ConfigsCommon.GetValue(IniPath, SectionMain, "RunAsAdmin", false); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionMain, "RunAsAdmin");
            }
        }
        public bool UnlockOnlySelectedDLC
        {
            get { return (bool)ConfigsCommon.GetValue(IniPath, SectionMain, "UnlockOnlySelectedDLC", false); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionMain, "UnlockOnlySelectedDLC");
            }
        }
        public bool CloudSaveAvailable
        {
            get { return (bool)ConfigsCommon.GetValue(IniPath, SectionMain, "CloudSaveAvailable", false); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionMain, "CloudSaveAvailable");
            }
        }
    }

    public partial class ConfigsColdClientLoader
    {
        public string IniPath { get; }

        private const string SectionInjection = "Injection";
        private const string SectionSteamClient = "SteamClient";
        public ConfigsColdClientLoader(string iniDirectory)
        {
            IniPath = Path.Combine(iniDirectory, "ColdClientLoader.ini");
        }
        public string Exe
        {
            get { return (string)ConfigsCommon.GetValue(IniPath, SectionSteamClient, "Exe", string.Empty); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionSteamClient, "Exe");
            }
        }
        public string ExeRunDir
        {
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionSteamClient, "ExeRunDir");
            }
        }
        public string ExeCommandLine
        {
            get { return (string)ConfigsCommon.GetValue(IniPath, SectionSteamClient, "ExeCommandLine", string.Empty); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionSteamClient, "ExeCommandLine");
            }
        }
        public string Appid
        {
            get { return (string)ConfigsCommon.GetValue(IniPath, SectionSteamClient, "AppId", string.Empty); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionSteamClient, "AppId");
            }
        }
        public string SteamClientDll
        {
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionSteamClient, "SteamClientDll");
            }
        }
        public string SteamClient64Dll
        {
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionSteamClient, "SteamClient64Dll");
            }
        }
        public bool PatchSteamStub
        {
            get
            {
                string KeyValue = (string)ConfigsCommon.GetValue(IniPath, SectionInjection, "DllsToInjectFolder", string.Empty);
                if (KeyValue.Equals("extra_dlls"))
                {
                    return true;
                }
                else { return false; }
            }
            set
            {
                if (value)
                {
                    ConfigsCommon.SerializeConfigs("extra_dlls", IniPath, SectionInjection, "DllsToInjectFolder");
                }
                else { ConfigsCommon.SerializeConfigs(string.Empty, IniPath, SectionInjection, "DllsToInjectFolder"); }
            }
        }
        public bool IgnoreLoaderArchDifference
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionInjection, "IgnoreLoaderArchDifference", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionInjection, "IgnoreLoaderArchDifference");
            }
        }
    }

    public partial class ConfigsApp
    {
        public string IniPath { get; }

        public ConfigsApp(string iniDirectory)
        {
            IniPath = Path.Combine(iniDirectory, "configs.app.ini");
        }
        public string BranchName
        {
            get
            {
                string Value = (string)ConfigsCommon.GetValue(IniPath, "app::general", "branch_name", "public");
                return Value?.ToString() ?? string.Empty;
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, "app::general", "branch_name");
            }
        }
        public bool UnlockAll
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, "app::dlcs", "unlock_all", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, "app::dlcs", "unlock_all");
            }
        }
        public Dictionary<string, string> DLC
        {
            set
            {
                foreach (var dlc in value)
                {
                    ConfigsCommon.SerializeConfigs(dlc.Value, IniPath, "app::dlcs", dlc.Key);
                }
            }
        }
    }

    public partial class ConfigsMain
    {
        public string IniPath { get; }

        private const string SectionConnectivity = "main::connectivity";
        public ConfigsMain(string iniDirectory)
        {
            IniPath = Path.Combine(iniDirectory, "configs.main.ini");
        }
        public bool EnableAccountAvatar
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, "main::general", "enable_account_avatar", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, "main::general", "enable_account_avatar");
            }
        }
        public string Listen_Port
        {
            get
            {
                return (string)ConfigsCommon.GetValue(IniPath, SectionConnectivity, "listen_port", 47584);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionConnectivity, "listen_port");
            }
        }
        public bool DisableLANOnly
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionConnectivity, "disable_lan_only", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionConnectivity, "disable_lan_only");
            }
        }
        public bool DisableNetworking
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionConnectivity, "disable_networking", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionConnectivity, "disable_networking");
            }
        }
        public bool OfflineModeSteam
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionConnectivity, "offline", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionConnectivity, "offline");
            }
        }
    }
    public partial class ConfigsOverlay
    {
        public string IniPath { get; }

        private string SectionGeneral = "overlay::general";
        public ConfigsOverlay(string iniDirectory)
        {
            IniPath = Path.Combine(iniDirectory, "configs.overlay.ini");
        }
        public bool EnableOverlay
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionGeneral, "enable_experimental_overlay", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "enable_experimental_overlay");
            }
        }
        public bool DisableOverlayAchievement
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionGeneral, "disable_achievement_notification", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "disable_achievement_notification");
            }
        }
        public bool DisableOverlayFriend
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(IniPath, SectionGeneral, "disable_friend_notification", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "disable_friend_notification");
            }
        }
        public string DelayHookInSec
        {
            get
            {
                return (string)ConfigsCommon.GetValue(IniPath, SectionGeneral, "hook_delay_sec", 0);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "hook_delay_sec");
            }
        }
    }

    public partial class ConfigsUser
    {
        public string IniPath { get; }

        private string SectionGeneral = "user::general";
        public ConfigsUser(string iniDirectory)
        {
            IniPath = Path.Combine(iniDirectory, "configs.user.ini");
        }
        public string AccountName
        {
            get
            {
                return (string)ConfigsCommon.GetValue(IniPath, SectionGeneral, "account_name", "gse orca");
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "account_name");
            }
        }
        public string ID
        {
            get
            {
                return (string)ConfigsCommon.GetValue(IniPath, SectionGeneral, "account_steamid", 76561197960287930);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "account_steamid");
            }
        }
        public string Language
        {
            get
            {
                return (string)ConfigsCommon.GetValue(IniPath, SectionGeneral, "language", "english");
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "language");
            }
        }
        public string IP
        {
            get
            {
                return (string)ConfigsCommon.GetValue(IniPath, SectionGeneral, "ip_country", "US");
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, IniPath, SectionGeneral, "ip_country");
            }
        }
    }
}
