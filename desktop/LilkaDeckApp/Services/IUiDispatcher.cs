using System;

namespace LilkaDeckApp.Services;

/// <summary>Runs work on the UI thread, where view models may be touched.</summary>
public interface IUiDispatcher
{
    /// <summary>Safe to call from any thread. The action runs later, in order.</summary>
    void Post(Action action);
}
