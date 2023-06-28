using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using SteamEmuUtility.Common;

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
                    var count = PlayniteCommon.AddFeatures(args.Games, GreenLumaCommon.GreenLumaFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {count} games GL Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Disable GreenLuma",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var count = PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Removed {count} games GL Features", "Steam Emu Utility");
                }
            };
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaFeature(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                var task = GreenLumaCommon.InjectUser32(Path.Combine(GetPluginUserDataPath(), "user32.dll"));
                if (task.IsFaulted == true)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage($"Cannot inject {GreenLumaCommon.user32} = {task.Exception.GetBaseException().Message}", "Steam Emu Utility");
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
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaFeature(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                SteamCommon.KillSteam();
                Task.Delay(100).Wait();
                GreenLumaCommon.DeleteUser32(SteamCommon.GetSteamDir());
                GreenLumaCommon.DeleteAppList();
            }
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