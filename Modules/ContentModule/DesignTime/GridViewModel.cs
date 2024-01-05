namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class GridViewModel
        : ViewModels.GridViewModel
    {
        #region Private Fields

        private const int SideCount = 4;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel()
            : base(default)
        {
            SetElements();
        }

        #endregion Public Constructors

        #region Private Methods

        private void SetElements()
        {
            for (int row = 0; row < SideCount; row++)
            {
                for (int column = 0; column < SideCount; column++)
                {
                    var element = new PlayerViewModel(
                        name: $"Player {row}{column}",
                        row: row,
                        column: column);

                    Elements.Add(element);
                }
            }
        }

        #endregion Private Methods
    }
}