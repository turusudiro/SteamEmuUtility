using GoldbergCommon.Configs;
using System.Collections.Generic;


namespace GoldbergCommon.Models
{

    public partial class ConfigsEmu
    {
        private string path;
        private const string SectionMain = "Main";
        public ConfigsEmu(string path)
        {
            this.path = path;
        }
        public string Architecture
        {
            get { return (string)ConfigsCommon.GetValue(path, SectionMain, "Architecture", ""); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionMain, "Architecture");
            }
        }
        public bool RunAsAdmin
        {
            get { return (bool)ConfigsCommon.GetValue(path, SectionMain, "RunAsAdmin", false); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionMain, "RunAsAdmin");
            }
        }
    }

    public partial class ConfigsColdClientLoader
    {
        private string path;
        private const string SectionInjection = "Injection";
        private const string SectionSteamClient = "SteamClient";
        public ConfigsColdClientLoader(string path)
        {
            this.path = path;
        }
        public string Exe
        {
            get { return (string)ConfigsCommon.GetValue(path, SectionSteamClient, "Exe", ""); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionSteamClient, "Exe");
            }
        }
        public string ExeRunDir
        {
            //get { return (string)ConfigsCommon.GetValue(path, SectionSteamClient, "ExeRunDir", ""); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionSteamClient, "ExeRunDir");
            }
        }
        public string ExeCommandLine
        {
            get { return (string)ConfigsCommon.GetValue(path, SectionSteamClient, "ExeCommandLine", string.Empty); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionSteamClient, "ExeCommandLine");
            }
        }
        public string AppId
        {
            get { return (string)ConfigsCommon.GetValue(path, SectionSteamClient, "AppId", ""); }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionSteamClient, "AppId");
            }
        }
        public string SteamClientDll
        {
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionSteamClient, "SteamClientDll");
            }
        }
        public string SteamClient64Dll
        {
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionSteamClient, "SteamClient64Dll");
            }
        }
        public bool PatchSteamStub
        {
            get
            {
                string KeyValue = (string)ConfigsCommon.GetValue(path, SectionInjection, "DllsToInjectFolder", string.Empty);
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
                    ConfigsCommon.SerializeConfigs("extra_dlls", path, SectionInjection, "DllsToInjectFolder");
                }
                else { ConfigsCommon.SerializeConfigs("", path, SectionInjection, "DllsToInjectFolder"); }
            }
        }
        public bool IgnoreLoaderArchDifference
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionInjection, "IgnoreLoaderArchDifference", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionInjection, "IgnoreLoaderArchDifference");
            }
        }
    }

    public partial class ConfigsApp
    {
        private string path;
        public ConfigsApp(string path)
        {
            this.path = path;
        }
        public string buildid
        {
            get
            {
                string Value = ConfigsCommon.GetValue(path, "app::general", "build_id");
                return Value?.ToString() ?? string.Empty;
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, "app::general", "build_id");
            }
        }
        public bool UnlockAll
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, "app::dlcs", "unlock_all", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, "app::dlcs", "unlock_all");
            }
        }
        private Dictionary<string, string> _dlc;
        public Dictionary<string, string> DLC
        {
            get => _dlc;
            set
            {
                _dlc = value;
                foreach (var dlc in DLC)
                {
                    ConfigsCommon.SerializeConfigs(dlc.Value, path, "app::dlcs", dlc.Key);
                }
            }
        }
    }

    public partial class ConfigsMain
    {
        private string SectionConnectivity = "main::connectivity";
        private string path;
        public ConfigsMain()
        {
            path = Goldberg.ConfigsMainIniPath;
        }
        public ConfigsMain(string path)
        {
            this.path = path;
        }
        public bool EnableAccountAvatar
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, "main::general", "enable_account_avatar", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, "main::general", "enable_account_avatar");
            }
        }
        public string Listen_Port
        {
            get
            {
                return (string)ConfigsCommon.GetValue(path, SectionConnectivity, "listen_port", 47584);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionConnectivity, "listen_port");
            }
        }
        public bool DisableLANOnly
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionConnectivity, "disable_lan_only", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionConnectivity, "disable_lan_only");
            }
        }
        public bool DisableNetworking
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionConnectivity, "disable_networking", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionConnectivity, "disable_networking");
            }
        }
        public bool OfflineModeSteam
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionConnectivity, "offline", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionConnectivity, "offline");
            }
        }
    }
    public partial class ConfigsOverlay
    {
        private string SectionGeneral = "overlay::general";
        private string path;
        public ConfigsOverlay(string path)
        {
            this.path = path;
        }
        public bool EnableOverlay
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionGeneral, "enable_experimental_overlay", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "enable_experimental_overlay");
            }
        }
        public bool DisableOverlayAchievement
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionGeneral, "disable_achievement_notification", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "disable_achievement_notification");
            }
        }
        public bool DisableOverlayFriend
        {
            get
            {
                return (bool)ConfigsCommon.GetValue(path, SectionGeneral, "disable_friend_notification", false);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "disable_friend_notification");
            }
        }
        public string DelayHookInSec
        {
            get
            {
                return (string)ConfigsCommon.GetValue(path, SectionGeneral, "hook_delay_sec", 0);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "hook_delay_sec");
            }
        }
    }

    public partial class ConfigsUser
    {
        private string SectionGeneral = "user::general";
        private string path = Goldberg.ConfigsUserIniPath;
        public string AccountName
        {
            get
            {
                return (string)ConfigsCommon.GetValue(path, SectionGeneral, "account_name", "gse orca");
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "account_name");
            }
        }
        public string ID
        {
            get
            {
                return (string)ConfigsCommon.GetValue(path, SectionGeneral, "account_steamid", 76561197960287930);
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "account_steamid");
            }
        }
        public string Language
        {
            get
            {
                return (string)ConfigsCommon.GetValue(path, SectionGeneral, "language", "english");
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "language");
            }
        }
        public string IP
        {
            get
            {
                return (string)ConfigsCommon.GetValue(path, SectionGeneral, "ip_country", "US");
            }
            set
            {
                ConfigsCommon.SerializeConfigs(value, path, SectionGeneral, "ip_country");
            }
        }

    }
}
