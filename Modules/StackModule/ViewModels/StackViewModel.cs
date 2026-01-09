using System.Collections.ObjectModel;
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

        #endregion Private Fields

        #region Public Constructors

        public StackViewModel(IRegionManager regionManager, IEventAggregator eventAggregator)
            : base(regionManager)
        {
            eventAggregator?.GetEvent<StackVisibleEvent>().Subscribe(
                action: UpdateEntries,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<Entry> Entries { get; private set; } = [];

        #endregion Public Properties

        #region Private Methods

        private void UpdateEntries(IStackService service)
        {
            if (this.service != service)
            {
                this.service = service;

                Entries.Clear();

                if (service?.Entries?.Count > 0)
                {
                    foreach (var entry in service.Entries)
                    {
                        Entries.Add(entry);
                    }
                }

                RaisePropertyChanged(nameof(Entries));
            }
        }

        #endregion Private Methods
    }
}