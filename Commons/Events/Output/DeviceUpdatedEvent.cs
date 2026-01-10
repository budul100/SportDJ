using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Output
{
    public class DeviceUpdatedEvent
        : PubSubEvent<IOutputService>
    { }
}