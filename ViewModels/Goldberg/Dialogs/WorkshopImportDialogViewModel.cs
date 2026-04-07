using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SteamEmuUtility.ViewModels.Goldberg.Dialogs
{
    public class WorkshopImportDialogViewModel
    {
        public Action<IEnumerable<GoldbergModViewModel>> ApplyHandler { get; set; }
        public Action RequestClose { get; set; }
        public ObservableCollection<GoldbergModViewModel> Mods { get; set; }
        public RelayCommand<bool> ImportAllCommand { get; }
        public RelayCommand<bool> CreateSymlinkAllCommand { get; }
        public RelayCommand ApplyCommand { get; }
        public RelayCommand CancelCommand { get; }
        public WorkshopImportDialogViewModel()
        {
            ImportAllCommand = new RelayCommand<bool>(ImportAll);
            CreateSymlinkAllCommand = new RelayCommand<bool>(CreateSymlinkAll);
            ApplyCommand = new RelayCommand(Apply);
            CancelCommand = new RelayCommand(CloseDialog);
        }
        void ImportAll(bool value)
        {
            foreach (var mod in Mods)
            {
                if (mod.CanImport)
                {
                    mod.IsEnabled = value;
                }
            }
        }
        void CreateSymlinkAll(bool value)
        {
            foreach (var mod in Mods)
            {
                if (!mod.ModDirExists)
                {
                    mod.CreateSymlink = value;
                }
            }
        }
        void Apply()
        {
            CloseDialog();
            ApplyHandler?.Invoke(Mods);
        }
        void CloseDialog()
        {
            RequestClose?.Invoke();
        }
    }
}
