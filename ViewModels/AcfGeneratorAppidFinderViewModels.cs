using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AcfGenerator.Models;
using Playnite.SDK;
using SteamCommon;

namespace SteamEmuUtility.ViewModels
{
    class AcfGeneratorAppidFinderViewModels : ObservableObject
    {
        private readonly IPlayniteAPI PlayniteApi;
        private readonly Action closeWindowAction;

        public AcfGeneratorAppidFinderViewModels(IPlayniteAPI api, Acf acf, Action closeWindow)
        {
            PlayniteApi = api;
            Game = acf;
            NameToQuery = Path.GetFileName(Game.Path);
            Items = new ObservableCollection<AppidFinder>();
            closeWindowAction = closeWindow;
        }
        private string nametoquery;
        public string NameToQuery
        {
            get => nametoquery;
            set
            {
                if (nametoquery != value)
                {
                    nametoquery = value;
                    OnPropertyChanged();
                }
            }
        }
        public ObservableCollection<AppidFinder> Items { get; set; }
        private AppidFinder selectedItem;
        public AppidFinder SelectedItem
        {
            get => selectedItem;
            set
            {
                if (selectedItem != value)
                {
                    selectedItem = value;
                    OnPropertyChanged();
                }
                if (selectedItem != null)
                {
                    Game.AppID = selectedItem.AppID;
                    closeWindowAction.Invoke();
                }
            }
        }


        public Acf Game { get; set; }
        public RelayCommand<string> FindAppid
        {
            get => new RelayCommand<string>((a) =>
            {
                if (string.IsNullOrEmpty(a)) return;
                PlayniteApi.Dialogs.ActivateGlobalProgress((args) => FindAppidByName(a, args), new GlobalProgressOptions(""));
            });
        }
        public void FindAppidByName(string name, GlobalProgressActionArgs args)
        {
            args.Text = string.Format(ResourceProvider.GetString("LOCSEU_SearchingName"), name);
            var queriedItems = SteamUtilities.GetAppStoreInfoByName(name);

            if (queriedItems == null || queriedItems.Count() == 0)
            {
                args.Text = "No results found.";
                return;
            }

            args.Text = "Preparing results...";

            // 🟢 Collect items first (avoids UI lag)
            List<AppidFinder> newItems = new List<AppidFinder>();

            foreach (var queriedItem in queriedItems)
            {
                newItems.Add(new AppidFinder
                {
                    AppID = (uint)queriedItem.AppId,
                    Name = queriedItem.Name,
                    ImageURL = queriedItem.ImageLink
                });
            }

            // 🟢 Perform UI update in a single batch operation
            PlayniteApi.MainView.UIDispatcher.Invoke(() =>
            {
                Items.Clear();  // Clear existing items
                foreach (var item in newItems)
                {
                    Items.Add(item); // Add items efficiently
                }
            });

            // 🟢 Final status update (less spammy)
            args.Text = $"Added {newItems.Count} items.";

        }
    }
}
