using System.Collections.ObjectModel;
using Prism.Commands;
using Prism.Navigation.Regions;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.SearchModule.ViewModels;

public class SearchViewModel
    : RegionViewModelBase
{
    #region Private Fields

    private string search;

    #endregion Private Fields

    #region Public Constructors

    public SearchViewModel(IRegionManager regionManager)
        : base(regionManager)
    {
        ClearCommand = new DelegateCommand(
            executeMethod: () => Search = string.Empty,
            canExecuteMethod: () => !string.IsNullOrEmpty(Search));
    }

    #endregion Public Constructors

    #region Public Properties

    // The search itself is not implemented yet, the panel is always enabled
    public static bool IsActive => true;

    public DelegateCommand ClearCommand { get; }

    public ObservableCollection<Entry> Entries { get; } = [];

    public string Search
    {
        get => search;

        set
        {
            if (SetProperty(ref search, value))
            {
                ClearCommand.RaiseCanExecuteChanged();
            }
        }
    }

    #endregion Public Properties
}
