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
    public class Service(IEventAggregator eventAggregator, IStorageService<Session> sessionService,
        ITrackService trackService)
        : IStackService
    {
        #region Private Fields

        private static readonly object lockPositions = new();
        private Position position;

        #endregion Private Fields

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

        public void Initialize(IEnumerable<string> paths, string name, bool resetPosition, bool isLoop)
        {
            var tracks = trackService
                .Get(paths).ToArray();

            Entries = GetEntries(tracks).ToList();

            position = GetPosition(
                playerName: name,
                resetPosition: resetPosition);

            if (resetPosition
                || Entries?.Any(e => e.Track.Path == position.Path) != true
                || position.Index >= Entries.Count)
            {
                Select(0);
            }
            else
            {
                Index = Entries[position.Index].Track != Track
                    ? Entries.IndexOf(Entries.First(e => e.Track.Path == position.Path))
                    : position.Index;

                if (isLoop)
                {
                    Select(Index);
                }
                else
                {
                    Next();
                }
            }
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

                Entry.IsPlayed = true;

                eventAggregator.GetEvent<EntryUpdatedEvent>().Publish(Entry);

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
                        Select(index);

                        return;
                    }
                }
                while (Index != index);

                Select(Index);
            }
        }

        public void Reset()
        {
            if (Entries?.Count > 0)
            {
                foreach (var entry in Entries)
                {
                    entry.IsPlayed = false;

                    eventAggregator.GetEvent<EntryUpdatedEvent>().Publish(entry);
                }
            }

            eventAggregator.GetEvent<StackResetEvent>().Publish(this);
        }

        public void Select(Entry entry)
        {
            var index = Entries.IndexOf(entry);

            Select(index);
        }

        public void Select(int index)
        {
            if (Entry?.Track.IsSilent == false)
            {
                Entry.IsSelected = false;
            }

            eventAggregator.GetEvent<EntryUpdatedEvent>().Publish(Entry);

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

            eventAggregator.GetEvent<EntryUpdatedEvent>().Publish(Entry);

            Index = index;

            eventAggregator.GetEvent<StackUpdatedEvent>().Publish(this);
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

                Select(Entry);
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
                        selector: Select);

                    yield return result;
                }
            }
        }

        private Position GetPosition(string playerName, bool resetPosition)
        {
            var result = default(Position);

            if (resetPosition)
            {
                result = new Position();
            }
            else
            {
                lock (lockPositions)
                {
                    if (!sessionService.Contents.Positions.Any(p => p.Name == playerName))
                    {
                        var position = new Position
                        {
                            Name = playerName,
                        };

                        sessionService.Contents.Positions.Add(position);
                    }

                    result = sessionService.Contents.Positions
                        .First(p => p.Name == playerName);
                }
            }

            return result;
        }

        #endregion Private Methods
    }
}