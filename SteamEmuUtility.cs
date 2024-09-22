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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static GreenLumaCommon.GreenLuma;

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

            bool goldberg = PlayniteUtilities.HasFeature(game, Goldberg.GoldbergFeature);
            bool greenLumaStealth = PlayniteUtilities.HasFeature(game, StealthModeFeature);
            bool greenLumaFamily = PlayniteUtilities.HasFeature(game, FamilySharingModeFeatuere);
            bool greenLumaNormal = PlayniteUtilities.HasFeature(game, NormalModeFeature);
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
            yield return new MainMenuItem
            {
                Description = ResourceProvider.GetString("LOCSEU_MenuUnlockAll"),
                MenuSection = "Steam Emu Utility",
                Action = (a) =>
                {
                    var SteamGame = PlayniteApi.Database.Games.Where(x => x.IsInstalled && Steam.IsGameSteamGame(x));

                    IEnumerable<string> appids = SteamGame.Select(x => x.GameId).ToList();
                    string pluginPath = GetPluginUserDataPath();
                    string pluginGreenLumaPath = Path.Combine(pluginPath, "GreenLuma");

                    string regexPattern = string.Join("|", AchievementRegex, GreenLumaDLL86Regex, GreenLumaDLL64Regex, InjectorRegex, X64launcherRegex, FamilyRegex);
                    IEnumerable<FileInfo> greenlumaFiles = FileSystem.GetFiles(pluginGreenLumaPath,
                                                                               regexPattern,
                                                                               RegexOptions.IgnoreCase);
                    GreenLumaTasks.StartGreenLumaJob(PlayniteApi, appids, greenlumaFiles);
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
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaFamilySharing"), count), "Steam Emu Utility");
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
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaFamilySharing"), count), "Steam Emu Utility");
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
                        PlayniteApi.Dialogs.ShowMessage(string.Format(ResourceProvider.GetString("LOCSEU_AddGreenLumaFamilySharing"), count), "Steam Emu Utility");
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

            window.Height = 440;
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

            bool goldberg = PlayniteUtilities.HasFeature(game, Goldberg.GoldbergFeature);
            bool greenLumaStealth = PlayniteUtilities.HasFeature(game, StealthModeFeature);
            bool greenLumaFamily = PlayniteUtilities.HasFeature(game, FamilySharingModeFeatuere);
            bool greenLumaNormal = PlayniteUtilities.HasFeature(game, NormalModeFeature);
            bool greenLumaGameUnlocking = PlayniteUtilities.HasFeature(game, GameUnlockingFeature);
            bool greenLumaDLCUnlocking = PlayniteUtilities.HasFeature(game, DLCUnlockingFeature);
            bool hasGreenLumaFeature = greenLumaNormal || greenLumaStealth || greenLumaFamily;

            int glFeature = (greenLumaNormal ? 1 : 0) + (greenLumaStealth ? 1 : 0) + (greenLumaFamily ? 1 : 0);
            bool glFeatureInvalid = glFeature >= 2;

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
            // dont execute the next code if the game doesnt have any gl feature
            else if (!hasGreenLumaFeature)
            {
                return;
            }
            // return a dialog error if gl feature is invalid
            else if (glFeatureInvalid)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFeature"));
                args.CancelStartup = true;
                return;
            }


            string pluginPath = GetPluginUserDataPath();
            string pluginGreenLumaPath = Path.Combine(pluginPath, "GreenLuma");

            string regexPattern = string.Join("|", AchievementRegex, GreenLumaDLL86Regex, GreenLumaDLL64Regex, InjectorRegex, X64launcherRegex, FamilyRegex);
            IEnumerable<FileInfo> greenlumaFiles = FileSystem.GetFiles(pluginGreenLumaPath,
                                                                       regexPattern,
                                                                       RegexOptions.IgnoreCase);

            // if greenluma files doesnt exists in plugin userdata path then return.
            if (greenLumaNormal)
            {
                if (!GreenLumaFilesExists(greenlumaFiles, GreenLumaMode.Normal))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFileMissing"));
                    args.CancelStartup = true;
                    return;
                }
            }
            else if (greenLumaStealth)
            {
                if (!GreenLumaFilesExists(greenlumaFiles, GreenLumaMode.Stealth))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFileMissing"));
                    args.CancelStartup = true;
                    return;
                }
            }
            else if (greenLumaFamily)
            {
                if (!GreenLumaFilesExists(greenlumaFiles, GreenLumaMode.Family))
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(ResourceProvider.GetString("LOCSEU_GreenLumaErrorFileMissing"));
                    args.CancelStartup = true;
                    return;
                }
            }

            string appid = game.GameId;
            var appids = new List<string>();

            if (greenLumaGameUnlocking)
            {
                appids.Add(appid);
            }
            if (greenLumaDLCUnlocking)
            {
                if (!DlcManager.HasGameInfo(pluginPath, appid))
                {
                    using (var steam = new SteamService())
                    {
                        PlayniteApi.Dialogs.ActivateGlobalProgress((progress) =>
                        {
                            progress.Text = ResourceProvider.GetString("LOCSEU_GettingDlcInfo");

                            steam.action = callback =>
                            {
                                if (callback is string text)
                                {
                                    progress.Text = text;
                                }
                            };

                            progress.CancelToken.Register(() =>
                            {
                                steam.Dispose();
                                return;
                            });
                            GreenLumaGenerator.GenerateDLC(game, steam, progress, settings.Settings.SteamWebApi, pluginPath);
                        }, new GlobalProgressOptions("Steam Emu Utility", true));
                    }
                }
                if (DlcManager.HasDLC(pluginPath, appid))
                {
                    var appidsDLC = DlcManager.GetDLCAppid(pluginPath, appid);
                    if (!appidsDLC.Any())
                    {
                        if (PlayniteApi.Dialogs.ShowMessage(ResourceProvider.GetString("LOCSEU_NoDLCEnabled"),
                            ResourceProvider.GetString("LOCSEU_DLCUnlocker"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.No)
                        {
                            args.CancelStartup = true;
                            return;
                        }
                    }
                    else
                    {
                appids.AddRange(DlcManager.GetDLCAppid(pluginPath, appid));
            }
                }
            }

            // abort if appids 0, since its useless to use greenluma with 0 appids
            if (appids.Count == 0)
            {
                return;
            }

            if (greenLumaNormal)
            {
                GreenLumaTasks.StartGreenLumaJob(args, PlayniteApi, appids, GreenLumaMode.Normal, greenlumaFiles);
            }
            else if (greenLumaStealth)
            {
                GreenLumaTasks.StartGreenLumaJob(args, PlayniteApi, appids, GreenLumaMode.Stealth, greenlumaFiles);
            }
            else if (greenLumaFamily)
            {
                GreenLumaTasks.StartGreenLumaJob(args, PlayniteApi, appids, GreenLumaMode.Family, greenlumaFiles);
            }
        }
        private async void CommonGameShutdownLogic(Game game)
        {
            if (game.Features != null)
            {
                bool goldberg = PlayniteUtilities.HasFeature(game, Goldberg.GoldbergFeature);
                bool greenLumaStealth = PlayniteUtilities.HasFeature(game, StealthModeFeature);
                bool greenLumaFamily = PlayniteUtilities.HasFeature(game, FamilySharingModeFeatuere);
                bool greenLumaNormal = PlayniteUtilities.HasFeature(game, NormalModeFeature);
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

                        string pluginGreenLumaPath = Path.Combine(pluginPath, "GreenLuma");

                        string regexPattern = string.Join("|", AchievementRegex, GreenLumaDLL86Regex, GreenLumaDLL64Regex, InjectorRegex, X64launcherRegex, FamilyRegex);
                        IEnumerable<FileInfo> greenlumaFiles = FileSystem.GetFiles(pluginGreenLumaPath,
                                                                                   regexPattern,
                                                                                   RegexOptions.IgnoreCase);

                        GreenLumaTasks.StartGreenLumaJob(PlayniteApi, new List<string> { game.GameId }, greenlumaFiles);
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
                    else if (greenLumaNormal && settings.Settings.CleanGreenLuma)
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

                        string steamDir = Steam.GetSteamDirectory();

                        var dirGreenLumaOnSteam = FileSystem.GetDirectories(steamDir, GreenLumaDirectoriesRegex, RegexOptions.IgnoreCase);

                        var fileGreenLumaOnSteam = FileSystem.GetFiles(steamDir, GreenLumaFilesRegex, RegexOptions.IgnoreCase);

                        switch (settings.Settings.CleanMode)
                        {
                            case 0:
                                Steam.KillSteam();
                                await GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath(), steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam);
                                PlayniteApi.Notifications.Add(notificationMessageFinish);
                                PlayniteApi.Notifications.Remove(notificationIdSteamRunning);
                                break;
                            case 1:
                                await GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath(), steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam);
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
            if (settings.Settings.CleanGreenLumaStartup)
            {
                string steamDir = Steam.GetSteamDirectory();

                if (!FileSystem.DirectoryExists(steamDir))
                {
                    return;
                }

                var dirGreenLumaOnSteam = FileSystem.GetDirectories(steamDir, GreenLumaDirectoriesRegex, RegexOptions.IgnoreCase);

                var fileGreenLumaOnSteam = FileSystem.GetFiles(steamDir, GreenLumaFilesRegex, RegexOptions.IgnoreCase);

                if (!dirGreenLumaOnSteam.Any() && !fileGreenLumaOnSteam.Any())
                {
                    return;
                }

                if (Steam.IsSteamRunning())
                {
                    string notificationIdSteamRunning = Id.ToString() + "Clean GreenLuma with Steam running";
                    PlayniteApi.Notifications.Add(new NotificationMessage(notificationIdSteamRunning, ResourceProvider.GetString("LOCSEU_SteamIsRunningNotification"),
                    NotificationType.Info));
                    GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath(), steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam)
                        .ContinueWith(t => { PlayniteApi.Notifications.Remove(notificationIdSteamRunning); });
                }
                else
                {
                    string notificationIdSteamRunning = Id.ToString() + "Clean GreenLuma with Steam running";
                    GreenLumaTasks.CleanAfterSteamExit(GetPluginUserDataPath(), steamDir, dirGreenLumaOnSteam, fileGreenLumaOnSteam)
                        .ContinueWith(t => { PlayniteApi.Notifications.Remove(notificationIdSteamRunning); });
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