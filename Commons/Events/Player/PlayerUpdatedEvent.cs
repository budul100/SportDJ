using Prism.Events;

namespace SportDJ.Commons.Events.Player
{
    public class PlayerUpdateEvent
        : PubSubEvent<Models.Structure.Player>
    { }
}