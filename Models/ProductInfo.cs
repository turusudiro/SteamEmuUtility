using System.Collections.Generic;

namespace SteamCommon.Models
{
#nullable enable
    public partial class SteamProductInfo
    {
        public uint AppId { get; set; }
    }

    public partial class ProductInfo
    {
        public Dictionary<int, AppIdData>? AppId { get; set; } = new Dictionary<int, AppIdData>();
        public class AppIdData
        {

            public CommonData Common { get; set; } = new CommonData();
            public class CommonData
            {
                public string? Name { get; set; }
                public string? Type { get; set; }
                public List<AssociationsData> Associations { get; set; } = new List<AssociationsData>();
                public class AssociationsData
                {
                    public string? Type { get; set; }
                    public string? Name { get; set; }
                }

                public class LanguagesData
                {
                    public string? English { get; set; }
                    public string? German { get; set; }
                }
                public LanguagesData? Languages { get; set; } = new LanguagesData();
            }

            public ExtendedData Extended { get; set; } = new ExtendedData();
            public class ExtendedData
            {
                public List<int>? listofdlc { get; set; }
            }
            public ConfigData Config { get; set; } = new ConfigData();
            public class ConfigData
            {
                public string? installdir { get; set; }
                public List<LaunchData> Launch { get; set; } = new List<LaunchData>();
                public class LaunchData
                {
                    public string? Executable { get; set; }
                    public string? Arguments { get; set; }
                    public string? WorkingDir { get; set; }
                    public string? Description { get; set; }
                    public string? Type { get; set; }
                    public ConfigLaunchData Config { get; set; } = new ConfigLaunchData();
                    public class ConfigLaunchData
                    {
                        public string? OsList { get; set; }
                        public string? OsArch { get; set; }
                        public string? BetaKey { get; set; }
                    }
                }
            }

        }

    }

}
