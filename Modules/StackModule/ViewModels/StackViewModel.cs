using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.StackModule.ViewModels
{
    public class StackViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private readonly IDialogService dialogService;

        private bool autoScroll;
        private Entry entry;
        private bool isSettingActive = false;
        private bool isUpdating = false;
        private IStackService service;

        #endregion Private Fields

        #region Public Constructors

        public StackViewModel(IRegionManager regionManager, IEventAggregator eventAggregator,
            IDialogService dialogService)
            : base(regionManager)
        {
            EventAggregator = eventAggregator;

            this.dialogService = dialogService;

            SelectCommand = new DelegateCommand<Entry>(e => service?.Select(e));
            ResetCommand = new DelegateCommand(() => service?.Reset());

            ShuffleCommand = new DelegateCommand(
                executeMethod: async () => await ShuffleAsync(),
                canExecuteMethod: () => Entries?.Count > 1);

            eventAggregator?.GetEvent<StackVisibleEvent>().Subscribe(
                action: UpdateService,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<StackUpdatedEvent>().Subscribe(
                action: _ => UpdateEntries(),
                threadOption: ThreadOption.PublisherThread,
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
                        service?.Select(value);
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

        #endregion Public Properties

        #region Private Methods

        private async Task ShuffleAsync()
        {
            if (service != default)
            {
                var result = await dialogService.GetMessageBoxResultAsync(
                    contentMessage: Texts.StackShuffleMessage,
                    contentTitle: Texts.StackShuffleHeader,
                    buttonDefinitions: ButtonEnum.YesNo,
                    enterDefaultButton: ClickEnum.No);

                if (result == ButtonResult.Yes)
                {
                    service.Shuffle();
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

                if (service?.Entries?.Count > 0)
                {
                    var hasChanged = Entries.Count != service.Entries.Count
                        || !Entries.SequenceEqual(service.Entries);

                    if (hasChanged)
                    {
                        entry = default;
                        RaisePropertyChanged(nameof(Entry));

                        for (var index = Entries.Count - 1; index >= 0; index--)
                        {
                            if (!service.Entries.Contains(Entries[index]))
                            {
                                Entries.RemoveAt(index);
                            }
                        }

                        for (var index = 0; index < service.Entries.Count; index++)
                        {
                            var serviceEntry = service.Entries[index];

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

        private void UpdateService(IStackService service)
        {
            if (this.service != service)
            {
                this.service = service;

                UpdateEntries();
            }
        }

        #endregion Private Methods
    }
}