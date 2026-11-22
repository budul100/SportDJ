using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Output;

public class OutputStartedEvent
    : PubSubEvent<IOutputService>
{ }
