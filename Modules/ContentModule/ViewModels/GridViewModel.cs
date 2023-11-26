using Prism.Ioc;
using SportDJ.Commons.Models;
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

        #endregion Public Properties

        #region Public Methods

        public void Initialize(IEnumerable<Content> contents, string name = default)
        {
            Initialize(name);

            Elements.Clear();

            if (contents?.Any() == true)
            {
                Columns = Convert.ToInt32(Math.Ceiling(Math.Sqrt(contents.Count())));

                var column = 0;
                var row = 0;

                foreach (var content in contents)
                {
                    switch (content)
                    {
                        case Player player:

                            var playerElement = containerProvider.Resolve<PlayerViewModel>();
                            playerElement.Initialize(player);

                            Elements.Add(playerElement);

                            break;

                        case Group group:

                            var groupElement = containerProvider.Resolve<GroupViewModel>();
                            groupElement.Initialize(group);

                            Elements.Add(groupElement);

                            break;
                    }

                    if (column++ > columns)
                    {
                        row++;
                        column = 0;
                    }
                }
            }
        }

        #endregion Public Methods
    }
}