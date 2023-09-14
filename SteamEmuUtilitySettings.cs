using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Playnite.SDK;
using Playnite.SDK.Data;
using SteamEmuUtility.Common;

namespace SteamEmuUtility
{
    public class SteamEmuUtilitySettings : ObservableObject
    {
        private string user32loaded = "Loaded!";
        private SolidColorBrush coloruser32 { get; set; } = new SolidColorBrush(Brushes.LimeGreen.Color);

        [DontSerialize]
        public string User32Loaded
        {
            get => user32loaded;
            set
            {
                user32loaded = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        public SolidColorBrush ColorUser32
        {
            get => coloruser32;
            set
            {
                coloruser32 = value;
                OnPropertyChanged();
            }
        }
    }

    public class SteamEmuUtilitySettingsViewModel : ObservableObject, ISettings
    {
        private readonly SteamEmuUtility plugin;
        private SteamEmuUtilitySettings editingClone { get; set; }

        private SteamEmuUtilitySettings settings;
        public SteamEmuUtilitySettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public SteamEmuUtilitySettingsViewModel(SteamEmuUtility plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            this.plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SteamEmuUtilitySettings>();

            // LoadPluginSettings returns null if no saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new SteamEmuUtilitySettings();
            }
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            if (!File.Exists(Path.Combine(plugin.GetPluginUserDataPath(), GreenLumaCommon.user32)))
            {
                settings.User32Loaded = "Not Found!";
                settings.ColorUser32.Color = Brushes.Red.Color;
            }
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            Settings = editingClone;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }

        public RelayCommand<object> BrowseUser32
        {
            get => new RelayCommand<object>((a) =>
            {
                var file = plugin.PlayniteApi.Dialogs.SelectFile("User32|User32.dll");
                try
                {
                    File.Copy(file, Path.Combine(plugin.GetPluginUserDataPath(), "User32.dll"), true);
                    settings.User32Loaded = "Loaded!";
                    settings.ColorUser32.Color = Brushes.LimeGreen.Color;
                }
                catch (Exception)
                {
                    return;
                }
            });
        }
    }
}