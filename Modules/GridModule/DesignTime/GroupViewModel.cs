using System.Linq;

namespace SportDJ.Modules.GridModule.DesignTime;

public class GroupViewModel
    : ViewModels.GroupViewModel
{
    #region Private Fields

    private const int ElementsCount = 2;

    #endregion Private Fields

    #region Public Constructors

    public GroupViewModel()
        : base(containerProvider: default, group: default, parent: default, isOnCanvas: false)
    {
        for (int index = 0; index < ElementsCount; ++index)
        {
            Tabs.Add(new GridViewModel());
        }

        Selected = Tabs.First();
    }

    #endregion Public Constructors
}