using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using GreenLumaCommon;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Playnite.SDK;
using Playnite.SDK.Data;

namespace SteamEmuUtility
{
    public class SteamEmuUtilitySettings : ObservableObject
    {
        private int maxattemptdllinjector = 0;
        public int MaxAttemptDLLInjector
        {
            get => maxattemptdllinjector;
            set
            {
                maxattemptdllinjector = value;
                OnPropertyChanged();
            }
        }

        private int millisecondstowait = 1000;
        public int MillisecondsToWait
        {
            get => millisecondstowait;
            set
            {
                millisecondstowait = value;
                OnPropertyChanged();
            }
        }

        private bool cleangreenluma = true;
        public bool CleanGreenLuma
        {
            get => cleangreenluma;
            set
            {
                cleangreenluma = value;
                OnPropertyChanged();
            }
        }

        private bool injectappownership = false;
        public bool InjectAppOwnership
        {
            get => injectappownership;
            set
            {
                injectappownership = value;
                OnPropertyChanged();
            }
        }

        private bool injectencryptedapp = false;
        public bool InjectEncryptedApp
        {
            get => injectencryptedapp;
            set
            {
                injectencryptedapp = value;
                OnPropertyChanged();
            }
        }

        private bool skipupdatestealth = false;
        public bool SkipUpdateStealth
        {
            get => skipupdatestealth;
            set
            {
                skipupdatestealth = value;
                OnPropertyChanged();
            }
        }

        private bool closesteamonexit = true;
        public bool CloseSteamOnExit
        {
            get => closesteamonexit;
            set
            {
                closesteamonexit = value;
                OnPropertyChanged();
            }
        }

        private bool cleanappcache = false;
        public bool CleanAppCache
        {
            get => cleanappcache;
            set
            {
                cleanappcache = value;
                OnPropertyChanged();
            }
        }

        private string greenlumastatus = "Active!";
        [DontSerialize]
        public string GreenLumaStatus
        {
            get => greenlumastatus;
            set
            {
                greenlumastatus = value;
                OnPropertyChanged();
            }
        }

        private SolidColorBrush colorgreenlumastatus { get; set; } = new SolidColorBrush(Brushes.LimeGreen.Color);
        [DontSerialize]
        public SolidColorBrush ColorGreenLumaStatus
        {
            get => colorgreenlumastatus;
            set
            {
                colorgreenlumastatus = value;
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
            Dictionary<string, bool> fileExistenceDictionary = new Dictionary<string, bool>();
            foreach (var fileName in GreenLuma.GreenLumaNormalModeFiles)
            {
                fileExistenceDictionary[fileName] = File.Exists(Path.Combine(plugin.GetPluginUserDataPath(), "GreenLuma\\NormalMode", fileName));
            }
            // check if all GreenLuma files is exists when opening settings
            if (fileExistenceDictionary.ContainsValue(false))
            {
                settings.GreenLumaStatus = "Not Found/Incomplete files!";
                settings.ColorGreenLumaStatus.Color = Brushes.Red.Color;
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

        public RelayCommand<object> BrowseGreenLuma
        {
            get => new RelayCommand<object>((a) =>
            {
                var path = plugin.PlayniteApi.Dialogs.SelectFile(@"GreenLuma_2023_X.X.X-Steam006.zip Files|*.zip");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                bool passwordCorrect = false;
                string password = string.Empty;
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    while (!passwordCorrect)
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                            using (ZipFile zf = new ZipFile(fs))
                            {
                                // Attempt to extract with the provided password
                                zf.Password = password; // Set the password
                                global.ProgressMaxValue = zf.Count;
                                foreach (ZipEntry zipEntry in zf)
                                {
                                    global.Text = $"Extracting {zipEntry.Name}";
                                    global.CurrentProgressValue = +1;
                                    if (!zipEntry.IsFile)
                                    {
                                        continue;
                                    }
                                    string entryFileName = zipEntry.Name;
                                    byte[] buffer = new byte[4096];
                                    Stream zipStream = zf.GetInputStream(zipEntry);

                                    string fullZipToPath = Path.Combine(plugin.GetPluginUserDataPath(), "GreenLuma", entryFileName);
                                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                                    if (directoryName.Length > 0)
                                    {
                                        Directory.CreateDirectory(directoryName);
                                    }

                                    using (FileStream streamWriter = File.Create(fullZipToPath))
                                    {
                                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                                    }
                                }
                                plugin.PlayniteApi.Dialogs.ShowMessage("Extraction Complete", "Steam Emu Utility", MessageBoxButton.OK, MessageBoxImage.Information);
                                Console.WriteLine("ZIP file extracted successfully.");

                                passwordCorrect = true;
                            }
                        }
                        catch (ZipException ex)
                        {
                            if (ex.Message.Contains("Password incorrect") || ex.Message.Contains("Invalid password"))
                            {
                                var result = plugin.PlayniteApi.Dialogs.SelectString("File is password-protected, Please enter the password", "Error", "");
                                if (result.Result)
                                {
                                    password = result.SelectedString;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                plugin.PlayniteApi.Dialogs.ShowErrorMessage("ZIP extraction failed with an unknown error: " + ex.Message);
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            plugin.PlayniteApi.Dialogs.ShowErrorMessage("An error occurred: " + ex.Message);
                            break;
                        }
                    }
                }, progress);
                Dictionary<string, bool> fileExistenceDictionary = new Dictionary<string, bool>();
                foreach (var fileName in GreenLuma.GreenLumaNormalModeFiles)
                {
                    fileExistenceDictionary[fileName] = File.Exists(Path.Combine(plugin.GetPluginUserDataPath(), "GreenLuma\\NormalMode", fileName));
                }
                if (fileExistenceDictionary.ContainsValue(true))
                {
                    settings.GreenLumaStatus = "Ready!";
                    settings.ColorGreenLumaStatus.Color = Brushes.LimeGreen.Color;
                }
            });
        }
    }
}