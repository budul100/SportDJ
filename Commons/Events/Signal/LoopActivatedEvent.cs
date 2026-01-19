using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signal
{
    public class LoopActivatedEvent
        : PubSubEvent<ISignalService>
    { }
}