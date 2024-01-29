using Prism.Events;
using SportDJ.Commons.Events.Entry;
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

        public Entry Entry { get; private set; }

        public bool HasPlayeds => Entries.HasPlayeds();

        public bool HasUnplayeds => Entries.HasUnplayeds(
            current: Entry);

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
                var index = Index;

                do
                {
                    index++;

                    if (index == Entries.Count)
                    {
                        index = 0;
                    }

                    if (Entries[index].CanBePlayed()
                        && Entries[index] != Entry)
                    {
                        Select(index);

                        return;
                    }
                }
                while (Index != index);

                Select(Index);
            }
        }

        public void Play()
        {
            if (Track != default)
            {
                if (Entries.AllPlayed(Entry))
                {
                    Reset();
                }

                Entry.Played = true;

                eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(Entry);

                position.Index = Index;
                position.Path = Track.Path;

                sessionService.Save();
            }
        }

        public void Previous()
        {
            if (Entries?.Any() == true)
            {
                var index = Index;

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
                while (Index != index);

                Select(Index);
            }
        }

        public void Reset()
        {
            if (Entries?.Any() == true)
            {
                foreach (var entry in Entries)
                {
                    entry.Played = false;

                    eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(entry);
                }
            }

            eventAggregator.GetEvent<StackResetEvent>()?.Publish(this);
        }

        public void Select(int index)
        {
            if (index < Entries.Count)
            {
                if (Entry != default
                    && !Entries[index].Track.IsSilent)
                {
                    Entry.Selected = false;

                    eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(Entry);
                }

                Entry = Entries[index];
                Entry.Selected = true;

                eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(Entry);

                Index = index;

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