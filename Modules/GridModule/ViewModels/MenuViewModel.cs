using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Models.Contents;
using System;

namespace SportDJ.Modules.GridModule.ViewModels
{
    public class MenuViewModel
        : BindableBase
    {
        #region Private Fields

        private readonly Action callback;
        private readonly Entry entry;

        private string header;
        private bool isPlayed;

        private bool isSelected;

        #endregion Private Fields

        #region Public Constructors

        public MenuViewModel(IContainerProvider containerProvider, Entry entry, Action callback)
        {
            this.entry = entry;
            this.callback = callback;

            Command = new DelegateCommand(ExecuteCallback);

            var eventAggregator = containerProvider.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<EntryUpdatedEvent>().Subscribe(
                action: _ => UpdateEntry(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: e => e == entry);

            eventAggregator.GetEvent<TrackUpdatedEvent>().Subscribe(
                action: _ => UpdateTrack(),
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true,
                filter: t => t == entry.Track);

            UpdateTrack();
        }

        #endregion Public Constructors

        #region Public Events

        public event EventHandler ClosingEvent;

        #endregion Public Events

        #region Public Properties

        public DelegateCommand Command { get; }

        public string Header
        {
            get { return header; }
            set { SetProperty(ref header, value); }
        }

        public bool IsPlayed
        {
            get { return isPlayed; }
            set { SetProperty(ref isPlayed, value); }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { SetProperty(ref isSelected, value); }
        }

        #endregion Public Properties

        #region Private Methods

        private void ExecuteCallback()
        {
            callback?.Invoke();

            ClosingEvent?.Invoke(
                sender: this,
                e: default);
        }

        private void UpdateEntry()
        {
            IsPlayed = entry.Played;
            IsSelected = entry.Selected;
        }

        private void UpdateTrack()
        {
            var duration = entry.Track?.Length.ToString(Constants.FormatDuration);

            Header = $"{entry.Track.Info} ({duration}) [{entry.Track.Registers.Count}]";

            UpdateEntry();
        }

        #endregion Private Methods
    }
}