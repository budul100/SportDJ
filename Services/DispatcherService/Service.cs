using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.DispatcherService;

public class Service
    : IDispatcherService
{
    #region Public Methods

    public async Task<T> InvokeAsync<T>(Func<T> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        var result = await Dispatcher.UIThread.InvokeAsync<T>(
            callback: callback,
            priority: DispatcherPriority.Background);

        return result;
    }

    public async Task InvokeAsync(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        await Dispatcher.UIThread.InvokeAsync(
            callback: callback,
            priority: DispatcherPriority.Background);
    }

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        Dispatcher.UIThread.Post(
            action: action,
            priority: DispatcherPriority.Background);
    }

    #endregion Public Methods
}