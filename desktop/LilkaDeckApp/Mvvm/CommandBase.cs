using System;
using System.Windows.Input;

namespace LilkaDeckApp.Mvvm;

/// <summary>Common plumbing for the commands the view models expose.</summary>
public abstract class CommandBase : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public abstract bool CanExecute(object? parameter);

    public abstract void Execute(object? parameter);

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
