using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signal;

public class SignalEndedEvent
    : PubSubEvent<ISignalService>
{ }
