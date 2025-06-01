using System;
using System.Windows.Input;

namespace MIRAGE_Launcher.ViewModels
{
    class Cmd : ICommand
    {
        private readonly Action<object>? executeWithParam;
        private readonly Action? executeWithoutParam;
        private readonly Func<object, bool>? canExecuteWithParam;
        private readonly Func<bool>? canExecuteWithoutParam;

        public Cmd(Action<object> execute, Func<object, bool>? canExecute = null)
        {
            executeWithParam = execute ?? throw new ArgumentNullException(nameof(execute));
            canExecuteWithParam = canExecute;
        }

        public Cmd(Action execute, Func<bool>? canExecute = null)
        {
            executeWithoutParam = execute ?? throw new ArgumentNullException(nameof(execute));
            canExecuteWithoutParam = canExecute;
        }

        public bool CanExecute(object? parameter) => executeWithParam != null ? canExecuteWithParam?.Invoke(parameter!) ?? true : canExecuteWithoutParam?.Invoke() ?? true;
        public void Execute(object? parameter)
        {
            if (executeWithParam != null)
            {
                ArgumentNullException.ThrowIfNull(parameter);
                executeWithParam(parameter);
            }
            else
            {
                executeWithoutParam?.Invoke();
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
