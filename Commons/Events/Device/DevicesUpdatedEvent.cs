using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Device
{
    public class DevicesUpdatedEvent
        : PubSubEvent<IOutputService>
    { }
}