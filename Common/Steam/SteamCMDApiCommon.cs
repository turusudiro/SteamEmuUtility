using DownloaderCommon;
using Playnite.SDK.Data;
using SteamCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamCommon
{
    public class SteamCMDApi
    {
        private const string Uri = "https://api.steamcmd.net/v1/info/";
        public static App GetAppInfo(string appid)
        {
            var response = HttpDownloader.DownloadString(Uri + appid);

            var parseJson = Serialization.TryFromJson(response, out SteamCMDApiJson steamCMDApijson);
            if (parseJson && steamCMDApijson.Status == "success")
            {
                var info = steamCMDApijson.Data[appid];
                var app = new App()
                {
                    Appid = uint.TryParse(info.Appid, out uint result) ? result : uint.Parse(appid),
                    Branches = Enumerable.Empty<Branches>(),
                    BuildID = 0,
                    Name = string.Empty,
                    DLC = new List<uint>(),
                    SupportedLanguages = new List<string>(),
                    Depots = new List<uint>(),
                    SteamControllerConfigDetails = new Dictionary<uint, Controller> { },
                    SteamControllerTouchConfigDetails = new Dictionary<uint, Controller> { },
                    Launch = new List<Launch>(),
                };
                var dlc = new List<uint>();

                if (info.Common != null)
                {
                    app.Name = string.IsNullOrEmpty(info.Common.Name) ? string.Empty : info.Common.Name;

                    if (!string.IsNullOrEmpty(info.Common.SmallCapsuleImage?.English))
                    {
                        app.SmallCapsuleImage = new Language() { English = info.Common.SmallCapsuleImage.English };
                    }

                    app.SupportedLanguages = info.Common.SupportedLanguages == null ? null : info.Common.SupportedLanguages.Select(x => x.Key).ToList();

                    app.ControllerSupport = string.IsNullOrEmpty(info.Common.ControllerSupport) ? false : true;
                }

                if (info.Config?.Launch != null)
                {
                    app.Launch = info.Config.Launch.Select(x => new Launch()
                    {
                        Executable = string.IsNullOrEmpty(x.Value?.Executable) ? string.Empty : x.Value.Executable,
                        OSList = string.IsNullOrEmpty(x.Value?.Config?.OS_List) ? string.Empty : x.Value.Config.OS_List
                    }).ToList();

                    app.SteamControllerConfigDetails = info.Config.SteamControllerConfigDetails == null ? null : info.Config.SteamControllerConfigDetails
                        .ToDictionary(x => uint.Parse(x.Key), x => new Controller()
                        {
                            ControllerType = x.Value.ControllerType,
                            EnabledBranches = x.Value.EnabledBranches
                        });
                    app.SteamControllerTouchConfigDetails = info.Config.SteamControllerConfigDetails == null ? null : info.Config.SteamControllerTouchConfigDetails
                        .ToDictionary(x => uint.Parse(x.Key), x => new Controller()
                        {
                            ControllerType = x.Value.ControllerType,
                            EnabledBranches = x.Value.EnabledBranches
                        });
                }

                if (info.Depots != null)
                {
                    if (info.Depots.Branches.Any())
                    {
                        var branches = new List<Branches>();
                        foreach (var branch in info.Depots.Branches)
                        {
                            var branchinfo = new Branches()
                            {
                                Name = branch.Key,
                                Description = string.Empty,
                                Protected = false,
                                BuildID = 0,
                                TimeUpdated = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                            };

                            if (!string.IsNullOrEmpty(branch.Value.Description))
                            {
                                branchinfo.Description = branch.Value.Description;
                            }

                            if (!string.IsNullOrEmpty(branch.Value.PwdRequired))
                            {
                                branchinfo.Protected = int.TryParse(branch.Value.PwdRequired, out int value) && (value == 1 ? true : false);
                            }

                            if (!string.IsNullOrEmpty(branch.Value.BuildID))
                            {
                                branchinfo.BuildID = uint.Parse(branch.Value.BuildID);
                            }

                            if (!string.IsNullOrEmpty(branch.Value.TimeUpdated))
                            {
                                branchinfo.TimeUpdated = int.Parse(branch.Value.TimeUpdated);
                            }

                            branches.Add(branchinfo);

                            app.Branches = branches;
                        }

                        if (app.Branches.Any(x => x.Name.Equals("public")))
                        {
                            app.BuildID = app.Branches.FirstOrDefault(x => x.Name.Equals("public")).BuildID;
                        }
                    }
                    app.Depots = info.Depots.Depots == null ? null : info.Depots.Depots.Select(x => x.Key).ToList();
                    if (info.Depots.Depots != null && info.Depots.Depots.Values.Any(x => !string.IsNullOrEmpty(x.dlcappid)))
                    {
                        dlc.AddMissing(info.Depots.Depots.Values.Where(x => uint.TryParse(x.dlcappid, out uint _)).Select(x => uint.Parse(x.dlcappid)).ToList());
                    }
                }

                if (info.Extended != null)
                {
                    dlc.AddMissing(string.IsNullOrEmpty(info.Extended.ListOfDLC) ? null : info.Extended.ListOfDLC.Split(',').Where(x => uint.TryParse(x, out uint _)).Select(x => uint.Parse(x)).ToList());
                }

                if (info.Ufs != null)
                {
                    app.CloudSaveAvailable = true;
                    if (info.Ufs.SaveFiles != null)
                    {
                        app.CloudSaveConfigured = true;
                    }
                }

                app.DLC = dlc;
                return app;
            }
            else
            {
                return null;
            }
        }
    }
}
