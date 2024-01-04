using Prism.Events;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Services.StackService.Extensions;
using SportDJ.Services.StackService.Models;
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

        private List<Entry> entries;
        private int index;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, ITrackService trackService)
        {
            this.eventAggregator = eventAggregator;
            this.trackService = trackService;
        }

        #endregion Public Constructors

        #region Public Properties

        public bool HasStack => entries.HasStack(Track);

        public Track Track { get; private set; }

        public IEnumerable<Track> Tracks { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Clear()
        {
            Tracks = default;
            Track = default;

            SetEntries(Tracks);

            eventAggregator.GetEvent<StackUpdatedEvent>().Publish(default);
        }

        public void Initialize(string path)
        {
            Tracks = trackService.GetTracks(path).ToArray();

            SetEntries(Tracks);
        }

        public void Next()
        {
            if (entries?.Any() == true)
            {
                var current = index;

                do
                {
                    current++;

                    if (current == entries.Count)
                    {
                        current = 0;
                    }

                    if (!entries[current].Played)
                    {
                        index = current;
                        Select(entries[index].Track);

                        break;
                    }
                }
                while (index != current);
            }
        }

        public void Play()
        {
            if (Track != default)
            {
                entries[index].Played = true;

                if (entries.AllPlayed())
                {
                    Reset();
                }
            }
        }

        public void Previous()
        {
            if (entries?.Any() == true)
            {
                var current = index;

                do
                {
                    if (current == 0)
                    {
                        current = entries.Count;
                    }

                    current--;

                    if (!entries[current].Played)
                    {
                        index = current;
                        Select(entries[index].Track);

                        break;
                    }
                }
                while (index != current);
            }
        }

        public void Reset()
        {
            entries.Reset();
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

                SetEntries(tracks);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void SetEntries(IEnumerable<Track> tracks)
        {
            entries = tracks.GetEntries().ToList();

            index = entries?.Any(e => e.Track == Track) == true
                ? entries.IndexOf(entries.First(e => e.Track == Track))
                : 0;

            var track = index < entries.Count
                ? entries[index].Track
                : default;

            Select(track);
        }

        #endregion Private Methods
    }
}