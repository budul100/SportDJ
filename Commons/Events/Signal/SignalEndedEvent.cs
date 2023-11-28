using Prism.Events;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Events.Signal
{
    public class SignalEndedEvent
        : PubSubEvent<Track>
    { }
}