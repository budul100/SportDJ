using Prism.Events;

namespace SportDJ.Commons.Events.Entry;

public class EntryUpdatedEvent
    : PubSubEvent<Models.Contents.Entry>
{ }