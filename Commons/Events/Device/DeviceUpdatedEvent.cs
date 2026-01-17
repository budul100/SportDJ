using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Device
{
    public class DeviceUpdatedEvent
        : PubSubEvent<IOutputService>
    { }
}