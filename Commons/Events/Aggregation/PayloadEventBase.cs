using System.Linq;
using System.Threading;

namespace SportDJ.Commons.Events.Aggregation;

public abstract class PayloadEventBase<TPayload>
    : EventBase
{
    #region Private Fields

    private readonly Lock subscriptionsLock = new();

    private EventSubscription<TPayload>[] subscriptions = [];

    #endregion Private Fields

    #region Public Methods

    public override bool Contains(SubscriptionToken token)
    {
        return token != default
            && Volatile.Read(ref subscriptions).Any(s => s.Token.Equals(token));
    }

    public override void Unsubscribe(SubscriptionToken token)
    {
        if (token == default)
        {
            return;
        }

        lock (subscriptionsLock)
        {
            subscriptions = subscriptions
                .Where(s => !s.Token.Equals(token))
                .ToArray();
        }
    }

    #endregion Public Methods

    #region Internal Methods

    internal SubscriptionToken AddSubscription(EventSubscription<TPayload> subscription)
    {
        lock (subscriptionsLock)
        {
            subscriptions = [.. subscriptions, subscription];
        }

        subscription.Token.Attach(Unsubscribe);

        return subscription.Token;
    }

    internal void Dispatch(TPayload payload)
    {
        var current = Volatile.Read(ref subscriptions);

        foreach (var subscription in current)
        {
            subscription.Invoke(payload);
        }
    }

    #endregion Internal Methods
}
