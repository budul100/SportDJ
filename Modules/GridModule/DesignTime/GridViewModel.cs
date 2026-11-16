namespace SportDJ.Modules.GridModule.DesignTime;

public class GridViewModel
    : ViewModels.GridViewModel
{
    #region Private Fields

    private const int SideCount = 2;

    #endregion Private Fields

    #region Public Constructors

    public GridViewModel()
        : base(containerProvider: default, grid: default, parent: default)
    {
        Columns = SideCount;
        Rows = SideCount;

        for (int row = 0; row < SideCount; row++)
        {
            for (int column = 0; column < SideCount; column++)
            {
                var element = new PlayerViewModel(
                    name: $"Player {row}{column}",
                    row: row,
                    column: column);

                Cells.Add(element);
            }
        }
    }

    #endregion Public Constructors

    #region Public Properties

    // The base implementation needs a model, which does not exist at design time
    public override string Name { get; set; } = "Group";

    #endregion Public Properties
}
