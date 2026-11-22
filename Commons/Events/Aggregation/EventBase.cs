namespace SportDJ.Commons.Events.Aggregation;

/// <summary>
/// Common base of all events which can be retrieved from an <see cref="IEventAggregator"/>.
/// </summary>
public abstract class EventBase
{
    #region Public Methods

    public abstract bool Contains(SubscriptionToken token);

    public abstract void Unsubscribe(SubscriptionToken token);

    #endregion Public Methods
}
