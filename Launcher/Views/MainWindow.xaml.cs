using System.Windows;
using System.Windows.Input;

namespace MIRAGE_Launcher.Views;

/// <summary>The launcher window. All logic is in ViewModels\MainViewModel.cs; App.xaml.cs sets the DataContext.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>The window has no title bar: drag it by the top area, double click minimizes.</summary>
    private void OnDragAreaMouseDown(object p_Sender, MouseButtonEventArgs p_Args)
    {
        if (p_Args.ClickCount == 2)
        {
            WindowState = WindowState.Minimized;
            return;
        }
        DragMove();
    }
}