using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Tracks
{
    public class TrackSelectedEvent
        : PubSubEvent<IStackService>
    { }
}