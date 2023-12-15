using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GoldbergCommon;
using GreenLumaCommon;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteCommon;
using SteamCommon;
using SteamEmuUtility.Controller;
using SteamEmuUtility.ViewModels;
using SteamEmuUtility.Views;

namespace SteamEmuUtility
{
    public class SteamEmuUtility : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SteamEmuUtilitySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("a237961d-d688-4be9-9576-fb635547f854");

        public SteamEmuUtility(IPlayniteAPI api) : base(api)
        {
            Goldberg.PluginPath = GetPluginUserDataPath();
            GreenLuma.PluginPath = GetPluginUserDataPath();
            settings = new SteamEmuUtilitySettingsViewModel(this);
            GreenLuma.GreenLumaSettings = settings.Settings;
            Goldberg.GoldbergSettings = settings.Settings;
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }
        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (SteamUtilities.IsGameSteamGame(args.Game) && args.Game.Features.Any(x => x.Name.Equals("[SEU] Goldberg")))
            {
                args.Game.IncludeLibraryPluginAction = false;
                yield return new GoldBergController(args.Game, PlayniteApi);
            }
        }
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "Unlock all installed Steam games",
                MenuSection = "@Steam Emu Utility|God Mode",
                Action = (a) =>
                {
                    GreenLumaGenerator.WriteAppList(PlayniteApi.Database.Games.Where(x => SteamUtilities.IsGameSteamGame(x)).Select(x => x.GameId).ToList());
                    GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(null, PlayniteApi);
                }
            };
            yield return new MainMenuItem
            {
                Description = "Unlock all installed Steam games and clean",
                MenuSection = "@Steam Emu Utility|God Mode",
                Action = (a) =>
                {
                    GreenLumaGenerator.WriteAppList(PlayniteApi.Database.Games.Where(x => SteamUtilities.IsGameSteamGame(x)).Select(x => x.GameId).ToList());
                    GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(null, PlayniteApi);
                    _ = GreenLumaTasks.CleanGreenLuma();
                }
            };
            yield return new MainMenuItem
            {
                Description = "Load Tickets",
                MenuSection = "@Steam Emu Utility",
                Action = (a) =>
                {
                    GreenLumaTasks.LoadTicket(PlayniteApi);
                }
            };
        }
        public void ShowGoldbergConfig()
        {
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            window.Height = 700;
            window.Width = 830;
            window.Title = "Goldberg Config Generator";
            window.Content = new GoldbergConfigView();
            window.DataContext = new GoldbergConfigViewModel(PlayniteApi);
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.ShowDialog();
        }
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var SteamGame = args.Games.Where(SteamUtilities.IsGameSteamGame);
            yield return new GameMenuItem
            {
                Description = "Enable Goldberg",
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    PlayniteUtilities.AddFeatures(SteamGame, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = false);
                    PlayniteApi.Dialogs.ShowMessage($"Added {SteamGame.Count()} games Goldberg Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Normal Mode] Game Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {SteamGame.Count()} games Normal Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Normal Mode] DLC Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {SteamGame.Count()} games Normal Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Normal Mode] Game and DLC",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {SteamGame.Count()} games Normal Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Stealth Mode] Game Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Stealth Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Stealth Mode] DLC Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {SteamGame.Count()} games Stealth Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Stealth Mode] Game and DLC",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {SteamGame.Count()} games Stealth Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Disable Goldberg",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    args.Games.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Removed {args.Games.Count} games GL Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "Disable GreenLuma",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Removed {args.Games.Count} games GL Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Generate {SteamGame.Count()} Games DLC Info for GreenLuma",
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    var Steamservice = new SteamService();
                    GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                    progress.Cancelable = true;
                    var b = PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                    {
                        var appids = SteamGame.Select(x => x.GameId).ToList();
                        GreenLumaGenerator.GenerateDLC(appids, global);
                    }, progress);
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Generate {SteamGame.Count()} Games config for Goldberg",
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    GoldbergGenerator.GenerateGoldbergConfig(SteamGame, PlayniteApi);
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Reset {SteamGame.Count()} Games Achievements for Goldberg and Achievement Watcher",
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    GoldbergTasks.ResetAchievementFile(SteamGame, PlayniteApi);
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Open Goldberg config generator for {SteamGame.Count()} Games",
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    ShowGoldbergConfig();
                }
            };
        }
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            if (!SteamUtilities.IsGameSteamGame(args.Game))
            {
                return;
            }
            if (args.Game.Features.Any(x => x.Name.Equals("[SEU] Goldberg")))
            {
                return;
            }
            var GreenLumaStealth = args.Game.Features.Any(x => x.Name.Equals("[SEU] Stealth Mode", StringComparison.OrdinalIgnoreCase));
            var GreenLumaNormal = args.Game.Features.Any(x => x.Name.Equals("[SEU] Normal Mode", StringComparison.OrdinalIgnoreCase));
            var GreenLumaGameUnlocking = args.Game.Features.Any(x => x.Name.Equals("[SEU] Game Unlocking", StringComparison.OrdinalIgnoreCase));
            var GreenLumaDLCUnlocking = args.Game.Features.Any(x => x.Name.Equals("[SEU] DLC Unlocking", StringComparison.OrdinalIgnoreCase));
            string CommonPath = Path.Combine(GetPluginUserDataPath(), "Common");
            if (GreenLumaStealth && GreenLumaNormal)
            {
                PlayniteApi.Dialogs.ShowErrorMessage("Normal Mode and Stealth Mode are enabled in this game, please make sure to choose one.");
                args.CancelStartup = true;
                return;
            }
            else if (!GreenLumaNormal && !GreenLumaStealth)
            {
                return;
            }
            if (GreenLumaGameUnlocking && !GreenLumaDLCUnlocking)
            {
                GreenLumaGenerator.WriteAppList(new List<string> { args.Game.GameId });
            }
            else if (GreenLumaDLCUnlocking && !GreenLumaGameUnlocking)
            {
                GreenLumaGenerator.DLCUnlocking(args, PlayniteApi);
            }
            else if (GreenLumaGameUnlocking && GreenLumaDLCUnlocking)
            {
                GreenLumaGenerator.GameAndDLCUnlocking(args, PlayniteApi);
            }
            else
            {
                return;
            }
            if (GreenLumaStealth)
            {
                if (!GreenLuma.GreenLumaFilesExists(out List<string> _))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Required GreenLuma files not found. Check your settings");
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaTasks.GreenLumaStealthMode(args, PlayniteApi);
            }
            else if (GreenLumaNormal)
            {
                if (!GreenLuma.GreenLumaFilesExists(out List<string> _))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Required GreenLuma files not found. Check your settings");
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaTasks.GreenLumaNormalMode(args, PlayniteApi);
            }

        }
        private void CommonGameShutdownLogic(Game game)
        {
            if (game.Features != null)
            {
                if (settings.Settings.OpenSteamAfterExit && game.Features.Any(x => x.Name.Equals("[SEU] Goldberg")))
                {
                    if (!Goldberg.ColdClientExists(out List<string> _))
                    {
                        return;
                    }
                    GreenLumaGenerator.WriteAppList(new List<string> { game.GameId });
                    GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(game, PlayniteApi);
                    if (settings.Settings.GoldbergCleanSteam)
                    {
                        _ = GreenLumaTasks.CleanGreenLuma();
                    }
                    return;
                }
                var GreenLumaStealth = game.Features.Any(x => x.Name.Equals("[SEU] Stealth Mode", StringComparison.OrdinalIgnoreCase));
                var GreenLumaNormal = game.Features.Any(x => x.Name.Equals("[SEU] Normal Mode", StringComparison.OrdinalIgnoreCase));
                var GreenLumaGameUnlocking = game.Features.Any(x => x.Name.Equals("[SEU] Game Unlocking", StringComparison.OrdinalIgnoreCase));
                var GreenLumaDLCUnlocking = game.Features.Any(x => x.Name.Equals("[SEU] DLC Unlocking", StringComparison.OrdinalIgnoreCase));
                if (GreenLumaStealth && GreenLumaNormal)
                {
                    return;
                }
                else if (GreenLumaStealth || GreenLumaNormal)
                {
                    if (!GreenLuma.GreenLumaFilesExists(out List<string> _))
                    {
                        return;
                    }
                    if (settings.Settings.CleanGreenLuma)
                    {
                        switch (settings.Settings.CleanMode)
                        {
                            case 0:
                                _ = SteamUtilities.KillSteam;
                                _ = GreenLumaTasks.CleanGreenLuma();
                                break;
                            case 1:
                                _ = GreenLumaTasks.CleanGreenLuma();
                                break;
                        }
                    }
                }
            }
        }
        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            if (!SteamUtilities.IsGameSteamGame(args.Game))
            {
                return;
            }
            CommonGameShutdownLogic(args.Game);
        }
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (!SteamUtilities.IsGameSteamGame(args.Game))
            {
                return;
            }
            CommonGameShutdownLogic(args.Game);
        }
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (settings.Settings.CleanGreenLumaStartup)
            {
                _ = GreenLumaTasks.CleanGreenLuma();
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