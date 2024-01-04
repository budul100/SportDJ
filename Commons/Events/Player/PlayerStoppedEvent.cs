using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Player
{
    public class PlayerStoppedEvent
        : PubSubEvent<IPlayerService>
    { }
}