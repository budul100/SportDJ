using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Commons.Events.Mixer;

public class MixerUpdatedEvent
    : PubSubEvent<IMixerService>
{ }
