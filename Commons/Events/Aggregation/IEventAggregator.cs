namespace SportDJ.Commons.Events.Aggregation;

public interface IEventAggregator
{
    TEventType GetEvent<TEventType>()
        where TEventType : EventBase, new();
}
