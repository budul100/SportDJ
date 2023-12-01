using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signals
{
    public class SignalUpdatedEvent
        : PubSubEvent<ISignalService>
    { }
}