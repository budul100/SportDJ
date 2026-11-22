using System;

namespace SportDJ.Commons.Events.Aggregation;

/// <summary>
/// Event without payload.
/// </summary>
public class PubSubEvent
    : PayloadEventBase<object>
{
    #region Public Methods

    public virtual void Publish()
    {
        Dispatch(default);
    }

    /// <param name="keepSubscriberReferenceAlive">
    /// Kept for source compatibility only, subscriptions always hold a strong reference.
    /// </param>
    public virtual SubscriptionToken Subscribe(Action action, ThreadOption threadOption = ThreadOption.PublisherThread,
        bool keepSubscriberReferenceAlive = false)
    {
        ArgumentNullException.ThrowIfNull(action);

        return AddSubscription(new EventSubscription<object>(
            action: _ => action(),
            threadOption: threadOption,
            filter: default));
    }

    #endregion Public Methods
}
