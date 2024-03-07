using GoldbergCommon;
using GreenLumaCommon;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteCommon;
using PluginsCommon;
using SteamCommon;
using SteamEmuUtility.Controller;
using SteamEmuUtility.ViewModels;
using SteamEmuUtility.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
        public override IEnumerable<SidebarItem> GetSidebarItems()
        {
            yield return new SidebarItem
            {
                Title = "Update",
                // Loads icon from plugin's installation path
                Icon = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png"),
                Activated = () => GreenLumaTasks.CheckForUpdate(PlayniteApi)
            };
        }
        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (Steam.IsGameSteamGame(args.Game) && args.Game.Features.Any(x => x.Name.Equals("[SEU] Goldberg")))
            {
                args.Game.IncludeLibraryPluginAction = false;
                yield return new GoldBergController(args.Game, PlayniteApi, settings.Settings);
            }
        }
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var SteamGame = PlayniteApi.Database.Games.Where(x => x.IsInstalled && Steam.IsGameSteamGame(x));
            yield return new MainMenuItem
            {
                Description = "Unlock all installed Steam games",
                MenuSection = "@Steam Emu Utility|God Mode",
                Action = (a) =>
                {
                    if (settings.Settings.CleanApplist && !FileSystem.IsDirectoryEmpty(Path.Combine(Steam.SteamDirectory, "applist")))
                    {
                        FileSystem.DeleteDirectory(Path.Combine(Steam.SteamDirectory, "applist"));
                    }
                    GreenLumaGenerator.WriteAppList(SteamGame.Select(x => x.GameId).ToList());
                    GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(PlayniteApi);
                }
            };
            yield return new MainMenuItem
            {
                Description = "Unlock all installed Steam games and clean",
                MenuSection = "@Steam Emu Utility|God Mode",
                Action = (a) =>
                {
                    if (settings.Settings.CleanApplist && !FileSystem.IsDirectoryEmpty(Path.Combine(Steam.SteamDirectory, "applist")))
                    {
                        FileSystem.DeleteDirectory(Path.Combine(Steam.SteamDirectory, "applist"));
                    }
                    GreenLumaGenerator.WriteAppList(SteamGame.Select(x => x.GameId).ToList());
                    GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(PlayniteApi);
                    GreenLumaTasks.Token = new CancellationTokenSource();
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
            window.DataContext = new GoldbergConfigViewModel(PlayniteApi, settings.Settings);
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.ShowDialog();
        }
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var SteamGame = args.Games.Where(x => x.IsInstalled && Steam.IsGameSteamGame(x));
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
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(Steam.IsGameSteamGame).Count()} games Stealth Mode Features", "Steam Emu Utility");
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
                    using (var steam = new SteamService())
                    {
                        PlayniteApi.Dialogs.ActivateGlobalProgress(async (progress) =>
                        {
                            progress.CancelToken.Register(() =>
                            {
                                steam.Dispose();
                                return;
                            });
                            var tasks = new List<Task>();
                            foreach (var game in SteamGame)
                            {
                                tasks.Add(Task.Run(() => { GreenLumaGenerator.GenerateDLC(game, steam, progress, settings.Settings.SteamWebApi); }));
                            }
                            while (!Task.WhenAll(tasks).IsCompleted)
                            {
                                if (progress.CancelToken.IsCancellationRequested)
                                {
                                    return;
                                }
                                await Task.Delay(500);
                            }
                        }, new GlobalProgressOptions("Steam Emu Utility", true));
                    }
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Generate {SteamGame.Count()} Games config for Goldberg",
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    GoldbergGenerator.GenerateGoldbergConfig(SteamGame, PlayniteApi, settings.Settings.SteamWebApi);
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
            if (!Steam.IsGameSteamGame(args.Game))
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
            string appid = args.Game.GameId;
            var appids = new List<string>();
            if (GreenLumaGameUnlocking)
            {
                appids.Add(appid);
            }
            if (GreenLumaDLCUnlocking)
            {
                if (!GreenLuma.DLCExists(appid))
                {
                    using (var steam = new SteamService())
                    {
                        PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
                        {
                            progress.CancelToken.Register(() =>
                            {
                                steam.Dispose();
                                return;
                            });
                            GreenLumaGenerator.GenerateDLC(args.Game, steam, progress, settings.Settings.SteamWebApi);
                        }, new GlobalProgressOptions("Steam Emu Utility", true));
                    }
                }
                if (GreenLuma.DLCExists(appid))
                {
                    appids.AddRange(GreenLuma.GetDLC(appid).ToList());
                }
            }
            if (GreenLumaStealth)
            {
                if (!GreenLuma.GreenLumaFilesExists(out List<string> _))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Required GreenLuma files not found. Check your settings");
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaTasks.GreenLumaStealthMode(args, PlayniteApi, appids);
            }
            else if (GreenLumaNormal)
            {
                if (!GreenLuma.GreenLumaFilesExists(out List<string> _))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage("Required GreenLuma files not found. Check your settings");
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaTasks.GreenLumaNormalMode(args, PlayniteApi, appids);
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
                    GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(PlayniteApi);
                    if (settings.Settings.GoldbergCleanSteam)
                    {
                        GreenLumaTasks.Token = new CancellationTokenSource();
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
                                _ = Steam.KillSteam;
                                GreenLumaTasks.Token = new CancellationTokenSource();
                                _ = GreenLumaTasks.CleanGreenLuma();
                                break;
                            case 1:
                                GreenLumaTasks.Token = new CancellationTokenSource();
                                _ = GreenLumaTasks.CleanGreenLuma();
                                break;
                        }
                    }
                }
            }
        }
        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            if (!Steam.IsGameSteamGame(args.Game))
            {
                return;
            }
            CommonGameShutdownLogic(args.Game);
        }
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            if (!Steam.IsGameSteamGame(args.Game))
            {
                return;
            }
            CommonGameShutdownLogic(args.Game);
        }
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            if (settings.Settings.CheckGoldbergUpdate)
            {
                GoldbergTasks.CheckForUpdate(PlayniteApi, settings);
            }
            if (settings.Settings.CheckGreenLumaUpdate)
            {
                GreenLumaTasks.CheckForUpdate(PlayniteApi);
            }
            if (settings.Settings.CleanGreenLumaStartup)
            {
                GreenLumaTasks.Token = new CancellationTokenSource();
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