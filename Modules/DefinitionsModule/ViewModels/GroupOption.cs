using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

/// <summary>
/// A group, which a player or tab can be moved into.
/// </summary>
public record GroupOption(Group Group, string Text);
