using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Players
{
    public class PlayerStoppedEvent
        : PubSubEvent<IPlayerService>
    { }
}