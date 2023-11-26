using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Modules.ContentModule.DesignTime
{
    public class GroupViewModel
        : ViewModels.BaseViewModel
    {
        #region Private Fields

        private const int ElementsCount = 2;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel()
        {
            Elements = GetElements().ToArray();
        }

        #endregion Public Constructors

        #region Public Properties

        public IEnumerable<GridViewModel> Elements { get; }

        #endregion Public Properties

        #region Private Methods

        private IEnumerable<GridViewModel> GetElements()
        {
            for (int index = 0; index < ElementsCount; index++)
            {
                var result = new GridViewModel();

                yield return result;
            }
        }

        #endregion Private Methods
    }
}