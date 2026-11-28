using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ParaWorldStatus.ViewModels;

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
