using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using GreenLumaCommon;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteCommon;
using ProcessCommon;
using ServiceCommon;
using SteamCommon;

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

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "Load OwnershipTickets or EncryptedAppTickets",
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var files = PlayniteApi.Dialogs.SelectFiles("Tickets Files|EncryptedTicket.*;Ticket.*");
                    if (files == null)
                    {
                        return;
                    }
                    else if (!files.Any())
                    {
                        return;
                    }
                    List<string> AppOwnershipTickets = files.Where(x => Regex.IsMatch(Path.GetFileName(x), @"^Ticket.*", RegexOptions.IgnoreCase)).ToList();
                    List<string> EncryptedAppTickets = files.Where(x => Regex.IsMatch(Path.GetFileName(x), @"^EncryptedTicket.*")).ToList();
                    bool availableAppOwnership = AppOwnershipTickets.Any();
                    bool availableEncryptedApp = EncryptedAppTickets.Any();
                    logger.Info(availableAppOwnership.ToString());
                    logger.Info(availableEncryptedApp.ToString());
                    if (!Directory.Exists(Path.Combine(GetPluginUserDataPath(), "Common", "AppOwnershipTickets")) && availableAppOwnership)
                    {
                        Directory.CreateDirectory((Path.Combine(GetPluginUserDataPath(), "Common", "AppOwnershipTickets")));
                    }
                    if (!Directory.Exists(Path.Combine(GetPluginUserDataPath(), "Common", "EncryptedAppTickets")) && availableEncryptedApp)
                    {
                        Directory.CreateDirectory((Path.Combine(GetPluginUserDataPath(), "Common", "EncryptedAppTickets")));
                    }
                    GlobalProgressOptions progressOptions = new GlobalProgressOptions("Steam Emu Utility");
                    PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
                    {
                        progress.ProgressMaxValue = AppOwnershipTickets.Count + EncryptedAppTickets.Count;
                        foreach (string file in AppOwnershipTickets)
                        {
                            string destinationFile = Path.Combine(GetPluginUserDataPath(), "Common", "AppOwnershipTickets", Path.GetFileName(file));
                            progress.Text = "Copying " + Path.GetFileName(file) + " to " + destinationFile;
                            progress.CurrentProgressValue++;
                            if (File.Exists(destinationFile))
                            {
                                if (PlayniteApi.Dialogs.ShowMessage($"The file {Path.GetFileName(file)} already exists. Do you want to overwrite it?", "Steam Emu Utility", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                                {
                                    progress.Text = "Skipping " + Path.GetFileName(file);
                                    progress.CurrentProgressValue++;
                                    continue;
                                }
                            }
                            File.Copy(file, destinationFile, true);
                        }
                        foreach (string file in EncryptedAppTickets)
                        {
                            string destinationFile = Path.Combine(GetPluginUserDataPath(), "Common", "EncryptedAppTickets", Path.GetFileName(file));
                            progress.Text = "Copying " + Path.GetFileName(file) + " to " + destinationFile;
                            progress.CurrentProgressValue++;
                            if (File.Exists(destinationFile))
                            {
                                if (PlayniteApi.Dialogs.ShowMessage($"The file {Path.GetFileName(file)} already exists. Do you want to overwrite it?", "Steam Emu Utility", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information) == System.Windows.MessageBoxResult.No)
                                {
                                    progress.Text = "Skipping " + Path.GetFileName(file);
                                    progress.CurrentProgressValue++;
                                    continue;
                                }
                            }
                            File.Copy(file, destinationFile, true);
                        }
                    }, progressOptions);
                    if (availableAppOwnership && !availableEncryptedApp)
                    {
                        PlayniteApi.Dialogs.ShowMessage($"Copied {AppOwnershipTickets.Count} AppOwnershipTickets");
                    }
                    else if (availableEncryptedApp && !availableAppOwnership)
                    {
                        PlayniteApi.Dialogs.ShowMessage($"Copied {EncryptedAppTickets.Count} EncryptedAppTickets");
                    }
                    else if (availableEncryptedApp && availableAppOwnership)
                    {
                        PlayniteApi.Dialogs.ShowMessage($"Copied {AppOwnershipTickets.Count} AppOwnershipTickets and {EncryptedAppTickets.Count} EncryptedAppTickets");
                    }
                }
            };
        }
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            yield return new GameMenuItem
            {
                Description = "[Normal Mode] Game Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Normal Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Normal Mode] DLC Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Normal Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Normal Mode] Game and DLC",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Normal Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Stealth Mode] Game Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Stealth Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Stealth Mode] DLC Only",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Stealth Mode Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = "[Stealth Mode] Game and DLC",
                MenuSection = @"Steam Emu Utility|Enable GreenLuma",
                Action = (a) =>
                {
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.NormalFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.GameFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.DLCFeature(PlayniteApi));
                    PlayniteUtilities.AddFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi));
                    PlayniteApi.Dialogs.ShowMessage($"Added {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games Stealth Mode Features", "Steam Emu Utility");
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
                    PlayniteUtilities.RemoveFeatures(args.Games, GreenLuma.StealthFeature(PlayniteApi)); ;
                    PlayniteApi.Dialogs.ShowMessage($"Removed {args.Games.Where(SteamUtilities.IsGameSteamGame).Count()} games GL Features", "Steam Emu Utility");
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Generate {args.Games.Where(x => SteamUtilities.IsGameSteamGame(x)).Count()} Games DLC Info for GreenLuma",
                MenuSection = @"Steam Emu Utility|Generate Info",
                Action = (a) =>
                {
                    string CommonPath = Path.Combine(GetPluginUserDataPath(), "Common");
                    var Steamservice = new SteamService();
                    var games = args.Games.Where(x => SteamUtilities.IsGameSteamGame(x));
                    GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                    progress.Cancelable = true;
                    var b = PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                    {
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
                            Task.Run(() => Steamservice.GetDLCStore(int.Parse(game.GameId), CommonPath));
                        }
                    }, progress);
                }
            };
            yield return new GameMenuItem
            {
                Description = $"Generate {args.Games.Where(x => SteamUtilities.IsGameSteamGame(x)).Count()} Games DLC Info for GreenLuma using SteamKit2",
                MenuSection = @"Steam Emu Utility|Generate Info",
                Action = (a) =>
                {
                    List<uint> appids = new List<uint>();
                    var games = args.Games.Where(x => SteamUtilities.IsGameSteamGame(x));
                    appids.AddRange(games.Select(x => uint.Parse(x.GameId)));
                    string CommonPath = Path.Combine(GetPluginUserDataPath(), "Common");
                    var Steamservice = new SteamService();
                    GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                    progress.Cancelable = true;
                    var b = PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                    {
                        progressOptions.IsIndeterminate = false;
                        progressOptions.ProgressMaxValue = games.Count();
                        progressOptions.CurrentProgressValue = 0;
                        logger.Info("Getting DLC Info...");
                        GreenLuma.GenerateDLCSteamKit(appids, progressOptions, CommonPath);
                    }, progress);
                }
            };
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            var GreenLumaStealth = args.Game.Features.Any(x => x.Name.Equals("[SEU] Stealth Mode", StringComparison.OrdinalIgnoreCase));
            var GreenLumaNormal = args.Game.Features.Any(x => x.Name.Equals("[SEU] Normal Mode", StringComparison.OrdinalIgnoreCase));
            var GreenLumaGameUnlocking = args.Game.Features.Any(x => x.Name.Equals("[SEU] Game Unlocking", StringComparison.OrdinalIgnoreCase));
            var GreenLumaDLCUnlocking = args.Game.Features.Any(x => x.Name.Equals("[SEU] DLC Unlocking", StringComparison.OrdinalIgnoreCase));
            if (!SteamUtilities.IsGameSteamGame(args.Game))
            {
                return;
            }
            List<string> appids = new List<string> { args.Game.GameId };
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
                GreenLuma.WriteAppList(appids);
            }
            else if (GreenLumaDLCUnlocking && !GreenLumaGameUnlocking)
            {
                string dlcpath = $"{GetPluginUserDataPath()}\\Common\\{args.Game.GameId}.txt";
                if (!File.Exists(dlcpath))
                {
                    GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                    PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                    {
                        progressOptions.ProgressMaxValue = appids.Count;
                        progressOptions.CurrentProgressValue = 0;
                        if (!GreenLuma.GenerateDLC(appids, progressOptions, CommonPath))
                        {
                            progressOptions.Text = "Failed to get info from store, using SteamKit2 instead";
                            progressOptions.ProgressMaxValue = appids.Count;
                            progressOptions.CurrentProgressValue = 0;
                            if (!GreenLuma.GenerateDLCSteamKit(appids.Select(x => uint.Parse(x)).ToList(), progressOptions, CommonPath))
                            {
                                PlayniteApi.Dialogs.ShowErrorMessage($"There is no detected DLC for {args.Game.Name}, ensure that this app has a DLC. Please enable Game only feature for {args.Game.Name}.");
                                args.CancelStartup = true;
                                return;
                            }
                        }
                    }, progress);
                }
                appids = File.ReadAllLines(dlcpath).ToList();
                GreenLuma.WriteAppList(appids);
            }
            else if (GreenLumaGameUnlocking && GreenLumaDLCUnlocking)
            {
                string dlcpath = $"{GetPluginUserDataPath()}\\Common\\{args.Game.GameId}.txt";
                if (!File.Exists(dlcpath))
                {
                    GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                    PlayniteApi.Dialogs.ActivateGlobalProgress((progressOptions) =>
                    {
                        progressOptions.ProgressMaxValue = appids.Count;
                        progressOptions.CurrentProgressValue = 0;
                        if (!GreenLuma.GenerateDLC(appids, progressOptions, CommonPath))
                        {
                            progressOptions.Text = "Failed to get info from store, using SteamKit2 instead";
                            progressOptions.ProgressMaxValue = appids.Count;
                            progressOptions.CurrentProgressValue = 0;
                            if (!GreenLuma.GenerateDLCSteamKit(appids.Select(x => uint.Parse(x)).ToList(), progressOptions, CommonPath))
                            {
                                PlayniteApi.Dialogs.ShowErrorMessage($"There is no detected DLC for {args.Game.Name}, ensure that this app has a DLC. Please enable Game only feature for {args.Game.Name}.");
                                args.CancelStartup = true;
                                return;
                            }
                        }
                    }, progress);
                }
                appids.AddRange(File.ReadAllLines(dlcpath).ToList());
                GreenLuma.WriteAppList(appids);
            }
            else
            {
                return;
            }
            if (GreenLumaStealth)
            {
                if (ProcessUtilities.IsProcessRunning("steam"))
                {
                    if (PlayniteApi.Dialogs.ShowMessage("Steam is running! Restart steam with Injector?", "ERROR!", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                    {
                        while (ProcessUtilities.IsProcessRunning("steam"))
                        {
                            ProcessUtilities.ProcessKill("steam");
                            Thread.Sleep(settings.Settings.MillisecondsToWait);
                        }
                    }
                    else
                    {
                        args.CancelStartup = true;
                        return;
                    }
                }
                GreenLuma.StealthMode(Path.Combine(GetPluginUserDataPath(), "GreenLuma\\StealthMode\\User32.dll")).Wait();
                if (settings.Settings.CleanAppCache)
                {
                    GreenLuma.CleanAppCache();
                }
                if (settings.Settings.SkipUpdateStealth)
                {
                    ProcessUtilities.TryStartProcess(SteamUtilities.SteamExecutable, $"-inhibitbootstrap -applaunch {args.Game.GameId}", SteamUtilities.SteamDirectory);
                }
            }
            else if (GreenLumaNormal)
            {
                if (ProcessUtilities.IsProcessRunning("steam") && !ProcessUtilities.IsProcessRunning("dllinjector"))
                {
                    if (PlayniteApi.Dialogs.ShowMessage("Steam is running! Restart steam with Injector?", "ERROR!", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes)
                    {
                        while (ProcessUtilities.IsProcessRunning("steam"))
                        {
                            ProcessUtilities.ProcessKill("steam");
                            Thread.Sleep(settings.Settings.MillisecondsToWait);
                        }
                    }
                    else
                    {
                        args.CancelStartup = true;
                        return;
                    }
                }
                if (!File.Exists(Path.Combine(GetPluginUserDataPath(), "backup\\Steam\\bin\\x64launcher.exe")))
                {
                    GreenLuma.BackupX64Launcher(GetPluginUserDataPath());
                }
                if (File.Exists(Path.Combine(GetPluginUserDataPath(), "Common\\AppOwnershipTickets", $"Ticket.{args.Game.GameId}")) && settings.Settings.InjectAppOwnership)
                {
                    logger.Info("Found AppOwnershipTickets, copying...");
                    GreenLuma.InjectAppOwnershipTickets(Path.Combine(GetPluginUserDataPath(), "Common\\AppOwnershipTickets", $"Ticket.{args.Game.GameId}"));
                }
                if (File.Exists(Path.Combine(GetPluginUserDataPath(), "Common\\EncryptedAppTickets", $"EncryptedTicket.{args.Game.GameId}")) && settings.Settings.InjectEncryptedApp)
                {
                    logger.Info("Found EncryptedAppTickets, copying...");
                    GreenLuma.InjectEncryptedAppTickets(Path.Combine(GetPluginUserDataPath(), "Common\\EncryptedAppTickets", $"EncryptedTicket.{args.Game.GameId}"));
                }
                if (settings.Settings.CleanAppCache)
                {
                    GreenLuma.CleanAppCache();
                }
                if (GreenLuma.NormalMode(this))
                {
                    if (!GreenLuma.StartInjector(args, settings.Settings.MaxAttemptDLLInjector, settings.Settings.MillisecondsToWait))
                    {
                        PlayniteApi.Dialogs.ShowMessage("An Error occured! Cannot run Steam with injector!", "ERROR!", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        args.CancelStartup = true;
                        return;
                    }
                }
                else
                {
                    args.CancelStartup = true;
                    return;
                }
            }

        }
        private void CommonGameShutdownLogic(Game game)
        {
            if (!SteamUtilities.IsGameSteamGame(game))
            {
                return;
            }
            if (game.Features != null)
            {
                var GreenLumaStealth = game.Features.Any(x => x.Name.Equals("[SEU] Stealth Mode", StringComparison.OrdinalIgnoreCase));
                var GreenLumaNormal = game.Features.Any(x => x.Name.Equals("[SEU] Normal Mode", StringComparison.OrdinalIgnoreCase));
                var GreenLumaGameUnlocking = game.Features.Any(x => x.Name.Equals("[SEU] Game Unlocking", StringComparison.OrdinalIgnoreCase));
                var GreenLumaDLCUnlocking = game.Features.Any(x => x.Name.Equals("[SEU] DLC Unlocking", StringComparison.OrdinalIgnoreCase));
                if (GreenLumaStealth && GreenLumaNormal)
                {
                    return;
                }
                else if (GreenLumaStealth)
                {
                    if (settings.Settings.CloseSteamOnExit)
                    {
                        while (ProcessUtilities.IsProcessRunning("steam"))
                        {
                            ProcessUtilities.ProcessKill("steam");
                            Thread.Sleep(settings.Settings.MillisecondsToWait);
                        }
                        if (settings.Settings.CleanGreenLuma)
                        {
                            GreenLuma.CleanGreenLumaStealthMode();
                        }
                    }
                }
                else if (GreenLumaNormal)
                {
                    if (settings.Settings.CloseSteamOnExit)
                    {
                        while (ProcessUtilities.IsProcessRunning("steam"))
                        {
                            ProcessUtilities.ProcessKill("steam");
                            Thread.Sleep(settings.Settings.MillisecondsToWait);
                        }
                        while (ProcessUtilities.IsProcessRunning("dllinjector"))
                        {
                            ProcessUtilities.ProcessKill("dllinjector");
                            Thread.Sleep(settings.Settings.MillisecondsToWait);
                        }
                        if (settings.Settings.CleanGreenLuma)
                        {
                            GreenLuma.CleanGreenLumaNormalMode(GetPluginUserDataPath());
                        }
                    }
                }
            }
        }
        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            CommonGameShutdownLogic(args.Game);
        }
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            CommonGameShutdownLogic(args.Game);
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