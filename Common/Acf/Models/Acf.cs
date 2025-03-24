using System.Collections.Generic;

namespace AcfGenerator.Models
{
    public partial class Acf : ObservableObject
    {
        public string Path { get; set; }
        private uint appid;
        public uint AppID
        {
            get => appid;
            set
            {
                if (appid != value)
                {
                    appid = value;
                    OnPropertyChanged();
                }
            }
        }
        private string installdir;
        public string InstallDir
        {
            get => installdir;
            set
            {
                if (installdir != value)
                {
                    installdir = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _selectedTarget;
        public string SelectedTarget
        {
            get => _selectedTarget;
            set
            {
                if (_selectedTarget != value)
                {
                    _selectedTarget = value;
                    OnPropertyChanged();
                }
            }
        }
        private string status;
        public string Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
