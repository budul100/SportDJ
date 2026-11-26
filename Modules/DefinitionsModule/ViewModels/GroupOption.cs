using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

/// <summary>
/// An entry of the group selection: an existing group or, without a group, the new group.
/// </summary>
public record GroupOption(Group Group, string Text)
{
    #region Public Properties

    public bool IsNew => Group == default;

    #endregion Public Properties
}
