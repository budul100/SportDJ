using Prism.Events;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Services.StackService.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SportDJ.Services.StackService
{
    public class Service
        : IStackService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly ITrackService trackService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, ITrackService trackService)
        {
            this.eventAggregator = eventAggregator;
            this.trackService = trackService;
        }

        #endregion Public Constructors

        #region Public Properties

        public Track Track { get; private set; }

        public IEnumerable<Track> Tracks { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Clear()
        {
            Tracks = default;
            Track = default;

            eventAggregator.GetEvent<StackUpdatedEvent>().Publish(default);
        }

        public void Initialize(string path)
        {
            var tracks = trackService.GetTracks(path).ToArray();

            UpdateStack(tracks);
        }

        public void Next()
        {
            if (Track?.Next != default)
            {
                Select(Track.Next);
            }
        }

        public void Previous()
        {
            if (Track?.Previous != default)
            {
                Select(Track.Previous);
            }
        }

        public void Select(Track track)
        {
            if (Tracks?.Contains(track) == true
                && Track != track)
            {
                Track = track;

                eventAggregator.GetEvent<StackUpdatedEvent>()?.Publish(this);
            }
        }

        public void Shuffle()
        {
            if (Tracks?.Any() == true)
            {
                var tracks = Tracks.ToArray();
                var count = tracks.Length;

                while (count > 1)
                {
                    var i = Random.Shared.Next(count--);

                    (tracks[i], tracks[count]) = (tracks[count], tracks[i]);
                }

                UpdateStack(tracks);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void UpdateStack(IEnumerable<Track> tracks)
        {
            Tracks = tracks.GetTracks().ToArray();

            Select(Tracks?.FirstOrDefault());
        }

        #endregion Private Methods
    }
}