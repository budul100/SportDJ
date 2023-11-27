namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class GridViewModel
        : ViewModels.GridViewModel
    {
        #region Private Fields

        private const int ElementsCount = 4;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel()
            : this("Group")
        { }

        public GridViewModel(string name)
            : base(default)
        {
            Initialize(
                name: name);

            SetElements();
        }

        #endregion Public Constructors

        #region Private Methods

        private void SetElements()
        {
            for (int index = 0; index < ElementsCount; index++)
            {
                var element = new PlayerViewModel(
                    name: $"Player {index}");

                Elements.Add(element);
            }
        }

        #endregion Private Methods
    }
}