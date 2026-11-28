using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MIRAGE_Launcher.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T p_Field, T p_Value, [CallerMemberName] string p_PropertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(p_Field, p_Value))
        {
            return false;
        }
        p_Field = p_Value;
        OnPropertyChanged(p_PropertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string p_PropertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p_PropertyName));
}