using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SteamCommon.Models
{
    public class ApplistDetails
    {
        [SerializationPropertyName("response")]
        public ApplistData Applist { get; set; }
        public class ApplistData
        {

            [SerializationPropertyName("last_appid")]
            public int LastAppid { get; set; }
            [SerializationPropertyName("have_more_results")]
            public bool HaveMoreResults { get; set; }
            [SerializationPropertyName("apps")]
            public List<AppsData> Apps { get; set; }
            public class AppsData
            {
                [SerializationPropertyName("appid")]
                public int Appid { get; set; }
                [SerializationPropertyName("name")]
                public string Name { get; set; }
            }
        }
    }
}
