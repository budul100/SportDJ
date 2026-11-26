using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Grid;

/// <summary>
/// The MIDI key of a tab was pressed, the tab is supposed to be shown.
/// </summary>
public class TabKeyPressedEvent
    : PubSubEvent<Models.Structure.Grid>
{ }
