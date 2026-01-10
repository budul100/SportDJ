using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Prism.Commands;
using Prism.Events;
using Prism.Navigation.Regions;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Prism;

namespace SportDJ.Modules.StackModule.ViewModels
{
    public class StackViewModel
        : RegionViewModelBase
    {
        #region Private Fields

        private bool isUpdatingEntry;
        private bool isUpdatingTrack;
        private IStackService service;
        private HashSet<Track> tracks = [];

        #endregion Private Fields

        #region Public Constructors

        public StackViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            SelectCommand = new DelegateCommand<Entry>(e => service?.Select(e));

            eventAggregator?.GetEvent<StackVisibleEvent>().Subscribe(
                action: UpdateService,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<EntryUpdatedEvent>().Subscribe(
                action: UpdateEntry,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: Entries.Contains);

            eventAggregator?.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: UpdateTrack,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: tracks.Contains);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<Entry> Entries { get; private set; } = [];

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

        private void UpdateEntry(Entry entry)
        {
            if (isUpdatingEntry)
                return;

            try
            {
                isUpdatingEntry = true;

                var existings = Entries
                    .Where(e => e == entry).ToList();

                foreach (var existing in existings)
                {
                    var index = Entries.IndexOf(existing);
                    Entries[index] = existing;
                }
            }
            finally
            {
                isUpdatingEntry = false;
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

        private void UpdateTrack(Track track)
        {
            if (isUpdatingTrack)
                return;

            try
            {
                isUpdatingTrack = true;

                var existings = Entries
                    .Where(e => e.Track == track).ToList();

                foreach (var existing in existings)
                {
                    var index = Entries.IndexOf(existing);
                    Entries[index] = existing;
                }
            }
            finally
            {
                isUpdatingTrack = false;
            }
        }

        #endregion Private Methods
    }
}