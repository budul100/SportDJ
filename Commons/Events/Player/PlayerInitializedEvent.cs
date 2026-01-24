using Prism.Events;

namespace SportDJ.Commons.Events.Player
{
    public class PlayerInitializedEvent
        : PubSubEvent<Models.Structure.Player>
    { }
}