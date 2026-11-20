using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MIRAGE_Launcher.ViewModels;

/// <summary>A command that runs an action. <see cref="ICommand"/> is part of .NET itself, not of WPF.</summary>
public sealed class RelayCommand(Action p_Execute, Func<bool>? p_CanExecute = null) : ICommand
{
    private readonly Action m_execute = p_Execute ?? throw new ArgumentNullException(nameof(p_Execute));
    private readonly Func<bool>? m_canExecute = p_CanExecute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? p_Parameter) => m_canExecute?.Invoke() ?? true;

    public void Execute(object? p_Parameter) => m_execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>
/// A command that runs an async action. It can't run twice at the same time, and an exception goes to
/// <paramref name="p_OnError"/> instead of being lost.
/// </summary>
public sealed class AsyncRelayCommand(Func<Task> p_Execute, Action<Exception> p_OnError) : ICommand
{
    private readonly Func<Task> m_execute = p_Execute ?? throw new ArgumentNullException(nameof(p_Execute));
    private readonly Action<Exception> m_onError = p_OnError ?? throw new ArgumentNullException(nameof(p_OnError));
    private bool m_isRunning;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? p_Parameter) => !m_isRunning;

    public async void Execute(object? p_Parameter) => await ExecuteAsync().ConfigureAwait(true);

    public async Task ExecuteAsync()
    {
        if (m_isRunning)
        {
            return;
        }
        m_isRunning = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await m_execute().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            m_onError(ex);
        }
        finally
        {
            m_isRunning = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}