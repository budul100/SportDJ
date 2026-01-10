using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.StackModule.ViewModels
{
    public class StackViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private IStackService service;
        private HashSet<Track> tracks = [];

        #endregion Private Fields

        #region Public Constructors

        public StackViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            EventAggregator = eventAggregator;

            SelectCommand = new DelegateCommand<Entry>(e => service?.Select(e));

            eventAggregator?.GetEvent<StackVisibleEvent>().Subscribe(
                action: UpdateService,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<Entry> Entries { get; private set; } = [];

        public IEventAggregator EventAggregator { get; }

        public DelegateCommand<Entry> SelectCommand { get; }

        #endregion Public Properties

        #region Private Methods

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