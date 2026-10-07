using System;
using System.Collections.Generic;
using System.Globalization;
using LilkaDeckApp.Mvvm;

namespace LilkaDeckApp.ViewModels;

/// <summary>What the application is doing: the event log and the progress of the current save.</summary>
public sealed class ActivityViewModel : ObservableObject
{
    // Without a limit the text would grow forever and every new line would copy all the old ones.
    private const int MaxLines = 500;

    private readonly Queue<string> _lines = new();
    private readonly Func<DateTime> _clock;
    private string _logText = "";
    private double _progress;

    public ActivityViewModel(Func<DateTime>? clock = null)
    {
        _clock = clock ?? (() => DateTime.Now);
    }

    public string LogText
    {
        get => _logText;
        private set => SetProperty(ref _logText, value);
    }

    /// <summary>From 0 to 100.</summary>
    public double Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public void Info(string message) => Append("ІНФО", message);

    public void Error(string message) => Append("ПОМИЛКА", message);

    private void Append(string level, string message)
    {
        string time = _clock().ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        _lines.Enqueue($"[{time}] [{level}] {message}\r\n");

        while (_lines.Count > MaxLines)
        {
            _lines.Dequeue();
        }
        LogText = string.Concat(_lines);
    }
}
