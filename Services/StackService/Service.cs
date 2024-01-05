using Prism.Events;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
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
        private readonly IStorageService<Session> sessionService;
        private readonly ITrackService trackService;

        private Position position;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Session> sessionService,
            ITrackService trackService)
        {
            this.eventAggregator = eventAggregator;
            this.sessionService = sessionService;
            this.trackService = trackService;
        }

        #endregion Public Constructors

        #region Public Properties

        public List<Entry> Entries { get; private set; }

        public bool HasUnplayeds => Entries.HasUnplayeds(Track);

        public Track Track { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Initialize(string path, string controllerName, bool savePosition)
        {
            var tracks = trackService
                .GetTracks(path).ToList();

            Entries = tracks.GetEntries().ToList();

            position = GetPosition(
                controllerName: controllerName,
                savePosition: savePosition);

            SetTrack();
        }

        public void Next()
        {
            if (Entries?.Any() == true)
            {
                var index = position.Index;

                do
                {
                    index++;

                    if (index == Entries.Count)
                    {
                        index = 0;
                    }

                    if (!Entries[index].Played)
                    {
                        Select(index);
                        break;
                    }
                }
                while (position.Index != index);
            }
        }

        public void Play()
        {
            if (Track != default)
            {
                Entries[position.Index].Played = true;

                if (Entries.AllPlayed())
                {
                    Reset();
                }
            }
        }

        public void Previous()
        {
            if (Entries?.Any() == true)
            {
                var index = position.Index;

                do
                {
                    if (index == 0)
                    {
                        index = Entries.Count;
                    }

                    index--;

                    if (!Entries[index].Played)
                    {
                        Select(index);

                        break;
                    }
                }
                while (position.Index != index);
            }
        }

        public void Reset()
        {
            Entries.Reset();
        }

        public void Select(int index)
        {
            if (index < Entries.Count)
            {
                Track = Entries[index].Track;

                position.Index = index;
                position.Path = Track.Path;

                sessionService.Save();

                eventAggregator.GetEvent<StackUpdatedEvent>()?.Publish(this);
            }
        }

        public void Shuffle()
        {
            if (Entries?.Any() == true)
            {
                var count = Entries.Count;

                while (count > 1)
                {
                    var index = Random.Shared.Next(count--);

                    (Entries[index], Entries[count]) = (Entries[count], Entries[index]);
                }

                SetTrack();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Position GetPosition(string controllerName, bool savePosition)
        {
            if (!savePosition)
            {
                return new Position();
            }
            else
            {
                if (!sessionService.Contents.Positions.Any(p => p.Name == controllerName))
                {
                    var position = new Position
                    {
                        Name = controllerName,
                    };

                    sessionService.Contents.Positions.Add(position);
                }

                return sessionService.Contents.Positions
                    .First(p => p.Name == controllerName);
            }
        }

        private void SetTrack()
        {
            if (Entries?.Any(e => e.Track == Track) != true
                || position.Index >= Entries.Count)
            {
                Select(0);
            }
            else if (Entries[position.Index].Track != Track)
            {
                var index = Entries.IndexOf(Entries.First(e => e.Track.Path == position.Path));

                Select(index);
            }
        }

        #endregion Private Methods
    }
}