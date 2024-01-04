using Prism.Events;

namespace SportDJ.Commons.Events.Track
{
    public class TrackUpdatedEvent
        : PubSubEvent<Models.Contents.Track>
    { }
}