using System;
using System.Threading;
using Avalonia.Threading;

namespace SportDJ.Commons.Events.Aggregation;

internal sealed class EventSubscription<TPayload>(Action<TPayload> action, ThreadOption threadOption,
    Predicate<TPayload> filter)
{
    #region Public Properties

    public SubscriptionToken Token { get; } = new();

    #endregion Public Properties

    #region Public Methods

    public void Invoke(TPayload payload)
    {
        // The filter always runs on the publisher thread, only the action is dispatched
        if (filter != default
            && !filter(payload))
        {
            return;
        }

        switch (threadOption)
        {
            case ThreadOption.UIThread:
                Dispatcher.UIThread.Post(() => action(payload));
                break;

            case ThreadOption.BackgroundThread:
                ThreadPool.QueueUserWorkItem(_ => action(payload));
                break;

            default:
                action(payload);
                break;
        }
    }

    #endregion Public Methods
}
