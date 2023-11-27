using Prism.Ioc;
using SportDJ.Commons.Models.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SportDJ.Modules.ContentModule.ViewModels
{
    public class GridViewModel
        : BaseViewModel
    {
        #region Private Fields

        private readonly IContainerProvider containerProvider;

        private int columns;
        private int rows;

        #endregion Private Fields

        #region Public Constructors

        public GridViewModel(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        #endregion Public Constructors

        #region Public Properties

        public int Columns
        {
            get { return columns; }
            set { SetProperty(ref columns, value); }
        }

        public ObservableCollection<BaseViewModel> Elements { get; } = new ObservableCollection<BaseViewModel>();

        public int Rows
        {
            get { return rows; }
            set { SetProperty(ref rows, value); }
        }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(IEnumerable<object> contents, string name = default)
        {
            if (contents?.Any() == true)
            {
                Columns = Convert.ToInt32(Math.Ceiling(Math.Sqrt(contents.Count())));
                Rows = 0;

                var column = 0;
                var currentRows = 1;

                foreach (var content in contents)
                {
                    if (column >= Columns)
                    {
                        Rows += currentRows;
                        column = 0;
                    }

                    switch (content)
                    {
                        case Player player:

                            var playerElement = containerProvider.Resolve<PlayerViewModel>();

                            playerElement.Initialize(
                                name: player.Name,
                                row: Rows,
                                column: column);

                            Elements.Add(playerElement);

                            column += playerElement.ColumnSpan;

                            break;

                        case Group group:

                            var groupElement = containerProvider.Resolve<GroupViewModel>();

                            groupElement.Initialize(
                                group: group,
                                row: Rows,
                                column: column);

                            Elements.Add(groupElement);

                            column += groupElement.ColumnSpan;
                            currentRows = groupElement.RowSpan > currentRows
                                ? groupElement.RowSpan
                                : currentRows;

                            break;
                    }
                }
            }

            Initialize(name);
        }

        #endregion Public Methods
    }
}