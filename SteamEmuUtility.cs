using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;
using System.CodeDom;
using SteamEmuUtility.Common;
using System.IO;

namespace SteamEmuUtility
{
    public class SteamEmuUtility : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SteamEmuUtilitySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("a237961d-d688-4be9-9576-fb635547f854");

        public SteamEmuUtility(IPlayniteAPI api) : base(api)
        {
            settings = new SteamEmuUtilitySettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            yield return new GameMenuItem
            {
                Description = "Enable GreenLuma",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var count = PlayniteCommon.PlayniteCommon.AddFeatures(PlayniteApi, args.Games, "Steam Emu Utility : [GL] Enabled");
                    logger.Info($"Added {count} games GL Features");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Disable GreenLuma",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var count = PlayniteCommon.PlayniteCommon.RemoveFeatures(PlayniteApi, args.Games, "Steam Emu Utility : [GL] Enabled");
                    logger.Info($"Removed {count} games GL Features");
                }
            };
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            var feature = PlayniteApi.Database.Features.Add("Steam Emu Utility : [GL] Enabled");
            if (args.Game.FeatureIds.Contains(feature.Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                var task = GreenLumaCommon.InjectUser32(Path.Combine(GetPluginUserDataPath(),"user32.dll"));
                if (task.IsFaulted == true)
                {
                    logger.Error(task.Exception.GetBaseException().Message);
                    args.CancelStartup = true;
                    return;
                }
                logger.Info("Injected User32.dll to Steam path!");
                GreenLumaCommon.WriteAppList(args.Game.GameId);
                GreenLumaCommon.Stealth();
                if (SteamCommon.IsSteamRunning())
                {
                    logger.Info("Steam Is Running, Killing...");
                    SteamCommon.KillSteam();
                    SteamCommon.RunSteam();
                }
            }
            if (args.CancelStartup == true)
            {
                SteamCommon.KillSteam();
                GreenLumaCommon.DeleteUser32(SteamCommon.GetSteamDir());
                GreenLumaCommon.DeleteAppList();
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            SteamCommon.KillSteam();
            GreenLumaCommon.DeleteUser32(SteamCommon.GetSteamDir());
            GreenLumaCommon.DeleteAppList();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SteamEmuUtilitySettingsView();
        }
    }
}