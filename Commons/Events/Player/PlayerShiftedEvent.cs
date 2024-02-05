using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Player
{
    public class PlayerShiftedEvent
        : PubSubEvent<IPlayerService>
    { }
}