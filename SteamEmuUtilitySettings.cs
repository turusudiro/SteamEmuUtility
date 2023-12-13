using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GoldbergCommon;
using GreenLumaCommon;
using Playnite.SDK;
using Playnite.SDK.Data;
using SevenZip;

namespace SteamEmuUtility
{
    public class SteamEmuUtilitySettings : ObservableObject
    {
        [DontSerialize]
        private BitmapImage _avatarimage;
        [DontSerialize]
        public BitmapImage AvatarImage
        {
            get
            {
                if (_avatarimage != null)
                {
                    return _avatarimage;
                }

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                if (File.Exists(Goldberg.GoldbergAvatar))
                {
                    image.UriSource = new Uri(Goldberg.GoldbergAvatar);
                }
                else
                {
                    // Create a "?" symbol image
                    DrawingVisual drawingVisual = new DrawingVisual();
                    using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                    {
                        Typeface typeface = new Typeface("Arial");
                        FormattedText formattedText = new FormattedText(
                            "?",
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            typeface,
                            24, // Font size
                            Brushes.Black);

                        double textX = formattedText.Width / 2;
                        double textY = formattedText.Height / 2;

                        drawingContext.DrawText(formattedText, new Point(textX, textY));
                    }

                    RenderTargetBitmap bmp = new RenderTargetBitmap(50, 50, 96, 96, PixelFormats.Pbgra32);
                    bmp.Render(drawingVisual);

                    image = new BitmapImage();
                    using (MemoryStream stream = new MemoryStream())
                    {
                        PngBitmapEncoder encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bmp));
                        encoder.Save(stream);
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                    }
                }
                image.EndInit();
                _avatarimage = image;
                return image;
            }
            set
            {
                _avatarimage = value;
                OnPropertyChanged();
            }
        }

        [DontSerialize]
        public string GoldbergAccountName
        {
            get => Goldberg.AccountName;
            set
            {
                Goldberg.AccountName = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        public string GoldbergLanguage
        {
            get => Goldberg.Language;
            set
            {
                Goldberg.Language = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        public string GoldbergListenPort
        {
            get => Goldberg.ListenPort;
            set
            {
                Goldberg.ListenPort = value;
                OnPropertyChanged();
            }
        }
        [DontSerialize]
        public string GoldbergUserSteamID
        {
            get => Goldberg.UserSteamID;
            set
            {
                Goldberg.UserSteamID = value;
                OnPropertyChanged();
            }
        }
        private int cleanmode = 0;
        public int CleanMode
        {
            get => cleanmode;
            set
            {
                cleanmode = value;
                OnPropertyChanged();
            }
        }
        private bool goldbergcleansteam = true;
        public bool GoldbergCleanSteam
        {
            get => goldbergcleansteam;
            set
            {
                goldbergcleansteam = value;
                OnPropertyChanged();
            }
        }
        private string steamwebapi;
        public string SteamWebApi
        {
            get => steamwebapi;
            set
            {
                steamwebapi = value;
                OnPropertyChanged();
            }
        }
        private string steamargs;
        public string SteamArgs
        {
            get => steamargs;
            set
            {
                steamargs = value;
                OnPropertyChanged();
            }
        }
        private bool enablesteamargs = false;
        public bool EnableSteamArgs
        {
            get => enablesteamargs;
            set
            {
                enablesteamargs = value;
                OnPropertyChanged();
            }
        }

        private bool symboliclinkappdata = false;
        public bool SymbolicLinkAppdata
        {
            get => symboliclinkappdata;
            set
            {
                symboliclinkappdata = value;
                OnPropertyChanged();
            }
        }
        private bool opensteamafterexit = false;
        public bool OpenSteamAfterExit
        {
            get => opensteamafterexit;
            set
            {
                opensteamafterexit = value;
                OnPropertyChanged();
            }
        }
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
        private bool cleangreenlumastartup = true;
        public bool CleanGreenLumaStartup
        {
            get => cleangreenlumastartup;
            set
            {
                cleangreenluma = value;
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
        [DontSerialize]
        public List<string> MissingGoldbergFiles
        {
            get
            {
                return Goldberg.ColdClientExists(out List<string> missingFiles) ? null : missingFiles;
            }
        }
        private bool goldbergnotexists;
        [DontSerialize]
        public bool GoldbergNotExists
        {
            get
            {
                return !Goldberg.ColdClientExists(out _);
            }
            set
            {
                goldbergnotexists = value;
                OnPropertyChanged();
            }
        }
        private SolidColorBrush colorgoldbergstatus;
        [DontSerialize]
        public SolidColorBrush ColorGoldbergStatus
        {
            get
            {
                return new SolidColorBrush(Goldberg.ColdClientExists(out _) ? Brushes.LimeGreen.Color : Brushes.IndianRed.Color);
            }
            set
            {
                colorgoldbergstatus = value;
                OnPropertyChanged();
            }
        }
        private string goldbergstatus;
        [DontSerialize]
        public string GoldbergStatus
        {
            get
            {
                return Goldberg.ColdClientExists(out _) ? "Active" : "Missing";
            }
            set
            {
                goldbergstatus = value;
                OnPropertyChanged();
            }
        }
        public List<string> MissingGreenLumaFiles
        {
            get
            {
                return GreenLuma.GreenLumaFilesExists(out List<string> missingFiles) ? null : missingFiles;
            }
        }
        private bool greenlumanotexists;
        [DontSerialize]
        public bool GreenLumaNotExists
        {
            get
            {
                return !GreenLuma.GreenLumaFilesExists(out _);
            }
            set
            {
                greenlumanotexists = value;
                OnPropertyChanged();
            }
        }
        private string greenlumastatus;
        [DontSerialize]
        public string GreenLumaStatus
        {
            get
            {
                return GreenLuma.GreenLumaFilesExists(out _) ? "Active" : "Missing";
            }
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
            get
            {
                return new SolidColorBrush(GreenLuma.GreenLumaFilesExists(out _) ? Brushes.LimeGreen.Color : Brushes.IndianRed.Color);
            }
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

        private static string SevenZipLib { get { return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Libraries", "7z"); } }
        public RelayCommand<object> ChangeAvatar
        {
            get => new RelayCommand<object>((a) =>
            {
                OnPropertyChanged(nameof(Settings.AvatarImage));
                var path = plugin.PlayniteApi.Dialogs.SelectFile("Image Files (*.bmp, *.jpg, *.png, *.gif)|*.bmp;*.jpg;*.png;*.gif");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    try
                    {
                        if (!Directory.Exists(Path.Combine(Goldberg.GoldbergAppData, "settings")))
                        {
                            Directory.CreateDirectory(Path.Combine(Goldberg.GoldbergAppData, "settings"));
                        }
                        string[] files = Directory.GetFiles(Path.Combine(Goldberg.GoldbergAppData, "settings"), $"account_avatar.*");

                        if (files.Length > 0)
                        {
                            // If multiple files match, you may want to choose one based on your criteria
                            // In this example, the first matching file is selected
                            File.Delete(files[0]);
                        }
                        File.Copy(path, Path.Combine(Goldberg.GoldbergAppData, "settings", $"account_avatar{Path.GetExtension(path)}"), true);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            BitmapImage image = new BitmapImage();
                            image.BeginInit();
                            image.CacheOption = BitmapCacheOption.OnLoad;
                            image.UriSource = new Uri(path);
                            image.EndInit();

                            // Update the property on the UI thread
                            Settings.AvatarImage = image;
                        });
                    }
                    catch { }

                }, progress);
            });
        }
        public RelayCommand<object> ImportGreenLuma
        {
            get => new RelayCommand<object>((a) =>
            {
                var path = plugin.PlayniteApi.Dialogs.SelectFile(@"GreenLuma_2023_X.X.X-Steam006.zip Files|*.zip");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    ExtractGreenLumaFiles(path, GreenLuma.GreenLumaPath);
                }, progress);
            });
        }
        public RelayCommand<object> ImportGoldberg
        {
            get => new RelayCommand<object>((a) =>
            {
                var path = plugin.PlayniteApi.Dialogs.SelectFile(@"GoldbergSteamEmu (*.7z;*.zip) Files|*.zip;*.7z");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    ExtractGoldbergFiles(path, Goldberg.GoldbergPath);
                }, progress);
            });
        }
        void ExtractGreenLumaFiles(string archivePath, string destinationFolder)
        {
            ExtractGreenLumaFiles(archivePath, destinationFolder, null);
        }
        void ExtractGreenLumaFiles(string archivePath, string destinationFolder, string password)
        {
            SevenZipBase.SetLibraryPath(Path.Combine(SevenZipLib, Environment.Is64BitProcess ? "x64" : "x86", "7z.dll"));
            using (var extractor = new SevenZipExtractor(archivePath, password))
            {
                List<string> gl = new List<string>
                {
                    "x64launcher.exe",
                    "AchievementUnlocked.wav",
                    "DLLInjector.exe",
                    "GreenLuma_2023_x64.dll",
                    "GreenLuma_2023_x86.dll",
                    "User32.dll"
                };
                var tes = extractor.ArchiveFileData.Where(x => gl.Any(file => x.FileName.Contains(file)));
                foreach (var item in tes)
                {
                    if (item.IsDirectory)
                    {
                        continue;
                    }
                    /// check if file is encrypted and user is not set the password
                    if (item.Encrypted && string.IsNullOrEmpty(password))
                    {
                        var result = plugin.PlayniteApi.Dialogs.SelectString("File is password-protected, Please enter the password", "Error", "");
                        if (result.Result)
                        {
                            ExtractGreenLumaFiles(archivePath, destinationFolder, result.SelectedString);
                            return;
                        }
                        return;
                    }
                    //try extracting files
                    if (item.FileName.Contains("Normal"))
                    {
                        if (!Directory.Exists(Path.Combine(destinationFolder, "NormalMode")))
                        {
                            Directory.CreateDirectory(Path.Combine(destinationFolder, "NormalMode"));
                        }
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, "NormalMode", Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                    }
                    else
                    {
                        if (!Directory.Exists(Path.Combine(destinationFolder, "StealthMode")))
                        {
                            Directory.CreateDirectory(Path.Combine(destinationFolder, "StealthMode"));
                        }
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, "StealthMode", Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                    }
                    //check if password is wrong, if wrong the extracted files will 0 bytes and delete it and ask user to reenter password
                    if (File.Exists(Path.Combine(destinationFolder, Path.GetFileName(item.FileName))))
                    {
                        FileInfo fileInfo = new FileInfo(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)));
                        if (fileInfo.Length == 0)
                        {
                            fileInfo.Delete();
                            var result = plugin.PlayniteApi.Dialogs.SelectString("Wrong Password, Please re-enter the password", "Error", "");
                            if (result.Result)
                            {
                                ExtractGreenLumaFiles(archivePath, destinationFolder, result.SelectedString);
                                return;
                            }
                            return;
                        }
                    }
                }
            }
            Settings.GreenLumaNotExists = !GreenLuma.GreenLumaFilesExists(out _);
            Settings.GreenLumaStatus = GreenLuma.GreenLumaFilesExists(out _) ? "Active" : "Missing";
            Settings.ColorGreenLumaStatus = new SolidColorBrush(GreenLuma.GreenLumaFilesExists(out _) ? Brushes.LimeGreen.Color : Brushes.IndianRed.Color);
        }
        void ExtractGoldbergFiles(string archivePath, string destinationFolder)
        {
            ExtractGoldbergFiles(archivePath, destinationFolder, null);
        }
        void ExtractGoldbergFiles(string archivePath, string destinationFolder, string password)
        {
            string regexPattern = @"\\experimental_steamclient\\";
            SevenZipBase.SetLibraryPath(Path.Combine(SevenZipLib, Environment.Is64BitProcess ? "x64" : "x86", "7z.dll"));
            using (var extractor = new SevenZipExtractor(archivePath, password))
            {
                foreach (var item in extractor.ArchiveFileData)
                {
                    if (item.IsDirectory)
                    {
                        continue;
                    }
                    if (item.Encrypted && string.IsNullOrEmpty(password))
                    {
                        var result = plugin.PlayniteApi.Dialogs.SelectString("File is password-protected, Please enter the password", "Error", "");
                        if (result.Result)
                        {
                            ExtractGreenLumaFiles(archivePath, destinationFolder, result.SelectedString);
                            return;
                        }
                        return;
                    }
                    if (Regex.IsMatch(item.FileName, regexPattern))
                    {
                        if (!Directory.Exists(destinationFolder))
                        {
                            Directory.CreateDirectory(destinationFolder);
                        }
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                        if (File.Exists(Path.Combine(destinationFolder, Path.GetFileName(item.FileName))))
                        {
                            FileInfo fileInfo = new FileInfo(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)));
                            if (fileInfo.Length == 0)
                            {
                                fileInfo.Delete();
                                var result = plugin.PlayniteApi.Dialogs.SelectString("Wrong Password, Please re-enter the password", "Error", "");
                                if (result.Result)
                                {
                                    ExtractGreenLumaFiles(archivePath, destinationFolder, result.SelectedString);
                                    return;
                                }
                                return;
                            }
                        }
                    }
                }
            }
            Settings.GoldbergNotExists = Goldberg.ColdClientExists(out _);
            Settings.GoldbergStatus = Goldberg.ColdClientExists(out _) ? "Active" : "Missing";
            Settings.ColorGoldbergStatus = new SolidColorBrush(Goldberg.ColdClientExists(out _) ? Brushes.LimeGreen.Color : Brushes.IndianRed.Color);
        }
    }
}