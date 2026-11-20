using MIRAGE.Shared.Logging;
using MIRAGE_Launcher.Services;
using System.Windows;
using Localization = MIRAGE_Launcher.Services.Localization;

namespace MIRAGE_Launcher.Wpf;

/// <summary>Message boxes on the UI thread (also when called from a background task). Every message is logged.</summary>
public sealed class WpfDialogService(Localization p_Text, ILog p_Log) : IDialogService
{
    private readonly Localization m_text = p_Text;
    private readonly ILog m_log = p_Log;

    public void ShowInfo(string p_Message)
    {
        m_log.Info(p_Message);
        _ = Show(p_Message, m_text["Warning"], MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string p_Message)
    {
        m_log.Warn(p_Message);
        _ = Show(p_Message, m_text["Warning"], MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string p_Message)
    {
        m_log.Error(p_Message);
        _ = Show(p_Message, "MIRAGE Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public bool AskYesNo(string p_Message)
    {
        m_log.Debug($"Question: {p_Message}");
        MessageBoxResult result = Show(p_Message, m_text["Warning"], MessageBoxButton.YesNo, MessageBoxImage.Question);
        m_log.Debug($"Answer: {result}");
        return result == MessageBoxResult.Yes;
    }

    private static MessageBoxResult Show(string p_Message, string p_Caption, MessageBoxButton p_Buttons, MessageBoxImage p_Icon)
    {
        Application? app = Application.Current;
        return (app is null) || app.Dispatcher.CheckAccess()
            ? ShowNow(p_Message, p_Caption, p_Buttons, p_Icon)
            : app.Dispatcher.Invoke(() => ShowNow(p_Message, p_Caption, p_Buttons, p_Icon));
    }

    private static MessageBoxResult ShowNow(string p_Message, string p_Caption, MessageBoxButton p_Buttons, MessageBoxImage p_Icon)
    {
        Window? owner = Application.Current?.MainWindow;
        return ((owner is not null) && owner.IsVisible)
            ? MessageBox.Show(owner, p_Message, p_Caption, p_Buttons, p_Icon)
            : MessageBox.Show(p_Message, p_Caption, p_Buttons, p_Icon);
    }
}