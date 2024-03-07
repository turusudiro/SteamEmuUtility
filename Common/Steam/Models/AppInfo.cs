using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace SteamCommon.Models
{
    public partial class AppInfo
    {
        [JsonProperty("data")]
        private Dictionary<string, AppIdInfo> _data;
        [JsonIgnore]
        public Dictionary<string, AppIdInfo> Data
        {
            get => _data;
            set
            {
                _data = value;
            }
        }
        [JsonProperty("status")]
        private string _status;
        [JsonIgnore]
        public bool Status
        {
            get
            {
                if (_status == null || !_status.Equals("success"))
                {
                    return false;
                }
                return true;
            }
        }
    }
    public partial class AppIdInfo
    {
        [JsonProperty("appid")]
        private string _appid { get; set; }
        [JsonIgnore]
        public int AppId
        {
            get { return int.TryParse(_appid, out int x) ? x : 0; }
        }
        [JsonProperty("common")]
        private CommonInfo _common;
        [JsonIgnore]
        public CommonInfo Common { get => _common; }
        [JsonProperty("depots")]
        private Depots _depots;
        [JsonIgnore]
        public Depots Depots { get => _depots; }
        [JsonProperty("config")]
        private ConfigInfo _config;
        [JsonIgnore]
        public ConfigInfo Config { get => _config; }
        [JsonProperty("extended")]
        private ExtendedInfo _extended;
        [JsonIgnore]
        public ExtendedInfo Extended { get => _extended; }
    }
    public partial class CommonInfo
    {
        [JsonProperty("name")]
        private string _name;
        public string Name { get => _name; }
        [JsonProperty("type")]
        private string _type;
        public string Type { get => _type; }
        [JsonProperty("controller_support")]
        private string _controllersupport;
        public string ControllerSupport { get => _controllersupport; }
        [JsonProperty("supported_languages")]
        private Dictionary<string, object> _supportedlanguages;
        public Dictionary<string, object> SupportedLanguages { get => _supportedlanguages; }
    }
    public partial class ConfigInfo
    {
        [JsonProperty("launch")]
        private Dictionary<string, LaunchInfo> _launch;
        [JsonIgnore]
        public Dictionary<string, LaunchInfo> Launch { get => _launch; }
        [JsonProperty("steamcontrollerconfigdetails")]
        private Dictionary<string, ControllerConfig> _steamcontrollerconfigdetails;
        [JsonIgnore]
        public Dictionary<string, ControllerConfig> SteamControllerConfigDetails { get => _steamcontrollerconfigdetails; }
        [JsonProperty("steamcontrollertouchconfigdetails")]
        private Dictionary<string, ControllerConfig> _steamcontrollertouchconfigdetails;
        [JsonIgnore]
        public Dictionary<string, ControllerConfig> SteamControllerTouchConfigDetails { get => _steamcontrollertouchconfigdetails; }
    }
    public partial class ControllerConfig
    {
        [JsonProperty("controller_type")]
        private string _controllertype;
        [JsonIgnore]
        public string ControllerType { get => _controllertype; }
        [JsonProperty("enabled_branches")]
        private string _enabledbranches;
        [JsonIgnore]
        public string EnabledBranches { get => _enabledbranches; }
    }

    public partial class LaunchInfo
    {
        [JsonProperty("executable")]
        private string _executable;
        [JsonIgnore]
        public string Executable { get => _executable; }
        [JsonProperty("type")]
        private string _type;
        [JsonIgnore]
        public string Type { get => _type; }
        [JsonProperty("arguments")]
        private string _arguments;
        [JsonIgnore]
        public string Arguments { get => _arguments; }
    }
    public partial class ExtendedInfo
    {
        [JsonProperty("listofdlc")]
        private string _listofdlc;
        [JsonIgnore]
        public List<string> ListOfDLC
        {
            get
            {
                if (_listofdlc == null)
                {
                    return null;
                }
                var dlcs = _listofdlc.Split(',').ToList();
                return dlcs;
            }
        }
    }
    public partial class Depots
    {
        [JsonProperty("baselanguages")]
        private string _baselanguages;
        [JsonIgnore]
        public List<string> baselanguages
        {
            get
            {
                if (_baselanguages == null)
                {
                    return null;
                }
                return _baselanguages.Split(',').Select(str => str.Trim()).ToList();
            }
        }
        [JsonProperty("branches")]
        private BranchesInfo _branches;
        [JsonIgnore]
        public BranchesInfo Branches
        {
            get => _branches;
        }
        [JsonExtensionData]
        private JObject _depotsjobject;
        private Dictionary<string, depotinfo> _depots;
        public Dictionary<string, depotinfo> depots
        {
            get
            {
                if (_depotsjobject == null)
                {
                    return null;
                }
                if (_depots != null)
                {
                    return _depots;
                }
                var dep = new Dictionary<string, depotinfo>();
                foreach (var depot in _depotsjobject)
                {
                    try
                    {
                        dep.Add(depot.Key, depot.Value.ToObject<depotinfo>());
                    }
                    catch { }
                }
                _depots = dep;
                return dep;
            }
            set { _depots = value; }
        }
    }
    public partial class BranchesInfo
    {
        [JsonProperty("public")]
        private PublicInfo _public;
        [JsonIgnore]
        public PublicInfo Public
        {
            get => _public;
        }
        public class PublicInfo
        {
            [JsonProperty("buildid")]
            private string _buildid;
            [JsonIgnore]
            public string BuildId
            {
                get => _buildid;
            }
        }
    }

    public partial class depotinfo
    {
        [JsonProperty("config")]
        private ConfigData _config;
        [JsonIgnore]
        public ConfigData Config { get => _config; }
        public class ConfigData
        {

            [JsonProperty("oslist")]
            private string _oslist;
            [JsonIgnore]
            public string oslist { get => _oslist; }
        }
        [JsonProperty("dlcappid")]
        private string _dlcappid;
        [JsonIgnore]
        public string dlcappid { get => _dlcappid; }
    }
}
