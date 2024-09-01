using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using SportDJ.Commons.Events.Entry;
using SportDJ.Commons.Events.Stack;
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

        public bool HasUnplayeds => Entries.HasUnplayeds(
            current: Entry);

        public int Index { get; private set; }

        public Track Track => Entry?.Track;

        #endregion Public Properties

        #region Public Methods

        public void Initialize(IEnumerable<string> paths, string name, bool savePosition, bool isLoop)
        {
            var tracks = trackService
                .GetTracks(paths).ToArray();

            Entries = GetEntries(tracks).ToList();

            position = GetPosition(
                playerName: name,
                savePosition: savePosition);

            SetTrack();

            if (savePosition)
            {
                if (isLoop)
                {
                    SelectEntry(position.Index);
                }
                else
                {
                    Next();
                }
            }

            eventAggregator.GetEvent<StackDefinedEvent>()?.Publish(this);
        }

        public void Next()
        {
            if (Entries?.Count > 0)
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
                        SelectEntry(index);

                        return;
                    }
                }
                while (Index != index);

                SelectEntry(Index);
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

                Entry.IsPlayed = true;

                eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(Entry);

                position.Index = Index;
                position.Path = Track.Path;

                sessionService.Save();
            }
        }

        public void Previous()
        {
            if (Entries?.Count > 0)
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
                        && !Entries[index].IsPlayed)
                    {
                        SelectEntry(index);

                        return;
                    }
                }
                while (Index != index);

                SelectEntry(Index);
            }
        }

        public void Reset()
        {
            if (Entries?.Count > 0)
            {
                foreach (var entry in Entries)
                {
                    entry.IsPlayed = false;

                    eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(entry);
                }
            }

            eventAggregator.GetEvent<StackResetEvent>()?.Publish(this);
        }

        public void Shuffle()
        {
            if (Entries?.Count > 0)
            {
                var count = Entries.Count;

                while (count > 1)
                {
                    var index = Random.Shared.Next(count--);

                    (Entries[index], Entries[count]) = (Entries[count], Entries[index]);
                }

                eventAggregator.GetEvent<StackDefinedEvent>()?.Publish(this);

                SelectEntry(Entry);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<Entry> GetEntries(IEnumerable<Track> tracks)
        {
            if (tracks?.Any() == true)
            {
                foreach (var track in tracks)
                {
                    var result = new Entry(
                        track: track,
                        selector: SelectEntry);

                    yield return result;
                }
            }
        }

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

        private void SelectEntry(Entry entry)
        {
            var index = Entries.IndexOf(entry);

            SelectEntry(index);
        }

        private void SelectEntry(int index)
        {
            if (Entry?.Track.IsSilent == false)
            {
                Entry.IsSelected = false;
            }

            if (index < Entries.Count)
            {
                Entry = Entries[index];
                Entry.IsSelected = true;
            }
            else
            {
                Entry = default;
                index = 0;
            }

            eventAggregator.GetEvent<EntryUpdatedEvent>()?.Publish(Entry);

            Index = index;

            eventAggregator.GetEvent<StackUpdatedEvent>()?.Publish(this);
        }

        private void SetTrack()
        {
            if (Entries?.Any(e => e.Track.Path == position.Path) != true
                || position.Index >= Entries.Count)
            {
                SelectEntry(0);
            }
            else if (Entries[position.Index].Track != Track)
            {
                var index = Entries.IndexOf(Entries.First(e => e.Track.Path == position.Path));

                SelectEntry(index);
            }
        }

        #endregion Private Methods
    }
}