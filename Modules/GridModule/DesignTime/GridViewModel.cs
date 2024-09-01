namespace SportDJ.Modules.GridModule.DesignTime
{
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
            Name = "Group";

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
    }
}