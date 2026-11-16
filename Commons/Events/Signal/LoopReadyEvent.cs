using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Signal;

public class LoopReadyEvent
    : PubSubEvent<ISignalService>
{ }