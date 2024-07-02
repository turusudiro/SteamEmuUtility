using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SteamCommon.Models
{
    public partial class SteamCMDApiJson
    {
        [SerializationPropertyName("data")]
        public Dictionary<string, DataDetails> Data { get; set; }
        [SerializationPropertyName("status")]
        public string Status { get; set; }
        public partial class DataDetails
        {
            [SerializationPropertyName("appid")]
            public string Appid { get; set; }
            public CommonInfo Common { get; set; }
            public ConfigInfo Config { get; set; }
            [SerializationPropertyName("depots")]
            public dynamic _depotsdynamic { get; set; }
            private DepotsInfo _depots;
            public DepotsInfo Depots
            {
                get
                {
                    if (_depotsdynamic == null)
                    {
                        return null;
                    }
                    if (_depots != null)
                    {
                        return _depots;
                    }
                    var dep = new DepotsInfo();
                    foreach (var depot in _depotsdynamic)
                    {
                        if (uint.TryParse(depot.Name, out uint result))
                        {
                            dep.Depots.Add(result, depot.Value.ToObject<DepotsInfo.DepotsDetail>());
                        }
                        else
                        {
                            switch (depot.Name)
                            {
                                case "baselanguages":
                                    dep.BaseLanguage = depot.Value;
                                    break;
                                case "branches":
                                    dep.Branches = depot.Value.ToObject<BranchesInfo>();
                                    break;
                            }
                        }
                    }
                    _depots = dep;
                    return dep;
                }
            }
            public ExtendedInfo Extended { get; set; }
            public UfsInfo Ufs { get; set; }

            public partial class CommonInfo
            {
                public string Name { get; set; }
                [SerializationPropertyName("controller_support")]
                public string ControllerSupport { get; set; }
                [SerializationPropertyName("supported_languages")]
                public Dictionary<string, LanguageDetails> SupportedLanguages { get; set; }
                [SerializationPropertyName("small_capsule")]
                public Language SmallCapsuleImage { get; set; }
            }

            public partial class ConfigInfo
            {
                [SerializationPropertyName("launch")]
                public Dictionary<string, LaunchInfo> Launch { get; set; }
                [SerializationPropertyName("steamcontrollerconfigdetails")]
                public Dictionary<string, Controller> SteamControllerConfigDetails { get; set; }
                [SerializationPropertyName("steamcontrollertouchconfigdetails")]
                public Dictionary<string, Controller> SteamControllerTouchConfigDetails { get; set; }
            }

            public partial class DepotsInfo
            {
                public BranchesInfo Branches { get; set; }
                public string BaseLanguage { get; set; }
                public Dictionary<uint, DepotsDetail> Depots { get; set; } = new Dictionary<uint, DepotsDetail>();
                public partial class DepotsDetail
                {
                    [SerializationPropertyName("config")]
                    public ConfigData Config { get; set; }
                    public class ConfigData
                    {

                        [SerializationPropertyName("oslist")]
                        public string oslist { get; set; }
                    }
                    [SerializationPropertyName("dlcappid")]
                    public string dlcappid { get; set; }
                }
            }

            public partial class ExtendedInfo
            {
                [SerializationPropertyName("listofdlc")]
                public string ListOfDLC { get; set; }
            }
            public partial class BranchesInfo
            {
                [SerializationPropertyName("public")]
                public PublicInfo Public { get; set; }
                public class PublicInfo
                {
                    [SerializationPropertyName("buildid")]
                    public string BuildId { get; set; }
                }
            }
            public partial class Controller
            {
                [SerializationPropertyName("controller_type")]
                public string ControllerType { get; set; }
                [SerializationPropertyName("enabled_branches")]
                public string EnabledBranches { get; set; }
            }

            public partial class LaunchInfo
            {
                [SerializationPropertyName("config")]
                public Configs Config { get; set; }
                public partial class Configs
                {
                    [SerializationPropertyName("osarch")]
                    public string OS_Arch { get; set; }
                    [SerializationPropertyName("oslist")]
                    public string OS_List { get; set; }
                }

                [SerializationPropertyName("executable")]
                public string Executable { get; set; }
                [SerializationPropertyName("type")]
                public string Type { get; set; }
                [SerializationPropertyName("arguments")]
                public string Arguments { get; set; }
            }
            public partial class Language
            {
                [SerializationPropertyName("english")]
                public string English { get; set; }
            }

            public partial class LanguageDetails
            {
                [SerializationPropertyName("subtitles")]
                public string Subtitles { get; set; }
                [SerializationPropertyName("supported")]
                public string Supported { get; set; }
            }

            public partial class UfsInfo
            {
                [SerializationPropertyName("maxnumfiles")]
                public string MaxnumFiles { get; set; }
                [SerializationPropertyName("quota")]
                public string Quota { get; set; }
                [SerializationPropertyName("savefiles")]
                public Dictionary<string, saveFilesDetail> SaveFiles { get; set; }
                public partial class saveFilesDetail
                {
                    [SerializationPropertyName("path")]
                    public string Path { get; set; }
                    [SerializationPropertyName("pattern")]
                    public string pattern { get; set; }
                    [SerializationPropertyName("recursive")]
                    public string recursive { get; set; }
                    [SerializationPropertyName("root")]
                    public string root { get; set; }
                }
            }
        }
    }
}
