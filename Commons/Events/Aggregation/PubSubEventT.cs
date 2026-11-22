using System;

namespace SportDJ.Commons.Events.Aggregation;

/// <summary>
/// Event with payload.
/// </summary>
public class PubSubEvent<TPayload>
    : PayloadEventBase<TPayload>
{
    #region Public Methods

    public virtual void Publish(TPayload payload)
    {
        Dispatch(payload);
    }

    /// <param name="keepSubscriberReferenceAlive">
    /// Kept for source compatibility only, subscriptions always hold a strong reference.
    /// </param>
    public virtual SubscriptionToken Subscribe(Action<TPayload> action,
        ThreadOption threadOption = ThreadOption.PublisherThread, bool keepSubscriberReferenceAlive = false,
        Predicate<TPayload> filter = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        return AddSubscription(new EventSubscription<TPayload>(
            action: action,
            threadOption: threadOption,
            filter: filter));
    }

    #endregion Public Methods
}
