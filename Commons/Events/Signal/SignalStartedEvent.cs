using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signal
{
    public class SignalStartedEvent
        : PubSubEvent<ISignalService>
    { }
}