using DownloaderCommon;
using GoldbergCommon;
using GoldbergCommon.Models;
using GreenLumaCommon;
using ImageCommon;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using PluginsCommon;
using ProcessCommon;
using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SteamEmuUtility
{
    public class SteamEmuUtilitySettings : ObservableObject
    {
        private BitmapImage _avatarimage;
        [DontSerialize]
        public BitmapImage AvatarImage
        {
            get => _avatarimage;
            set
            {
                _avatarimage = value;
                OnPropertyChanged();
            }
        }

        private bool _goldbergenableaccountavatar;
        [DontSerialize]
        public bool GoldbergEnableAccountAvatar
        {
            get => _goldbergenableaccountavatar;
            set
            {
                _goldbergenableaccountavatar = value;
                OnPropertyChanged();
            }
        }

        private string _goldbergaccountname;
        [DontSerialize]
        public string GoldbergAccountName
        {
            get => _goldbergaccountname;
            set
            {
                _goldbergaccountname = value;
                OnPropertyChanged();
            }
        }

        private string _goldberglanguage;
        [DontSerialize]
        public string GoldbergLanguage
        {
            get => _goldberglanguage;
            set
            {
                _goldberglanguage = value;
                OnPropertyChanged();
            }
        }
        private string _goldberglistenport;
        [DontSerialize]
        public string GoldbergListenPort
        {
            get => _goldberglistenport;
            set
            {
                _goldberglistenport = value;
                OnPropertyChanged();
            }
        }
        private string _goldbergcustombroadcasts;
        [DontSerialize]
        public string GoldbergCustomBroadcasts
        {
            get => _goldbergcustombroadcasts;
            set
            {
                _goldbergcustombroadcasts = value;
                OnPropertyChanged();
            }
        }
        private string _goldbergusersteamid;
        [DontSerialize]
        public string GoldbergUserSteamID
        {
            get => _goldbergusersteamid;
            set
            {
                _goldbergusersteamid = value;
                OnPropertyChanged();
            }
        }
        private string _goldbergcountryip;
        [DontSerialize]
        public string GoldbergCountryIP
        {
            get => _goldbergcountryip;
            set
            {
                _goldbergcountryip = value;
                OnPropertyChanged();
            }
        }
        private bool goldbergoverride;
        public bool GoldbergOverride
        {
            get => goldbergoverride;
            set
            {
                goldbergoverride = value;
                OnPropertyChanged();
            }
        }
        private bool checkgreenlumaupdate;
        public bool CheckGreenLumaUpdate
        {
            get => checkgreenlumaupdate;
            set
            {
                checkgreenlumaupdate = value;
                OnPropertyChanged();
            }
        }
        private bool cleanapplist = true;
        public bool CleanApplist
        {
            get => cleanapplist;
            set
            {
                cleanapplist = value;
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
        private bool enablesteamargs;
        public bool EnableSteamArgs
        {
            get => enablesteamargs;
            set
            {
                enablesteamargs = value;
                OnPropertyChanged();
            }
        }
        private bool opensteamafterexit;
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
                OnPropertyChanged(nameof(MillisecondsToWaitText));
            }
        }
        [DontSerialize]
        public string MillisecondsToWaitText
        {
            get { return string.Format(ResourceProvider.GetString("LOCSEU_GLDelay"), MillisecondsToWait); }
        }
        private bool cleangreenlumastartup;
        public bool CleanGreenLumaStartup
        {
            get => cleangreenlumastartup;
            set
            {
                cleangreenlumastartup = value;
                OnPropertyChanged();
            }
        }
        private bool cleangreenluma;
        public bool CleanGreenLuma
        {
            get => cleangreenluma;
            set
            {
                cleangreenluma = value;
                OnPropertyChanged();
            }
        }

        private bool injectappownership;
        public bool InjectAppOwnership
        {
            get => injectappownership;
            set
            {
                injectappownership = value;
                OnPropertyChanged();
            }
        }

        private bool injectencryptedapp;
        public bool InjectEncryptedApp
        {
            get => injectencryptedapp;
            set
            {
                injectencryptedapp = value;
                OnPropertyChanged();
            }
        }

        private bool skipupdatefamily;
        public bool SkipUpdateFamily
        {
            get => skipupdatefamily;
            set
            {
                skipupdatefamily = value;
                OnPropertyChanged();
            }
        }
        private bool skipupdatestealth;
        public bool SkipUpdateStealth
        {
            get => skipupdatestealth;
            set
            {
                skipupdatestealth = value;
                OnPropertyChanged();
            }
        }
        private bool cleanappcache;
        public bool CleanAppCache
        {
            get => cleanappcache;
            set
            {
                cleanappcache = value;
                OnPropertyChanged();
            }
        }
        private IEnumerable<string> missinggoldbergfiles;
        [DontSerialize]
        public IEnumerable<string> MissingGoldbergFiles
        {
            get => missinggoldbergfiles;
            set
            {
                missinggoldbergfiles = value;
                OnPropertyChanged();
            }
        }
        private bool goldbergnotexists;
        [DontSerialize]
        public bool GoldbergNotExists
        {
            get => goldbergnotexists;
            set
            {
                goldbergnotexists = value;
                OnPropertyChanged();
            }
        }
        private bool goldbergready;
        [DontSerialize]
        public bool GoldbergReady
        {
            get => goldbergready;
            set
            {
                goldbergready = value;
                OnPropertyChanged();
            }
        }
        private string goldbergstatus;
        [DontSerialize]
        public string GoldbergStatus
        {
            get => goldbergstatus;
            set
            {
                goldbergstatus = value;
                OnPropertyChanged();
            }
        }
        private bool greenlumaready;
        [DontSerialize]
        public bool GreenLumaReady
        {
            get => greenlumaready;
            set
            {
                greenlumaready = value;
                OnPropertyChanged();
            }
        }
        private IEnumerable<string> missinggreenlumaFiles;
        [DontSerialize]
        public IEnumerable<string> MissingGreenLumaFiles
        {
            get => missinggreenlumaFiles;
            set
            {
                missinggreenlumaFiles = value;
                OnPropertyChanged();
            }
        }
        private bool greenlumanotexists;
        [DontSerialize]
        public bool GreenLumaNotExists
        {
            get => greenlumanotexists;
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
            get => greenlumastatus;
            set
            {
                greenlumastatus = value;
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
            CheckFiles();
            GetGoldbergConfigs();
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
            UpdateConfigs();
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

        private void UpdateConfigs()
        {
            string goldbergPath = Goldberg.GetGoldbergAppData();
            string goldbergSettingsPath = Path.Combine(goldbergPath, "settings");

            ConfigsMain configsMain = new ConfigsMain(goldbergSettingsPath);
            configsMain.EnableAccountAvatar = Settings.GoldbergEnableAccountAvatar;
            configsMain.Listen_Port = Settings.GoldbergListenPort;

            ConfigsUser configsUser = new ConfigsUser(goldbergSettingsPath);
            configsUser.AccountName = Settings.GoldbergAccountName;
            configsUser.ID = Settings.GoldbergUserSteamID;
            configsUser.IP = Settings.GoldbergCountryIP;
            configsUser.Language = Settings.GoldbergLanguage;
        }

        private void CheckFiles()
        {
            string pluginPath = plugin.GetPluginUserDataPath();
            string pluginGoldbergPath = Path.Combine(pluginPath, "Goldberg");
            string pluginGreenLumaPath = Path.Combine(pluginPath, "GreenLuma");

            bool glExists = GreenLuma.GreenLumaFilesExists(pluginGreenLumaPath, out List<string> missingFilesGL);

            Settings.MissingGreenLumaFiles = glExists ? null : missingFilesGL;
            Settings.GreenLumaNotExists = !glExists;
            Settings.GreenLumaReady = glExists;
            Settings.GreenLumaStatus = glExists
               ? ResourceProvider.GetString("LOCSEU_Active") : ResourceProvider.GetString("LOCSEU_Missing");
            Settings.OnPropertyChanged(nameof(Settings.GreenLumaNotExists));



            bool gbExists = Goldberg.ColdClientExists(pluginGoldbergPath, out List<string> missingFilesGB);

            Settings.MissingGoldbergFiles = gbExists ? null : missingFilesGB;
            Settings.GoldbergNotExists = !gbExists;
            Settings.GoldbergReady = gbExists;
            Settings.GoldbergStatus = gbExists ? ResourceProvider.GetString("LOCSEU_Active") : ResourceProvider.GetString("LOCSEU_Missing");
        }

        private void GetGoldbergConfigs()
        {
            string goldbergPath = Goldberg.GetGoldbergAppData();
            string goldbergSettingsPath = Path.Combine(goldbergPath, "settings");

            ConfigsMain configsMain = new ConfigsMain(goldbergSettingsPath);
            Settings.GoldbergEnableAccountAvatar = configsMain.EnableAccountAvatar;
            Settings.GoldbergListenPort = configsMain.Listen_Port;

            ConfigsUser configsUser = new ConfigsUser(goldbergSettingsPath);
            Settings.GoldbergAccountName = configsUser.AccountName;
            Settings.GoldbergUserSteamID = configsUser.ID;
            Settings.GoldbergCountryIP = configsUser.IP;
            Settings.GoldbergLanguage = configsUser.Language;

            Settings.AvatarImage = GetGoldbergAvatar(goldbergPath);
        }

        private BitmapImage GetGoldbergAvatar(string goldbergPath)
        {
            BitmapImage image;
            try
            {
                string[] files = Directory.GetFiles(Path.Combine(goldbergPath, "settings"), $"account_avatar.*");
                string avatarPath = files[0];
                image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(avatarPath);
                image.EndInit();
            }
            // if there are no files or exception is throw
            catch
            {
                // Create a "?" symbol image
                image = ImageUtilites.CreateQuestionMarkImage(100, 100);
            }
            return image;
        }

        public RelayCommand<object> OpenURL
        {
            get => new RelayCommand<object>((url) =>
            {
                try
                {
                    NavigateUrl(url);
                }
                catch { }
            });
        }
        public static void NavigateUrl(object url)
        {
            if (url is string stringUrl)
            {
                NavigateUrl(stringUrl);
            }
            else if (url is Link linkUrl)
            {
                NavigateUrl(linkUrl.Url);
            }
            else if (url is Uri uriUrl)
            {
                NavigateUrl(uriUrl.OriginalString);
            }
            else
            {
                throw new Exception("Unsupported URL format.");
            }
        }
        public static void NavigateUrl(string url)
        {
            if (url.IsNullOrEmpty())
            {
                throw new Exception("No URL was given.");
            }

            if (!url.IsUri())
            {
                url = "http://" + url;
            }

            ProcessUtilities.StartUrl(url);
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
                        string goldbergPath = Goldberg.GetGoldbergAppData();
                        if (!FileSystem.DirectoryExists(Path.Combine(goldbergPath, "settings")))
                        {
                            FileSystem.CreateDirectory(Path.Combine(goldbergPath, "settings"));
                        }
                        string[] files = Directory.GetFiles(Path.Combine(goldbergPath, "settings"), $"account_avatar.*");

                        if (files.Length > 0)
                        {
                            FileSystem.DeleteFile(files[0]);
                        }
                        FileSystem.CopyFile(path, Path.Combine(goldbergPath, "settings", $"account_avatar{Path.GetExtension(path)}"), true);
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
                var path = plugin.PlayniteApi.Dialogs.SelectFile(@"GreenLuma_XXXX_X.X.X-Steam006.zip Files|*.zip");
                string glPath = Path.Combine(plugin.GetPluginUserDataPath(), "GreenLuma");

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    ExtractGreenLumaFiles(path, glPath);
                }, progress);

            });
        }
        public RelayCommand<object> ImportGoldberg
        {
            get => new RelayCommand<object>((a) =>
            {
                var path = plugin.PlayniteApi.Dialogs.SelectFile(@"GoldbergSteamEmu (*.7z;*.zip) Files|*.zip;*.7z");
                string gbPath = Path.Combine(plugin.GetPluginUserDataPath(), "Goldberg");

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
                GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
                plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
                {
                    ExtractGoldbergFiles(path, gbPath);
                    string changelogPath = Path.Combine(gbPath, "CHANGELOG.md");
                    if (FileSystem.FileExists(changelogPath))
                    {
                        string regexPatternDate = @"\d{4}[\W_][0-9]+[\W_][0-9]+";
                        string ver = FileSystem.ReadStringFromFile(changelogPath);
                        FileSystem.WriteStringToFile(Path.Combine(gbPath, "Version.txt"), Regex.Match(ver, regexPatternDate).Value, true, true);
                    }
                }, progress);
                CheckFiles();
            });
        }
        public void DownloadGoldberg(string pluginPath)
        {
            string gbPath = Path.Combine(plugin.GetPluginUserDataPath(), "Goldberg");
            string url = @"https://api.github.com/repos/otavepto/gbe_fork/releases/latest";
            string raw = HttpDownloader.DownloadString(url);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }
            GlobalProgressOptions progress = new GlobalProgressOptions("Steam Emu Utility");
            plugin.PlayniteApi.Dialogs.ActivateGlobalProgress((global) =>
            {
                dynamic json = Serialization.FromJson<object>(raw);
                foreach (var asset in json.assets)
                {
                    var name = asset.name;
                    if (name == "emu-win-release.7z")
                    {
                        string urlDownload = asset.browser_download_url;
                        string tempfilename = Path.GetTempFileName();
                        HttpDownloader.DownloadFile(urlDownload, tempfilename);
                        ExtractGoldbergFiles(tempfilename, gbPath);
                        FileSystem.DeleteFile(tempfilename);
                        DateTime date = json.published_at;
                        string ver = $"{date.Year}/{date.Month}/{date.Day}";
                        FileSystem.WriteStringToFile(Path.Combine(gbPath, "Version.txt"), ver, true, true);
                        break;
                    }
                }
            }, progress);
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
                var files = extractor.ArchiveFileData.Where(x => Regex.IsMatch(x.FileName, GreenLuma.GreenLumaFilesRegex, RegexOptions.IgnoreCase));

                if (!FileSystem.IsDirectoryEmpty(destinationFolder))
                {
                    FileSystem.DeleteDirectory(destinationFolder, true);
                }
                if (!FileSystem.DirectoryExists(destinationFolder))
                {
                    FileSystem.CreateDirectory(destinationFolder);
                }

                foreach (var item in files)
                {
                    /// check if file is encrypted and user is not set the password
                    if (item.Encrypted && string.IsNullOrEmpty(password))
                    {
                        var result = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOCSEU_PasswordProtectedFile"),
                            ResourceProvider.GetString("LOCSEU_Error"), "");
                        if (result.Result)
                        {
                            ExtractGreenLumaFiles(archivePath, destinationFolder, result.SelectedString);
                            return;
                        }
                        return;
                    }

                    if (Regex.IsMatch(item.FileName, @"GreenLuma\d{4}.txt"))
                    {
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                        string file = FileSystem.ReadStringFromFile(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)));
                        string rawinfo = Regex.Match(file, @"\* (.+?) \*").Value.Trim('*', ' ', '\n', '\r');
                        var version = new GreenLumaVersion()
                        {
                            Version = rawinfo.Split(' ')[2],
                        };
                        FileSystem.WriteStringToFileSafe(Path.Combine(destinationFolder, "Version.json"), Serialization.ToJson(version, true));
                    }
                    else
                    {
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                    }

                    //check if password is wrong, if wrong the extracted files will 0 bytes and delete it and ask user to reenter password
                    if (FileSystem.FileExists(Path.Combine(destinationFolder, Path.GetFileName(item.FileName))))
                    {
                        FileInfo fileInfo = new FileInfo(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)));
                        if (fileInfo.Length == 0)
                        {
                            fileInfo.Delete();
                            var result = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOCSEU_PasswordProtectedFileWrong"),
                                ResourceProvider.GetString("LOCSEU_Error"), "");
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
            CheckFiles();
        }
        void ExtractGoldbergFiles(string archivePath, string destinationFolder)
        {
            ExtractGoldbergFiles(archivePath, destinationFolder, null);
        }
        void ExtractGoldbergFiles(string archivePath, string destinationFolder, string password)
        {
            string regexPattern = @"^(?!.*\bdll_injection\b).+steamclient_experimental";
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
                        var result = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOCSEU_PasswordProtectedFile"),
                            ResourceProvider.GetString("LOCSEU_Error"), "");
                        if (result.Result)
                        {
                            ExtractGoldbergFiles(archivePath, destinationFolder, result.SelectedString);
                            return;
                        }
                        return;
                    }
                    if (item.FileName.Contains("steamclient_extra"))
                    {
                        string destinationFolderExtra = Path.Combine(destinationFolder, "extra_dlls");
                        if (!FileSystem.DirectoryExists(destinationFolderExtra))
                        {
                            FileSystem.CreateDirectory(destinationFolderExtra);
                        }
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolderExtra, Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                        if (FileSystem.FileExists(Path.Combine(destinationFolderExtra, Path.GetFileName(item.FileName))))
                        {
                            FileInfo fileInfo = new FileInfo(Path.Combine(destinationFolderExtra, Path.GetFileName(item.FileName)));
                            if (fileInfo.Length == 0)
                            {
                                fileInfo.Delete();
                                var result = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOCSEU_PasswordProtectedFileWrong"),
                                ResourceProvider.GetString("LOCSEU_Error"), "");
                                if (result.Result)
                                {
                                    ExtractGoldbergFiles(archivePath, destinationFolderExtra, result.SelectedString);
                                    return;
                                }
                                return;
                            }
                        }
                        continue;
                    }
                    if (Regex.IsMatch(item.FileName, regexPattern))
                    {
                        if (!FileSystem.DirectoryExists(destinationFolder))
                        {
                            FileSystem.CreateDirectory(destinationFolder);
                        }
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                        if (FileSystem.FileExists(Path.Combine(destinationFolder, Path.GetFileName(item.FileName))))
                        {
                            FileInfo fileInfo = new FileInfo(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)));
                            if (fileInfo.Length == 0)
                            {
                                fileInfo.Delete();
                                var result = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOCSEU_PasswordProtectedFileWrong"),
                                ResourceProvider.GetString("LOCSEU_Error"), "");
                                if (result.Result)
                                {
                                    ExtractGoldbergFiles(archivePath, destinationFolder, result.SelectedString);
                                    return;
                                }
                                return;
                            }
                        }
                        continue;
                    }
                    if (item.FileName.Contains("CHANGELOG.md"))
                    {
                        if (!FileSystem.DirectoryExists(destinationFolder))
                        {
                            FileSystem.CreateDirectory(destinationFolder);
                        }
                        using (FileStream fileStream = new FileStream(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)), FileMode.Create))
                        {
                            extractor.ExtractFile(item.Index, fileStream);
                        }
                        if (FileSystem.FileExists(Path.Combine(destinationFolder, Path.GetFileName(item.FileName))))
                        {
                            FileInfo fileInfo = new FileInfo(Path.Combine(destinationFolder, Path.GetFileName(item.FileName)));
                            if (fileInfo.Length == 0)
                            {
                                fileInfo.Delete();
                                var result = plugin.PlayniteApi.Dialogs.SelectString(ResourceProvider.GetString("LOCSEU_PasswordProtectedFileWrong"),
                                ResourceProvider.GetString("LOCSEU_Error"), "");
                                if (result.Result)
                                {
                                    ExtractGoldbergFiles(archivePath, destinationFolder, result.SelectedString);
                                    return;
                                }
                                return;
                            }
                        }
                    }
                }
            }
            CheckFiles();
        }
    }
}