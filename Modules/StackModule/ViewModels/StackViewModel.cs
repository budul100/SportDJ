using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia.Enums;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Panel;
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
        private Entry active;
        private IStackService service;
        private HashSet<Track> tracks = [];

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
            ShuffleCommand = new DelegateCommand(async () => await ShuffleAsync());

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

        public Entry Active
        {
            get => active;
            set
            {
                if (value != default
                    && SetProperty(ref active, value))
                {
                    service?.Select(value);
                }
            }
        }

        public ObservableCollection<Entry> Entries { get; private set; } = [];

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
            Entries.Clear();

            if (service?.Entries?.Count > 0)
            {
                tracks = [.. service.Entries.Select(e => e.Track)];

                foreach (var entry in service.Entries)
                {
                    Entries.Add(entry);
                }
            }
            else
            {
                tracks.Clear();
            }

            RaisePropertyChanged(nameof(Entries));
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