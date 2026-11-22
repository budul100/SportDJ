using System.Collections.ObjectModel;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Mvvm;

namespace SportDJ.Modules.SearchModule.ViewModels;

public class SearchViewModel
    : BindableBase
{
    #region Private Fields

    private string search;

    #endregion Private Fields

    #region Public Constructors

    public SearchViewModel()
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
