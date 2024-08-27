using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SportDJ.Modules.MenuModule.ViewModels;

namespace SportDJ.Modules.MenuModule.Views
{
    public partial class MenuView : UserControl
    {
        #region Public Constructors

        public MenuView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        private void OnContextMenuOpened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu)
            {
                var viewModel = contextMenu.DataContext as MenuViewModel;

                if (viewModel != default)
                {
                    viewModel.MenuCloseEvent += (s, e) => Dispatcher.UIThread.Invoke(() => contextMenu.Close());
                }
            }
        }

        #endregion Private Methods
    }
}