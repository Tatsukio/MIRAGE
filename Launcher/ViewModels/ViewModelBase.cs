using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MIRAGE_Launcher.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool Set<T>(ref T p_field, T p_value, [CallerMemberName] string p_propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(p_field, p_value)) return false;
            p_field = p_value;
            OnPropertyChanged(p_propertyName);
            return true;
        }

        private void OnPropertyChanged(string p_propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p_propertyName));
        }
    }
}
