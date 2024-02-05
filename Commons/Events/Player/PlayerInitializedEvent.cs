using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Player
{
    public class PlayerInitializedEvent
        : PubSubEvent<IPlayerService>
    { }
}