using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Grid;

/// <summary>
/// A tab was added to a group by the user. Published after the grid update.
/// </summary>
public class TabAddedEvent
    : PubSubEvent<Models.Structure.Grid>
{ }
