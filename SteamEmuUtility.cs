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
using System.Threading;
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
                Description = ResourceProvider.GetString("LOCSEU_MenuUnlockAll"),
                MenuSection = $"@Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_GodMode")}",
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
                Description = ResourceProvider.GetString("LOCSEU_MenuUnlockAllClean"),
                MenuSection = $"@Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_GodMode")}",
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
                Description = ResourceProvider.GetString("LOCSEU_LoadTickets"),
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
            window.Width = 880;
            window.Title = ResourceProvider.GetString("LOCSEU_GoldbergConfigGenerator");
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
                Description = ResourceProvider.GetString("LOCSEU_EnableGoldberg"),
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    PlayniteUtilities.AddFeatures(SteamGame, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = false);
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGoldberg"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"[{ResourceProvider.GetString("LOCSEU_NormalMode")}] {ResourceProvider.GetString("LOCSEU_GameOnly")}",
                MenuSection = $@"Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_EnableGreenLuma")}",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaNormal"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"[{ResourceProvider.GetString("LOCSEU_NormalMode")}] {ResourceProvider.GetString("LOCSEU_DLCOnly")}",
                MenuSection = $@"Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_EnableGreenLuma")}",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaNormal"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"[{ResourceProvider.GetString("LOCSEU_NormalMode")}] {ResourceProvider.GetString("LOCSEU_GameAndDLC")}",
                MenuSection = $@"Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_EnableGreenLuma")}",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaNormal"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"[{ResourceProvider.GetString("LOCSEU_StealthMode")}] {ResourceProvider.GetString("LOCSEU_GameOnly")}",
                MenuSection = $@"Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_EnableGreenLuma")}",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"[{ResourceProvider.GetString("LOCSEU_StealthMode")}] {ResourceProvider.GetString("LOCSEU_DLCOnly")}",
                MenuSection = $@"Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_EnableGreenLuma")}",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"[{ResourceProvider.GetString("LOCSEU_StealthMode")}] {ResourceProvider.GetString("LOCSEU_GameAndDLC")}",
                MenuSection = $@"Steam Emu Utility|{ResourceProvider.GetString("LOCSEU_EnableGreenLuma")}",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    SteamGame.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(SteamGame, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), SteamGame.Count()), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCSEU_DisableGoldberg"),
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    args.Games.ForEach(x => x.IncludeLibraryPluginAction = true);
                    PlayniteUtilities.RemoveFeatures(args.Games, Goldberg.Feature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_RemoveGoldbergFeature"), args.Games.Count), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = ResourceProvider.GetString("LOCSEU_DisableGreenLuma"),
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_RemoveGreenlumaFeature"), args.Games.Count), "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = string.Format(ResourceProvider.GetString("LOCSEU_ResetAchievement"), SteamGame.Count()),
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    GoldbergTasks.ResetAchievementFile(SteamGame, PlayniteApi);
                }
            };
            yield return new GameMenuItem
            {
                Description = string.Format(ResourceProvider.GetString("LOCSEU_OpenGoldbergGenerator"), SteamGame.Count()),
                MenuSection = @"Steam Emu Utility",
                Action = (a) =>
                {
                    if (SteamGame.Count() == 0)
                    {
                        PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_Error0SteamGame"));
                        return;
                    }
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
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFeature"));
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
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFileMissing"));
                    args.CancelStartup = true;
                    return;
                }
                GreenLumaTasks.GreenLumaStealthMode(args, PlayniteApi, appids);
            }
            else if (GreenLumaNormal)
            {
                if (!GreenLuma.GreenLumaFilesExists(out List<string> _))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFileMissing"));
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
                if (game.Features.Any(x => x.Name.Equals("[SEU] Goldberg")))
                {
                    FileSystem.DeleteDirectory(Goldberg.SteamSettingsPath);
                    FileSystem.DeleteFile(Goldberg.ColdClientIni);
                }
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
                if (InternetCommon.Internet.IsInternetAvailable())
                {
                    GoldbergTasks.CheckForUpdate(PlayniteApi, settings, this);
                }
            }
            if (settings.Settings.CheckGreenLumaUpdate)
            {
                if (InternetCommon.Internet.IsInternetAvailable())
                {
                    GreenLumaTasks.CheckForUpdate(PlayniteApi, this);
                }
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