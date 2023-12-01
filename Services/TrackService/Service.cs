using ATL.AudioData;
using ATL.Playlist;
using Prism.Events;
using SportDJ.Commons.Events.Tracks;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.TrackService.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SportDJ.Services.TrackService
{
    public class Service
        : ITrackService
    {
        #region Private Fields

        private readonly AudioDataIOFactory audioDataIOFactory;
        private readonly IDispatcherService dispatcherService;
        private readonly IEventAggregator eventAggregator;
        private readonly IEnumerable<ATL.Format> formatsAudio;
        private readonly IEnumerable<ATL.Format> formatsPlaylist;
        private readonly PlaylistIOFactory playlistIOFactory;
        private readonly ISettingsService<Session> settingsService;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IDispatcherService dispatcherService,
            ISettingsService<Session> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.dispatcherService = dispatcherService;
            this.settingsService = settingsService;

            playlistIOFactory = PlaylistIOFactory.GetInstance();
            audioDataIOFactory = AudioDataIOFactory.GetInstance();

            formatsAudio = GetFormatsAudio();
            formatsPlaylist = GetFormatsPlaylist();
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

            eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(default);
        }

        public void Load(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            var tracks = GetTracks(
                path: path,
                searchOption: searchOption).ToArray();

            SetTracks(tracks);

            dispatcherService.InvokeAsync(UpdateDurations);
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

                eventAggregator.GetEvent<TrackUpdatedEvent>()?.Publish(Track);
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

                SetTracks(tracks);
            }
        }

        #endregion Public Methods

        #region Private Methods

        private IEnumerable<string> GetFiles(string path, SearchOption searchOption)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (File.Exists(path)
                    && formatsAudio.Any(f => f.IsValidExtension(Path.GetExtension(path))))
                {
                    yield return path;
                }

                var relevants = default(IEnumerable<string>);

                if (File.Exists(path)
                    && formatsPlaylist.Any(f => f.IsValidExtension(Path.GetExtension(path))))
                {
                    relevants = playlistIOFactory.GetPlaylistIO(path).FilePaths;
                }
                else if (Directory.Exists(path))
                {
                    relevants = Directory.GetFiles(
                        path: path,
                        searchPattern: "*.*",
                        searchOption: searchOption)
                        .Where(p => formatsAudio.Any(f => f.IsValidExtension(Path.GetExtension(p)))
                            || formatsPlaylist.Any(f => f.IsValidExtension(Path.GetExtension(p)))).ToArray();
                }

                if (relevants?.Any() == true)
                {
                    foreach (var relevant in relevants)
                    {
                        var files = GetFiles(
                            path: relevant,
                            searchOption: searchOption).ToArray();

                        foreach (var file in files)
                        {
                            yield return relevant;
                        }
                    }
                }
            }
        }

        private IEnumerable<ATL.Format> GetFormatsAudio()
        {
            var result = audioDataIOFactory.getFormats()
                .Where(f => f.Readable).ToArray();

            return result;
        }

        private IEnumerable<ATL.Format> GetFormatsPlaylist()
        {
            var result = playlistIOFactory.getFormats()
                .Where(f => f.Readable).ToArray();

            return result;
        }

        private IEnumerable<Track> GetTracks(string path, SearchOption searchOption)
        {
            var files = GetFiles(
                path: path,
                searchOption: searchOption)
                .Select(p => Path.GetFullPath(p)).ToArray();

            if (files?.Any() == true)
            {
                foreach (var file in files)
                {
                    var atlTrack = new ATL.Track(file);

                    if (atlTrack != default)
                    {
                        var length = TimeSpan.FromSeconds(atlTrack.Duration);

                        var result = new Track(
                            path: file,
                            title: atlTrack.Title,
                            artist: atlTrack.Artist,
                            length: length);

                        yield return result;
                    }
                }
            }
        }

        private void SetTracks(IEnumerable<Track> tracks)
        {
            Tracks = tracks.GetTracks().ToArray();

            Select(Tracks?.FirstOrDefault());
        }

        private void UpdateDurations()
        {
            var relevants = Tracks?
                .Where(t => !t.Checked).ToArray();

            if (relevants?.Any() == true)
            {
                var trimVolume = settingsService.Settings.TrimVolume;
                var trimRearPortion = settingsService.Settings.TrimRearPortion;

                foreach (var relevant in relevants)
                {
                    relevant.Trim(
                        trimVolume: trimVolume,
                        trimRearPortion: trimRearPortion);

                    if (relevant.Trimmed)
                    {
                        eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(relevant);
                    }
                }
            }
        }

        #endregion Private Methods
    }
}