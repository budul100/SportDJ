using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.Regions;

namespace SportDJ.Modules.MenuModule.ViewModels
{
    public class MenuViewModel
        : BindableBase, INavigationAware
    {
        #region Public Events

        public event EventHandler MenuCloseEvent;

        #endregion Public Events

        #region Public Properties

        public ObservableCollection<BindableBase> MenuItems { get; }

        #endregion Public Properties

        #region Public Methods

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            throw new NotImplementedException();
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            throw new NotImplementedException();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            throw new NotImplementedException();
        }

        #endregion Public Methods
    }
}