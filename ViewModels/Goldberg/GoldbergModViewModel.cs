using SteamEmuUtility.Common.Goldberg.Models;
using System.Collections.Generic;

namespace SteamEmuUtility.ViewModels
{
    public class GoldbergModViewModel : ObservableObject
    {
        private string _id;
        private string _title;
        private string _description;
        private ulong? _steamidowner;
        private int? _timecreated;
        private int? _timeupdated;
        private int? _timeadded;
        private string _tags;
        private string _primaryfilename;
        private int? _primaryfilesize;
        private string _previewfilename;
        private int? _previewfilesize;
        private int? _totalfilessizes;
        private string _mingamebranch;
        private string _maxgamebranch;
        private string _workshopitemurl;
        private int? _upvotes;
        private int? _downvotes;
        private int? _numchildren;
        private string _path;
        private string _previewurl;
        private double? _score;
        private string _metadata;
        private bool _isEnabled;
        private bool _canImport;
        private bool _createSymlink;
        private bool _canCreateSymlink;
        private bool _modDirExists;
        private bool _inModsJson;
        private string _modPath;


        public string ID { get => _id; set => SetValue(ref _id, value); }
        public string Title { get => _title; set => SetValue(ref _title, value); }
        public string Description { get => _description; set => SetValue(ref _description, value); }
        public ulong? SteamIDOwner { get => _steamidowner; set => SetValue(ref _steamidowner, value); }
        public int? TimeCreated { get => _timecreated; set => SetValue(ref _timecreated, value); }
        public int? TimeUpdated { get => _timeupdated; set => SetValue(ref _timeupdated, value); }
        public int? TimeAdded { get => _timeadded; set => SetValue(ref _timeadded, value); }
        public string Tags { get => _tags; set => SetValue(ref _tags, value); }
        public string PrimaryFileName { get => _primaryfilename; set => SetValue(ref _primaryfilename, value); }
        public int? PrimaryFileSize { get => _primaryfilesize; set => SetValue(ref _primaryfilesize, value); }
        public string PreviewFileName { get => _previewfilename; set => SetValue(ref _previewfilename, value); }
        public int? PreviewFileSize { get => _previewfilesize; set => SetValue(ref _previewfilesize, value); }
        public int? TotalFilesSizes { get => _totalfilessizes; set => SetValue(ref _totalfilessizes, value); }
        public string MinGameBranch { get => _mingamebranch; set => SetValue(ref _mingamebranch, value); }
        public string MaxGameBranch { get => _maxgamebranch; set => SetValue(ref _maxgamebranch, value); }
        public string WorkshopItemURL { get => _workshopitemurl; set => SetValue(ref _workshopitemurl, value); }
        public int? Upvotes { get => _upvotes; set => SetValue(ref _upvotes, value); }
        public int? Downvotes { get => _downvotes; set => SetValue(ref _downvotes, value); }
        public int? NumChildren { get => _numchildren; set => SetValue(ref _numchildren, value); }
        public string Path { get => _path; set => SetValue(ref _path, value); }
        public string PreviewURL { get => _previewurl; set => SetValue(ref _previewurl, value); }
        public double? Score { get => _score; set => SetValue(ref _score, value); }
        public string Metadata { get => _metadata; set => SetValue(ref _metadata, value); }
        public bool CanCreateSymlink { get => _canCreateSymlink; set => SetValue(ref _canCreateSymlink, value); }
        public bool IsEnabled { get => _isEnabled; set => SetValue(ref _isEnabled, value); }
        public bool CanImport { get => _canImport; set => SetValue(ref _canImport, value); }
        public bool CreateSymlink { get => _createSymlink; set => SetValue(ref _createSymlink, value); }
        public bool ModDirExists { get => _modDirExists; set => SetValue(ref _modDirExists, value); }
        public bool InModsJson { get => _inModsJson; set => SetValue(ref _inModsJson, value); }
        public string ModPath { get => _modPath; set => SetValue(ref _modPath, value); }


        public GoldbergModViewModel() { }
        public GoldbergModViewModel(GoldbergMod mod)
        {
            ID = mod.ID;
            Title = mod.Title;
            Description = mod.Description;
            SteamIDOwner = mod.SteamIDOwner;
            TimeCreated = mod.TimeCreated;
            TimeUpdated = mod.TimeUpdated;
            TimeAdded = mod.TimeAdded;
            Tags = mod.Tags;
            PrimaryFileName = mod.PrimaryFileName;
            PrimaryFileSize = mod.PrimaryFileSize;
            PreviewFileName = mod.PreviewFileName;
            PreviewFileSize = mod.PreviewFileSize;
            TotalFilesSizes = mod.TotalFilesSizes;
            MinGameBranch = mod.MinGameBranch;
            MaxGameBranch = mod.MaxGameBranch;
            WorkshopItemURL = mod.WorkshopItemURL;
            Upvotes = mod.Upvotes;
            Downvotes = mod.Downvotes;
            NumChildren = mod.NumChildren;
            Path = mod.Path;
            PreviewURL = mod.PreviewURL;
            Score = mod.Score;
            Metadata = mod.Metadata;
        }
    }
}
