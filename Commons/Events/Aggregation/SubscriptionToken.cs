using System;

namespace SportDJ.Commons.Events.Aggregation;

public sealed class SubscriptionToken
    : IEquatable<SubscriptionToken>, IDisposable
{
    #region Private Fields

    private readonly Guid token = Guid.NewGuid();

    private Action<SubscriptionToken> unsubscribeAction;

    #endregion Private Fields

    #region Public Methods

    /// <summary>
    /// Removes the subscription from its event, like Prism's token does.
    /// </summary>
    public void Dispose()
    {
        unsubscribeAction?.Invoke(this);
    }

    public bool Equals(SubscriptionToken other)
    {
        return other != default
            && token == other.token;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SubscriptionToken);
    }

    public override int GetHashCode()
    {
        return token.GetHashCode();
    }

    public override string ToString()
    {
        return token.ToString();
    }

    #endregion Public Methods

    #region Internal Methods

    internal void Attach(Action<SubscriptionToken> unsubscribe)
    {
        unsubscribeAction = unsubscribe;
    }

    #endregion Internal Methods
}
