using Prism.Ioc;
using SportDJ.Commons.Models.Settings;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class GroupViewModel
        : CellViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        #endregion Private Fields

        #region Public Constructors

        public GroupViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<GridViewModel> Elements { get; } = new ObservableCollection<GridViewModel>();

        #endregion Public Properties

        #region Public Methods

        public void Initialize(Group group, int row, int column)
        {
            var rowSpan = 1;
            var columnSpan = 1;

            if (group.Contents?.Any() == true)
            {
                var element = containerProvider.Resolve<GridViewModel>();

                element.Initialize(
                    contents: group.Contents,
                    name: group.Name);

                rowSpan = element.Rows;
                columnSpan = element.Columns;

                Elements.Add(element);
            }

            Initialize(
                name: group.Name,
                row: row,
                column: column,
                rowSpan: rowSpan,
                columnSpan: columnSpan);
        }

        #endregion Public Methods
    }
}