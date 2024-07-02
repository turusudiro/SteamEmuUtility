using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace SteamCommon.Models
{
    public partial class DlcInfo : ObservableObject, IDisposable
    {
        public void Dispose()
        {
            image?.StreamSource?.Dispose();
            image = null;
        }
        private string _imageurl;
        public string ImageURL
        {
            get => _imageurl;
            set
            {
                _imageurl = value;
                OnPropertyChanged();
            }
        }
        private BitmapImage image;
        [DontSerialize]
        public BitmapImage Image
        {
            get => image;
            set
            {
                image = value;
                OnPropertyChanged();
            }
        }
        private string imagePath;
        public string ImagePath
        {
            get => imagePath;
            set
            {
                imagePath = value;
                OnPropertyChanged();
            }
        }
        private uint appid;
        public uint Appid
        {
            get => appid;
            set
            {
                appid = value;
            }
        }

        private bool enable;
        public bool Enable
        {
            get => enable;
            set
            {
                enable = value;
                OnPropertyChanged(nameof(Enable));
            }
        }
        private string name;
        public string Name
        {
            get => name;
            set
            {
                name = value;
            }
        }
    }
}
