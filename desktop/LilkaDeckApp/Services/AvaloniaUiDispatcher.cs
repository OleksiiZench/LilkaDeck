using System;
using Avalonia.Threading;

namespace LilkaDeckApp.Services;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
