using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Structure;

namespace SportDJ.Modules.DefinitionsModule.ViewModels;

public class StackViewModel
    : TabViewModel
{
    #region Private Fields

    private const string Titel = "Content";

    private readonly IDialogService dialogService;

    private bool autoScroll;
    private Entry entry;
    private bool isSettingActive = false;
    private bool isUpdating = false;
    private Player player;

    #endregion Private Fields

    #region Public Constructors

    public StackViewModel(IEventAggregator eventAggregator, IDialogService dialogService)
    {
        EventAggregator = eventAggregator;

        this.dialogService = dialogService;

        SelectCommand = new DelegateCommand<Entry>(e => player?.StackService?.Select(e));
        ResetCommand = new DelegateCommand(() => player?.StackService?.Reset());

        ShuffleCommand = new DelegateCommand(
            executeMethod: async () => await ShuffleAsync(),
            canExecuteMethod: () => Entries?.Count > 1);

        eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
            action: _ => UpdateEntries(),
            threadOption: ThreadOption.UIThread,
            keepSubscriberReferenceAlive: true);
    }

    #endregion Public Constructors

    #region Public Properties

    public bool AutoScroll
    {
        get => autoScroll;
        set => SetProperty(ref autoScroll, value);
    }

    public ObservableCollection<Entry> Entries { get; private set; } = [];

    public Entry Entry
    {
        get => entry;

        set
        {
            if (value != default
                && !isUpdating
                && !isSettingActive
                && SetProperty(
                    storage: ref entry,
                    value: value))
            {
                try
                {
                    isSettingActive = true;
                    player?.StackService?.Select(value);
                }
                finally
                {
                    isSettingActive = false;
                }
            }
        }
    }

    public IEventAggregator EventAggregator { get; }

    public DelegateCommand ResetCommand { get; }

    public DelegateCommand<Entry> SelectCommand { get; }

    public DelegateCommand ShuffleCommand { get; }

    public override string Title => Titel;

    #endregion Public Properties

    #region Public Methods

    public void Set(Player player)
    {
        if (player != default
            && this.player != player)
        {
            this.player = player;

            UpdateEntries();
        }
    }

    #endregion Public Methods

    #region Private Methods

    private async Task ShuffleAsync()
    {
        if (player?.StackService != default)
        {
            var result = await dialogService.GetMessageBoxResultAsync(
                contentMessage: Texts.StackShuffleMessage,
                contentTitle: Texts.StackShuffleHeader,
                buttonDefinitions: ButtonEnum.YesNo,
                enterDefaultButton: ClickEnum.No);

            if (result == ButtonResult.Yes)
            {
                player.StackService.Shuffle();
            }
        }
    }

    private void UpdateEntries()
    {
        if (isUpdating) return;

        try
        {
            isUpdating = true;
            AutoScroll = false;

            var current = entry;
            var entries = player?.StackService?.Entries;

            if (entries?.Count > 0)
            {
                var hasChanged = Entries.Count != entries.Count
                    || !Entries.SequenceEqual(entries);

                if (hasChanged)
                {
                    entry = default;
                    RaisePropertyChanged(nameof(Entry));

                    for (var index = Entries.Count - 1; index >= 0; index--)
                    {
                        if (!entries.Contains(Entries[index]))
                        {
                            Entries.RemoveAt(index);
                        }
                    }

                    for (var index = 0; index < entries.Count; index++)
                    {
                        var serviceEntry = entries[index];

                        if (index >= Entries.Count)
                        {
                            Entries.Add(serviceEntry);
                        }
                        else if (Entries[index] != serviceEntry)
                        {
                            var existingIndex = Entries.IndexOf(serviceEntry);

                            if (existingIndex >= 0)
                            {
                                Entries.Move(
                                    oldIndex: existingIndex,
                                    newIndex: index);
                            }
                            else
                            {
                                Entries.Insert(
                                    index: index,
                                    item: serviceEntry);
                            }
                        }
                    }
                }

                if (current != default
                    && Entries.Contains(current))
                {
                    entry = current;
                }
                else
                {
                    entry = Entries.FirstOrDefault(e => e.IsSelected);
                }

                if (hasChanged)
                {
                    RaisePropertyChanged(nameof(Entry));
                }
            }
            else
            {
                if (Entries.Count > 0)
                {
                    entry = default;
                    RaisePropertyChanged(nameof(Entry));

                    Entries.Clear();
                }
            }

            ShuffleCommand.RaiseCanExecuteChanged();
        }
        finally
        {
            isUpdating = false;
            AutoScroll = true;
        }
    }

    #endregion Private Methods
}