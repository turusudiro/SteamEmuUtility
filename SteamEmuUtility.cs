using DlcManagerCommon;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SteamEmuUtility
{
    public class SteamEmuUtility : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SteamEmuUtilitySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("a237961d-d688-4be9-9576-fb635547f854");

        private const string goldbergFeature = "[SEU] Goldberg";
        private const string stealthFeature = "[SEU] Stealth Mode";
        private const string normalFeature = "[SEU] Normal Mode";
        private const string familyFeature = "[SEU] Family Beta Mode";
        private const string gameUnlockingFeature = "[SEU] Game Unlocking";
        private const string dlcUnlockingFeature = "[SEU] DLC Unlocking";

        public SteamEmuUtility(IPlayniteAPI api) : base(api)
        {
            settings = new SteamEmuUtilitySettingsViewModel(this);

            Application.Current.Resources.Add("SEU_SteamIco", new TextBlock
            {
                Text = "\xed71",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            });

            Application.Current.Resources.Add("SEU_DeleteIco", new TextBlock
            {
                Text = "\xec53",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            });

            Application.Current.Resources.Add("SEU_SettingIco", new TextBlock
            {
                Text = "\xefe2",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            });

            Application.Current.Resources.Add("SEU_CheckIco", new TextBlock
            {
                Text = "\xeed7",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            });

            Application.Current.Resources.Add("SEU_DisallowIco", new TextBlock
            {
                Text = "\xefa9",
                FontSize = 20,
                FontFamily = ResourceProvider.GetResource("FontIcoFont") as FontFamily
            });

            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }
        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            Game game = args.Game;

            bool isSteamGame = Steam.IsGameSteamGame(game);

            bool goldberg = PlayniteUtilities.HasFeature(game, goldbergFeature);
            bool greenLumaStealth = PlayniteUtilities.HasFeature(game, stealthFeature);
            bool greenLumaFamily = PlayniteUtilities.HasFeature(game, familyFeature);
            bool greenLumaNormal = PlayniteUtilities.HasFeature(game, normalFeature);
            bool hasGreenLumaFeature = greenLumaNormal || greenLumaStealth || greenLumaFamily;

            if (isSteamGame && goldberg && !hasGreenLumaFeature)
            {
                if (game.IncludeLibraryPluginAction && settings.Settings.GoldbergOverride)
                {
                    game.IncludeLibraryPluginAction = false;
                }
                yield return new GoldBergController(args.Game, PlayniteApi, settings.Settings, GetPluginUserDataPath());
            }
        }
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var SteamGame = PlayniteApi.Database.Games.Where(x => x.IsInstalled && Steam.IsGameSteamGame(x));

            string pluginPath = GetPluginUserDataPath();

            string glPath = Path.Combine(pluginPath, "GreenLuma");

            IEnumerable<string> appids = SteamGame.Select(x => x.GameId).ToList();

            yield return new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOCSEU_MenuUnlockAll"),
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    GreenLumaTasks.StartGreenLumaJob(PlayniteApi, appids, GreenLuma.GreenLumaMode.Stealth);
                }
            };
            yield return new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOCSEU_LoadTickets"),
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    GreenLumaTasks.LoadTicket(PlayniteApi);
                }
            };
        }
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var steamGame = args.Games.Where(x => x.IsInstalled && Steam.IsGameSteamGame(x));
            int steamGameCount = steamGame.Count();

            if (steamGameCount == 1)
            {
                yield return new GameMenuItem
                {
                    Icon = "SEU_SteamIco",
                    Description = ResourceProvider.GetString("LOCSEU_ManageDLC"),
                    Action = (a) =>
                    {
                        ShowDlcOption(steamGame.FirstOrDefault());
                    }
                };
            }
            if (steamGameCount >= 1)
            {
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_EnableGoldberg"),
                    MenuSection = "Goldberg",
                    Action = (a) =>
                    {
                        int count = Goldberg.AddGoldbergFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGoldberg"), count, "Steam Emu Utility"));
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_DisallowIco",
                    Description = ResourceProvider.GetString("LOCSEU_DisableGoldberg"),
                    MenuSection = "Goldberg",
                    Action = (a) =>
                    {
                        int count = Goldberg.RemoveGoldbergFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_RemoveGoldbergFeature"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_DeleteIco",
                    Description = string.Format(ResourceProvider.GetString("LOCSEU_ResetAchievement"), steamGameCount),
                    MenuSection = "Goldberg",
                    Action = (a) =>
                    {
                        GoldbergTasks.ResetAchievementFile(steamGame, PlayniteApi);
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_SettingIco",
                    Description = string.Format(ResourceProvider.GetString("LOCSEU_OpenGoldbergGenerator"), steamGameCount),
                    MenuSection = "Goldberg",
                    Action = (a) =>
                    {
                        if (steamGameCount == 0)
                        {
                            PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_Error0SteamGame"));
                            return;
                        }
                        ShowGoldbergConfig();
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_GameOnly"),
                    MenuSection = $"GreenLuma|[{ResourceProvider.GetString("LOCSEU_NormalMode")}]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddNormalGameOnlyFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaNormal"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_DLCOnly"),
                    MenuSection = $"GreenLuma|[{ResourceProvider.GetString("LOCSEU_NormalMode")}]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddNormalDLCOnlyFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaNormal"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_GameAndDLC"),
                    MenuSection = $"GreenLuma|[{ResourceProvider.GetString("LOCSEU_NormalMode")}]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddNormalGameAndDLCFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaNormal"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_GameOnly"),
                    MenuSection = $"GreenLuma|[{ResourceProvider.GetString("LOCSEU_StealthMode")}]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddStealthGameOnlyFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_DLCOnly"),
                    MenuSection = $"GreenLuma|[{ResourceProvider.GetString("LOCSEU_StealthMode")}]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddStealthDLCOnlyFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_GameAndDLC"),
                    MenuSection = $"GreenLuma|[{ResourceProvider.GetString("LOCSEU_StealthMode")}]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddStealthGameAndDLCFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_GameOnly"),
                    MenuSection = "GreenLuma|[Family Beta]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddFamilyGameOnlyFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaStealth"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_DLCOnly"),
                    MenuSection = "GreenLuma|[Family Beta]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddFamilyDLCOnlyFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaFamilyBeta"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_CheckIco",
                    Description = ResourceProvider.GetString("LOCSEU_GameAndDLC"),
                    MenuSection = "GreenLuma|[Family Beta]",
                    Action = (a) =>
                    {
                        int count = GreenLuma.AddFamilyGameAndDLCFeature(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaFamilyBeta"), count), "Steam Emu Utility");
                    }
                };
                yield return new GameMenuItem
                {
                    Icon = "SEU_DisallowIco",
                    Description = ResourceProvider.GetString("LOCSEU_DisableGreenLuma"),
                    MenuSection = "GreenLuma",
                    Action = (a) =>
                    {
                        int count = GreenLuma.DisableGreenLuma(steamGame, PlayniteApi);
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_RemoveGreenlumaFeature"), count), "Steam Emu Utility");
                    }
                };
            }
        }
        void ShowDlcOption(Game game)
        {
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            var viewModel = new DlcManagerViewModels(PlayniteApi, game, settings.Settings.SteamWebApi, GetPluginUserDataPath());

            window.Height = 500;
            window.Width = 1080;
            window.Title = ResourceProvider.GetString("LOCSEU_DlcManagerWindow");
            window.Content = new ManageDlcView();
            window.DataContext = viewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            window.Closed += (sender, e) => viewModel.Dispose();

            window.ShowDialog();
        }
        void ShowGoldbergConfig()
        {
            var window = PlayniteApi.Dialogs.CreateWindow(new WindowCreationOptions
            {
                ShowMinimizeButton = false
            });

            var viewModel = new GoldbergConfigViewModel(PlayniteApi, settings.Settings, GetPluginUserDataPath());

            window.Height = 410;
            window.Width = 780;
            window.Title = ResourceProvider.GetString("LOCSEU_GoldbergConfigGenerator");
            window.Content = new GoldbergConfigView();
            window.DataContext = viewModel;
            window.Owner = PlayniteApi.Dialogs.GetCurrentAppWindow();
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Closed += (sender, e) => viewModel.Dispose();

            if (viewModel.GamesCount >= 2)
            {
                window.Width = 1100;
            }

            window.ShowDialog();
        }
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            Game game = args.Game;

            bool isSteamGame = Steam.IsGameSteamGame(game);

            if (!isSteamGame)
            {
                return;
            }

            bool goldberg = PlayniteUtilities.HasFeature(game, goldbergFeature);
            bool greenLumaStealth = PlayniteUtilities.HasFeature(game, stealthFeature);
            bool greenLumaFamily = PlayniteUtilities.HasFeature(game, familyFeature);
            bool greenLumaNormal = PlayniteUtilities.HasFeature(game, normalFeature);
            bool greenLumaGameUnlocking = PlayniteUtilities.HasFeature(game, gameUnlockingFeature);
            bool greenLumaDLCUnlocking = PlayniteUtilities.HasFeature(game, dlcUnlockingFeature);
            bool hasGreenLumaFeature = greenLumaNormal || greenLumaStealth || greenLumaFamily;

            int glFeature = (greenLumaNormal ? 1 : 0) + (greenLumaStealth ? 1 : 0) + (greenLumaFamily ? 1 : 0);
            bool glFeatureInvalid = glFeature != 1;

            if (goldberg)
            {
                // dont execute the next code if the game have any gl feature mode
                if (hasGreenLumaFeature)
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFeatureGoldberg"));
                    args.CancelStartup = true;
                    return;
                }
                return;
            }
            // return a dialog error if gl feature is invalid
            else if (glFeatureInvalid)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFeature"));
                args.CancelStartup = true;
                return;
            }
            // dont execute the next code if the game doesnt have any gl feature
            else if (!hasGreenLumaFeature)
            {
                return;
            }

            string pluginPath = GetPluginUserDataPath();
            string pluginGreenLumaPath = Path.Combine(pluginPath, "GreenLuma");

            // if greenluma files doesnt exists in plugin userdata path then return.
            if (!GreenLuma.GreenLumaFilesExists(pluginGreenLumaPath, out List<string> _))
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFileMissing"));
                args.CancelStartup = true;
                return;
            }

            string appid = game.GameId;
            var appids = new List<string>();
            if (greenLumaGameUnlocking)
            {
                appids.Add(appid);
            }
            if (greenLumaDLCUnlocking)
            {
                if (!DlcManager.HasDLC(pluginPath, appid))
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
                            GreenLumaGenerator.GenerateDLC(game, steam, progress, settings.Settings.SteamWebApi, pluginPath);
                        }, new GlobalProgressOptions("Steam Emu Utility", true));
                    }
                }
                appids.AddRange(DlcManager.GetDLCAppid(pluginPath, appid));
            }

            // abort if appids 0, since its useless to use greenluma with 0 appids
            if (appids.Count == 0)
            {
                return;
            }

            if (greenLumaNormal)
            {
                GreenLumaTasks.StartGreenLumaJob(args, PlayniteApi, appids, GreenLuma.GreenLumaMode.Normal);
            }
            else if (greenLumaStealth)
            {
                GreenLumaTasks.StartGreenLumaJob(args, PlayniteApi, appids, GreenLuma.GreenLumaMode.Stealth);
            }
            else if (greenLumaFamily)
            {
                GreenLumaTasks.StartGreenLumaJob(args, PlayniteApi, appids, GreenLuma.GreenLumaMode.Family);
            }
        }
        private async void CommonGameShutdownLogic(Game game)
        {
            if (game.Features != null)
            {
                bool goldberg = PlayniteUtilities.HasFeature(game, goldbergFeature);
                bool greenLumaStealth = PlayniteUtilities.HasFeature(game, stealthFeature);
                bool greenLumaFamily = PlayniteUtilities.HasFeature(game, familyFeature);
                bool greenLumaNormal = PlayniteUtilities.HasFeature(game, normalFeature);
                bool hasGreenLumaFeature = greenLumaNormal || greenLumaStealth || greenLumaFamily;

                string pluginPath = GetPluginUserDataPath();

                if (goldberg)
                {
                    if (hasGreenLumaFeature)
                    {
                        return;
                    }
                    string pluginGoldbergPath = Path.Combine(pluginPath, "Goldberg");
                    string steamSettingsPath = Path.Combine(pluginGoldbergPath, "steam_settings");
                    string coldClientLoaderiniPath = Path.Combine(pluginGoldbergPath, "ColdClientLoader.ini");

                    FileSystem.DeleteDirectory(steamSettingsPath);
                    FileSystem.DeleteFile(coldClientLoaderiniPath);

                    if (settings.Settings.OpenSteamAfterExit)
                    {
                        if (!Goldberg.ColdClientExists(pluginGoldbergPath, out List<string> _))
                        {
                            return;
                        }
                        GreenLumaTasks.RunSteamWIthGreenLumaStealthMode(PlayniteApi, new List<string> { game.GameId });
                        if (settings.Settings.GoldbergCleanSteam)
                        {
                            _ = GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath());
                        }
                        return;
                    }

                    return;
                }
                else if (hasGreenLumaFeature)
                {
                    // since im using any folder method for Stealth Mode, it doesnt leave any greenlumafiles in steam folder so we dont have to clean any greenlumafiles.
                    if (greenLumaStealth || greenLumaFamily)
                    {
                        return;
                    }
                    else if (greenLumaNormal && settings.Settings.CleanGreenLuma && GreenLuma.IsGreenLumaPresentOnSteamDir())
                    {
                        string notificationIdSteamRunning = Id.ToString() + "Clean GreenLuma with Steam running";
                        string notificationIdClean = Id.ToString() + "Clean GreenLuma";
                        NotificationMessage notificationMessageFinish = new NotificationMessage(notificationIdClean,
                                    ResourceProvider.GetString("LOCSEU_GreenLumaCleanTaskFinish"), NotificationType.Info);

                        if (Steam.IsSteamRunning())
                        {
                            PlayniteApi.Notifications.Add(new NotificationMessage(notificationIdSteamRunning, ResourceProvider.GetString("LOCSEU_SteamIsRunningNotification"),
                                NotificationType.Info));
                            PlayniteApi.Notifications.Remove(notificationIdClean);
                        }
                        switch (settings.Settings.CleanMode)
                        {
                            case 0:
                                Steam.KillSteam();
                                await GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath());
                                PlayniteApi.Notifications.Add(notificationMessageFinish);
                                PlayniteApi.Notifications.Remove(notificationIdSteamRunning);
                                break;
                            case 1:
                                await GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath());
                                PlayniteApi.Notifications.Add(notificationMessageFinish);
                                PlayniteApi.Notifications.Remove(notificationIdSteamRunning);
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
                    Task.Run(() => GoldbergTasks.CheckForUpdate(PlayniteApi, settings, this));
                }
            }
            if (settings.Settings.CheckGreenLumaUpdate)
            {
                if (InternetCommon.Internet.IsInternetAvailable())
                {
                    Task.Run(() => GreenLumaTasks.CheckForUpdate(PlayniteApi, this));
                }
            }
            if (settings.Settings.CleanGreenLumaStartup && GreenLuma.IsGreenLumaPresentOnSteamDir())
            {
                if (Steam.IsSteamRunning())
                {
                    string notificationIdSteamRunning = Id.ToString() + "Clean GreenLuma with Steam running";
                    PlayniteApi.Notifications.Add(new NotificationMessage(notificationIdSteamRunning, ResourceProvider.GetString("LOCSEU_SteamIsRunningNotification"),
                    NotificationType.Info));
                    GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath()).ContinueWith(t => { PlayniteApi.Notifications.Remove(notificationIdSteamRunning); });
                }
                else
                {
                    string notificationIdSteamRunning = Id.ToString() + "Clean GreenLuma with Steam running";
                    GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath()).ContinueWith(t => { PlayniteApi.Notifications.Remove(notificationIdSteamRunning); });
                }
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