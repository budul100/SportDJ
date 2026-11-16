using Prism.Events;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Mixer;

public class MixerStoppedEvent
    : PubSubEvent<IMixerService>
{ }