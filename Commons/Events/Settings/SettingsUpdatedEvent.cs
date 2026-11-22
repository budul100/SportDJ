using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Settings;

public class SettingsUpdatedEvent<T>
    : PubSubEvent
    where T : class
{ }
