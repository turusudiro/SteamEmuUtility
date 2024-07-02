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
        public List<uint> Depots { get; set; }
        public List<string> SupportedLanguages { get; set; }
        public Language SmallCapsuleImage { get; set; }
        public bool CloudSaveAvailable { get; set; }
        public bool CloudSaveConfigured { get; set; }
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
