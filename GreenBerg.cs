using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using GreenBerg.Models.Steam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GreenBerg
{
    public class GreenBerg : GenericPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GreenBergSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("a237961d-d688-4be9-9576-fb635547f854");

        public GreenBerg(IPlayniteAPI api) : base(api)
        {
            settings = new GreenBergSettingsViewModel(this);
            Properties = new GenericPluginProperties
            {
                HasSettings = true
            };
        }

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            yield return new GameMenuItem
            {
                Description = "Tes",
                Action = (a) =>
                {
                    foreach (Game game in a.Games)
                    {
                        if (Steam.IsGameSteamGame(game))
                        {
                            PlayniteApi.Dialogs.ShowMessage("Betol anjay");
                        }
                        else
                        {
                            PlayniteApi.Dialogs.ShowMessage("Salah Anjay");
                        }
                    }
                }
            };
        }

        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            yield return new MainMenuItem
            {
                Description = "GreenBerg",
                Action = (a) => 
                {
                    PlayniteApi.Dialogs.ShowMessage("mawi");
                    Console.WriteLine("Invoked from main menu item!");
                }
            };
        }

        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(OnGameStartedEventArgs args)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            // Add code to be executed when library is updated.
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GreenBergSettingsView();
        }
    }
}