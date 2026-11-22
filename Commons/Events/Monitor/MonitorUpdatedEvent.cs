using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Volume;

public class MonitorUpdatedEvent
    : PubSubEvent<IMonitorService>
{ }
