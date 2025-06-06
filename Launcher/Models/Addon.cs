using System.Collections.Generic;
using System.ComponentModel;

namespace MIRAGE_Launcher.Models
{
    public class Addon : INotifyPropertyChanged
    {
        private bool isEnabled;
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled != value)
                {
                    isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        public string Id { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public List<string> Requires { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string p_name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p_name));
        }
    }
}
