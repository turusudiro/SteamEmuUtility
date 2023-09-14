using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using SteamEmuUtility.Common;
using SteamEmuUtility.Services;

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
                Description = "DLC Unlocking",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaFull(PlayniteApi));
                    PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaGame(PlayniteApi));
                    var count = PlayniteCommon.AddFeatures(args.Games, GreenLumaCommon.GreenLumaDLC(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {count} games GL DLC Unlocking Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Game Unlocking",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaFull(PlayniteApi));
                    PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaDLC(PlayniteApi));
                    var count = PlayniteCommon.AddFeatures(args.Games, GreenLumaCommon.GreenLumaGame(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {count} games GL Game Unlocking Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Game And DLC Unlocking",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaDLC(PlayniteApi));
                    PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaGame(PlayniteApi));
                    var count = PlayniteCommon.AddFeatures(args.Games, GreenLumaCommon.GreenLumaFull(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {count} games GL Game And DLC Unlocking Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Disable GreenLuma",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var count = PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaDLC(PlayniteApi))
                    + PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaGame(PlayniteApi))
                    + PlayniteCommon.RemoveFeatures(args.Games, GreenLumaCommon.GreenLumaFull(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Removed {count} games GL Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Generate {args.Games.Where(x => SteamCommon.IsGameSteamGame(x)).Count()} Games DLC Info for GreenLuma",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var Steamservice = new SteamService(this, logger);
                    GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                    progress.Cancelable = true;
                    var b = PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                    {
                        var games = args.Games.Where(x => SteamCommon.IsGameSteamGame(x));
                        global.IsIndeterminate = false;
                        global.ProgressMaxValue = games.Count();
                        global.CurrentProgressValue = 0;
                        logger.Info("Getting DLC Info...");
                        foreach (var game in games)
                        {
                            if (global.CancelToken.IsCancellationRequested)
                            {
                                global.Text = "Cancelling...";
                                break;
                            }
                            Thread.Sleep(TimeSpan.FromSeconds(1));
                            global.Text = $"Getting DLC Info for {game.Name}";
                            global.CurrentProgressValue++;
                            Task.Run(() => Steamservice.GetDLCStore(game, global));
                        }
                    }, progress);
                }
            };
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaDLC(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                var task = GreenLumaCommon.InjectUser32(Path.Combine(GetPluginUserDataPath(), GreenLumaCommon.user32));
                if (task.IsFaulted == true)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage($"Cannot inject {GreenLumaCommon.user32} = {task.Exception.GetBaseException().Message}", "Steam Emu Utility");
                    logger.Error(task.Exception.GetBaseException().Message);
                    args.CancelStartup = true;
                    return;
                }
                logger.Info("Injected User32.dll to Steam path!");
                GreenLumaCommon.InjectDLC(args, this, logger, 0);
                GreenLumaCommon.Stealth();
                if (SteamCommon.IsSteamRunning())
                {
                    logger.Info("Steam Is Running, Killing...");
                    SteamCommon.KillSteam();
                    Task.Delay(100).Wait();
                    SteamCommon.RunSteam();
                }
            }
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaGame(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                var task = GreenLumaCommon.InjectUser32(Path.Combine(GetPluginUserDataPath(), GreenLumaCommon.user32));
                if (task.IsFaulted == true)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage($"Cannot inject {GreenLumaCommon.user32} = {task.Exception.GetBaseException().Message}", "Steam Emu Utility");
                    logger.Error(task.Exception.GetBaseException().Message);
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaCommon.InjectGame(args, this, logger);
                GreenLumaCommon.Stealth();
                if (SteamCommon.IsSteamRunning())
                {
                    logger.Info("Steam Is Running, Killing...");
                    SteamCommon.KillSteam();
                    Task.Delay(100).Wait();
                    SteamCommon.RunSteam();
                }
            }
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaFull(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                var task = GreenLumaCommon.InjectUser32(Path.Combine(GetPluginUserDataPath(), GreenLumaCommon.user32));
                if (task.IsFaulted == true)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage($"Cannot inject {GreenLumaCommon.user32} = {task.Exception.GetBaseException().Message}", "Steam Emu Utility");
                    logger.Error(task.Exception.GetBaseException().Message);
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaCommon.InjectGame(args, this, logger);
                GreenLumaCommon.InjectDLC(args, this, logger, 1);
                GreenLumaCommon.Stealth();
                if (SteamCommon.IsSteamRunning())
                {
                    logger.Info("Steam Is Running, Killing...");
                    SteamCommon.KillSteam();
                    Task.Delay(100).Wait();
                    SteamCommon.RunSteam();
                }
            }
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaDLC(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                SteamCommon.KillSteam();
                Task.Delay(100).Wait();
                GreenLumaCommon.DeleteUser32(SteamCommon.GetSteamDir());
                GreenLumaCommon.DeleteAppList();
            }
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaGame(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
            {
                SteamCommon.KillSteam();
                Task.Delay(100).Wait();
                GreenLumaCommon.DeleteUser32(SteamCommon.GetSteamDir());
                GreenLumaCommon.DeleteAppList();
            }
            if (args.Game.FeatureIds.Contains(GreenLumaCommon.GreenLumaFull(PlayniteApi).Id) && SteamCommon.IsGameSteamGame(args.Game))
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