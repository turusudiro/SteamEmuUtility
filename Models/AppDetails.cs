using System.Collections.Generic;

namespace SteamEmuUtility.Services.Models
{
    public partial class SteamAppDetails
    {
        public class AppDetails
        {
            public string type;
            public string name;
            public int steam_appid;
            public List<int> dlc;
        }
        public bool success
        {
            get; set;
        }
        public AppDetails data
        {
            get; set;
        }
    }
}