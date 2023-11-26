using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class GridViewModel
        : ViewModels.BaseViewModel
    {
        #region Private Fields

        private const int ElementsCount = 4;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel()
            : this("Group")
        { }

        public GridViewModel(string name)
        {
            Initialize(
                name: name);

            Elements = GetElements().ToArray();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<ViewModels.BaseViewModel> Elements { get; }

        #endregion Public Properties

        #region Private Methods

        private IEnumerable<ViewModels.BaseViewModel> GetElements()
        {
            for (int index = 0; index < ElementsCount; index++)
            {
                var result = new PlayerViewModel(
                    name: $"Player {index}");

                yield return result;
            }
        }

        #endregion Private Methods
    }
}