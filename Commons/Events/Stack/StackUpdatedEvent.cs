using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Stack
{
    public class StackUpdatedEvent
        : PubSubEvent<IStackService>
    { }
}