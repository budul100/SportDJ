using Prism.Events;
using System.Collections.Generic;

namespace SportDJ.Commons.Events.Entry
{
    public class EntriesUpdatedEvent
        : PubSubEvent<IEnumerable<Models.Contents.Entry>>
    { }
}