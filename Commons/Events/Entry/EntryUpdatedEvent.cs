using Prism.Events;

namespace SportDJ.Commons.Events.Track
{
    public class EntryUpdatedEvent
        : PubSubEvent<Models.Contents.Entry>
    { }
}