using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Settings;

public class SettingsLoadedEvent<T>
    : PubSubEvent
    where T : class
{ }
