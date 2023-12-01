using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signals
{
    public class SignalEndedEvent
        : PubSubEvent<ISignalService>
    { }
}