using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SteamCommon.Models
{
    public partial class App
    {
        public uint Appid { get; set; }
        public uint? BuildID { get; set; }
        public bool? ControllerSupport { get; set; } = false;
        public Dictionary<uint, Controller> SteamControllerConfigDetails { get; set; }
        public Dictionary<uint, Controller> SteamControllerTouchConfigDetails { get; set; }
        public List<Launch> Launch { get; set; }
        public string Name { get; set; }
        public List<uint> DLC { get; set; }
        public Dictionary<uint, Depots> Depots { get; set; }
        public List<string> SupportedLanguages { get; set; }
        public Language SmallCapsuleImage { get; set; }
        public bool CloudSaveAvailable { get; set; }
        public bool CloudSaveConfigured { get; set; }
        public IEnumerable<Branches> Branches { get; set; }
        public string Installdir { get; set; }
        public string InstallSize { get; set; }
    }
    public partial class Branches
    {
        [SerializationPropertyName("name")]
        public string Name { get; set; }
        [SerializationPropertyName("description")]
        public string Description { get; set; }
        [SerializationPropertyName("protected")]
        public bool Protected { get; set; }
        [SerializationPropertyName("build_id")]
        public uint BuildID { get; set; }
        [SerializationPropertyName("time_updated")]
        public int TimeUpdated { get; set; }
    }
    public partial class Depots
    {
        public int DlcAppID { get; set; }
        public int DepotFromApp { get; set; }
        public string Manifest { get; set; }
        public bool SharedInstall { get; set; }
        public string Size { get; set; }
    }

    public partial class Language
    {
        public string English { get; set; }
    }
    public partial class Controller
    {
        public string ControllerType { get; set; }
        public string EnabledBranches { get; set; }
    }
    public partial class Launch
    {
        public string OSList { get; set; }
        public string Executable { get; set; }
    }
}
