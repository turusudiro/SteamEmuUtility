using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Windows;

namespace SteamEmuUtility.ViewModels
{
    public class GoldbergModEditorDialogViewModel : ObservableObject
    {
        private readonly IPlayniteAPI PlayniteApi;
        private readonly Window window;

        public GoldbergModEditorDialogViewModel(GoldbergModViewModel goldbergMod, SteamEmuUtility plugin, Window window)
        {
            Mod = Serialization.GetClone(goldbergMod);
            PlayniteApi = plugin.PlayniteApi;
            this.window = window;
        }
        public Action<GoldbergModViewModel> OnApply { get; set; }
        public GoldbergModViewModel Mod { get; set; }
        public RelayCommand ApplyCommand => new RelayCommand(Apply);
        public RelayCommand Cancel => new RelayCommand(OnCancel);
        void Apply()
        {
            if (string.IsNullOrEmpty(Mod.ID))
            {
                PlayniteApi.Dialogs.ShowErrorMessage("ID Cannot be empty!");
                return;
            }
            OnApply?.Invoke(Mod);
            window.Close();
        }
        void OnCancel()
        {
            window.Close();
        }
    }
}
