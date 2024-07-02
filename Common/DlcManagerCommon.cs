using Playnite.SDK;
using Playnite.SDK.Data;
using PluginsCommon;
using SteamCommon;
using SteamCommon.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace DlcManagerCommon
{
    public class DlcManager
    {
        public static bool HasDLC(string pluginPath, string appid)
        {
            string file = Path.Combine(Path.Combine(pluginPath, "GamesInfo", $"{appid}.json"));

            return FileSystem.FileExists(file);
        }
        public static IEnumerable<DlcInfo> GetDLC(string pluginPath, string appid)
        {
            string file = Path.Combine(Path.Combine(pluginPath, "GamesInfo", $"{appid}.json"));

            if (Serialization.TryFromJsonFile(file, out ObservableCollection<DlcInfo> json))
            {
                return json.Where(dlc => dlc.Enable.Equals(true));
            }
            else
            {
                return Enumerable.Empty<DlcInfo>();
            }
        }
        public static IEnumerable<string> GetDLCAppid(string pluginPath, string appid)
        {
            string path = Path.Combine(Path.Combine(pluginPath, "GamesInfo", $"{appid}.json"));

            if (Serialization.TryFromJsonFile(path, out ObservableCollection<DlcInfo> json))
            {
                return json.Where(dlc => dlc.Enable.Equals(true)).Select(x => x.Appid.ToString());
            }
            else
            {
                return Enumerable.Empty<string>();
            }
        }
        public static void GenerateDLC(string appid, SteamService steam, GlobalProgressActionArgs progress, string apiKey, string pluginPath)
        {
            var dlc = SteamUtilities.GetDLC(appid, steam, apiKey: apiKey);

            progress.IsIndeterminate = false;
            progress.ProgressMaxValue = dlc.Count();
            progress.CurrentProgressValue = 0;

            List<DlcInfo> dlclist = new List<DlcInfo>();

            SteamUtilities.GetApp(dlc, steam, CallbackApp);

            void CallbackApp(object obj)
            {
                if (obj is App app)
                {
                    progress.CurrentProgressValue++;

                    string name = !string.IsNullOrEmpty(app?.Name) ? app.Name : "Unknown App";

                    progress.Text = $"Processing {name}";
                    var newDlc = new DlcInfo
                    {
                        Appid = app.Appid,
                        Name = name,
                        Enable = true
                    };

                    if (!string.IsNullOrEmpty(app?.SmallCapsuleImage?.English))
                    {
                        string filePath = Path.Combine(pluginPath, "GamesInfo", "Assets", appid, $"{app.Appid}.jpg");
                        newDlc.ImageURL = app.SmallCapsuleImage?.English;
                        newDlc.ImagePath = filePath;
                    }

                    dlclist.Add(newDlc);
                }
            }

            FileSystem.WriteStringToFile(Path.Combine(pluginPath, "GamesInfo", $"{appid}.json"), Serialization.ToJson(dlclist, true));
        }
    }
}
