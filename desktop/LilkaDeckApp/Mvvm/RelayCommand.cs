using System;

namespace LilkaDeckApp.Mvvm;

public sealed class RelayCommand : CommandBase
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public override void Execute(object? parameter) => _execute();
}
