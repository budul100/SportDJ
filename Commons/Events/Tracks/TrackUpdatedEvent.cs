using Prism.Events;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Commons.Events.Tracks
{
    public class TrackUpdatedEvent
        : PubSubEvent<Track>
    { }
}