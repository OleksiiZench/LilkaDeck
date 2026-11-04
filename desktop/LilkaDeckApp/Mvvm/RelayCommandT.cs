using System;

namespace LilkaDeckApp.Mvvm;

/// <summary>A command that receives its argument from the view (CommandParameter).</summary>
public sealed class RelayCommand<T> : CommandBase
{
    private readonly Action<T> _execute;
    private readonly Func<T, bool>? _canExecute;

    public RelayCommand(Action<T> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public override bool CanExecute(object? parameter) => parameter is T value && (_canExecute?.Invoke(value) ?? true);

    public override void Execute(object? parameter)
    {
        if (parameter is T value) _execute(value);
    }
}
