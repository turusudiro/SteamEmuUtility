using System.Collections.Generic;

namespace AcfGenerator.Models
{
    public partial class AppidFinder : ObservableObject
    {
        public string Name { get; set; }
        public uint AppID { get; set; }
        public string ImageURL { get; set; }
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
    }
}
