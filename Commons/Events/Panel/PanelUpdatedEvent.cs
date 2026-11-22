using System;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Panel;

public class PanelUpdatedEvent
    : PubSubEvent<(PanelType PanelType, Type Type)>
{ }
