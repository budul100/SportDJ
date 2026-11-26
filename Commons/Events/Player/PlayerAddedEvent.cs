using SportDJ.Commons.Events.Aggregation;

namespace SportDJ.Commons.Events.Player;

/// <summary>
/// A player was added to the grid by the user. Published after the grid update.
/// </summary>
public class PlayerAddedEvent
    : PubSubEvent<Models.Structure.Player>
{ }
