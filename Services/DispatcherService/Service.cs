using Avalonia.Threading;
using SportDJ.Commons.Interfaces;
using System;
using System.Threading.Tasks;

namespace SportDJ.Services.DispatcherService
{
    public class Service
        : IDispatcherService
    {
        #region Public Methods

        public async Task<T> InvokeAsync<T>(Func<T> callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var result = await Dispatcher.UIThread.InvokeAsync<T>(
                callback: callback,
                priority: DispatcherPriority.Background);

            return result;
        }

        public async Task InvokeAsync(Action callback)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            await Dispatcher.UIThread.InvokeAsync(
                callback: callback,
                priority: DispatcherPriority.Background);
        }

        public void Post(Action action)
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Dispatcher.UIThread.Post(
                action: action,
                priority: DispatcherPriority.Background);
        }

        #endregion Public Methods
    }
}