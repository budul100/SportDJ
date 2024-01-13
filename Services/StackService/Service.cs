using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prism.Events;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Stack;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.StackService.Extensions;

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

        public Entry Entry { get; private set; }

        public bool HasPlayeds => Entries.HasPlayeds();

        public bool HasUnplayeds => Entries.HasUnplayeds(Track);

        public int Index { get; private set; }
        public Track Track => Entry?.Track;

        #endregion Public Properties

        #region Public Methods

        public void Initialize(string path, string name, bool savePosition)
        {
            var tracks = trackService
                .GetTracks(path).ToList();

            Entries = tracks.GetEntries().ToList();

            position = GetPosition(
                playerName: name,
                savePosition: savePosition);

            SetTrack();

            if (savePosition)
            {
                Next();
            }

            eventAggregator.GetEvent<StackDefinedEvent>()?.Publish(this);
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

                    if (!Entries[index].Track.IsSilent
                        && !Entries[index].Played)
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
                Entry.Played = true;

                if (Entries.AllPlayed())
                {
                    Reset();
                }

                position.Index = Index;

                sessionService.Save();

                eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(Entry);
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

                    if (!Entries[index].Track.IsSilent
                        && !Entries[index].Played)
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

            eventAggregator.GetEvent<EntriesUpdatedEvent>()?.Publish(Entries);
        }

        public void Select(int index)
        {
            if (index < Entries.Count)
            {
                if (Entry != default)
                {
                    Entry.Selected = false;
                }

                Entry = Entries[index];
                Entry.Selected = true;

                eventAggregator.GetEvent<EntriesUpdatedEvent>()?.Publish(Entries);

                Index = index;
                position.Path = Track.Path;

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

                eventAggregator.GetEvent<StackDefinedEvent>()?.Publish(this);

                SetTrack();
            }
        }

        #endregion Public Methods

        #region Private Methods

        private Position GetPosition(string playerName, bool savePosition)
        {
            if (!savePosition)
            {
                return new Position();
            }
            else
            {
                if (!sessionService.Contents.Positions.Any(p => p.Name == playerName))
                {
                    var position = new Position
                    {
                        Name = playerName,
                    };

                    sessionService.Contents.Positions.Add(position);
                }

                var result = sessionService.Contents.Positions
                    .First(p => p.Name == playerName);

                return result;
            }
        }

        private void SetTrack()
        {
            if (Entries?.Any(e => e.Track.Path == position.Path) != true
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