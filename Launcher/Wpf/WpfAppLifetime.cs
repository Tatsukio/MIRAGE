using System.Windows;
using MIRAGE_Launcher.Services;

namespace MIRAGE_Launcher.Wpf;

public sealed class WpfAppLifetime : IAppLifetime
{
    public void Shutdown() => Application.Current.Shutdown();
}