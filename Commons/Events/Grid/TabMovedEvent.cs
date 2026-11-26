using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Grid;

/// <summary>
/// A tab was moved into a group or out of its group by the user, or got a player from outside.
/// Published after the grid update.
/// </summary>
public class TabMovedEvent
    : PubSubEvent<Models.Structure.Grid>
{ }
