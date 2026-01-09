using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Panel
{
    public class StackVisibleEvent
        : PubSubEvent<IStackService>
    { }
}