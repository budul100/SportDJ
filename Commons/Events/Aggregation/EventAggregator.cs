using System;
using System.Collections.Concurrent;

namespace SportDJ.Commons.Events.Aggregation;

public class EventAggregator
    : IEventAggregator
{
    #region Private Fields

    private readonly ConcurrentDictionary<Type, EventBase> events = new();

    #endregion Private Fields

    #region Public Methods

    public TEventType GetEvent<TEventType>()
        where TEventType : EventBase, new()
    {
        return (TEventType)events.GetOrAdd(
            key: typeof(TEventType),
            valueFactory: _ => new TEventType());
    }

    #endregion Public Methods
}
