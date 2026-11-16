using Prism.Events;
using SportDJ.Commons.Enums;
using System;

namespace SportDJ.Commons.Events.Panel;

public class PanelUpdatedEvent
    : PubSubEvent<(PanelType PanelType, Type Type)>
{ }