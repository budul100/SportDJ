using Prism.Events;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Events.Panel
{
    public class PanelUpdatedEvent
        : PubSubEvent<PanelType>
    { }
}