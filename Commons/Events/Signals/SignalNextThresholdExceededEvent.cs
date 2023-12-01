using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signals
{
    public class SignalNextThresholdExceededEvent
        : PubSubEvent<ISignalService>
    { }
}