using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ATL.AudioData;

using ATL.Playlist;

using Prism.Events;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.TrackService.Extensions;

namespace SportDJ.Services.TrackService
{
    public class Service
        : ITrackService
    {
        #region Private Fields

        private readonly AudioDataIOFactory audioDataIOFactory;
        private readonly IEventAggregator eventAggregator;
        private readonly IEnumerable<ATL.Format> formatsAudio;
        private readonly IEnumerable<ATL.Format> formatsPlaylist;
        private readonly object lockTracks = new();
        private readonly PlaylistIOFactory playlistIOFactory;
        private readonly IStorageService<Set> settingsService;
        private readonly Dictionary<string, Track> tracks = [];

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            playlistIOFactory = PlaylistIOFactory.GetInstance();
            audioDataIOFactory = AudioDataIOFactory.GetInstance();

            formatsAudio = audioDataIOFactory.getFormats()
                .Where(f => f.Readable).ToArray();
            formatsPlaylist = playlistIOFactory.getFormats()
                .Where(f => f.Readable).ToArray();
        }

        #endregion Public Constructors

        #region Public Methods

        public void Begin(Track track)
        {
            ArgumentNullException.ThrowIfNull(track);

            var register = new Register
            {
                Start = DateTime.Now
            };

            track.Registers.Add(register);

            eventAggregator.GetEvent<TrackUpdatedEvent>()?.Publish(track);
        }

        public void End(Track track)
        {
            ArgumentNullException.ThrowIfNull(track);

            var register = track.Registers.LastOrDefault();

            if (register.Start != default
                && register.End == default)
            {
                register.End = DateTime.Now;
            }
        }

        public int GetCount(Track track)
        {
            ArgumentNullException.ThrowIfNull(track);

            var result = tracks[track.Path].Registers.Count;

            return result;
        }

        public IEnumerable<Track> GetTracks(IEnumerable<string> paths)
        {
            var files = GetFiles(paths)
                .Select(Path.GetFullPath).ToArray();

            if (files?.Length > 0)
            {
                foreach (var file in files)
                {
                    AddTrack(file);

                    var result = tracks.TryGetValue(
                        key: file,
                        value: out var value)
                        ? value
                        : default;

                    if (result != default)
                    {
                        yield return result;
                    }
                }
            }
        }

        public void Render(Track track)
        {
            Task.Run(() => RenderTrack(track));
        }

        #endregion Public Methods

        #region Private Methods

        private void AddTrack(string file)
        {
            var atlTrack = File.Exists(file)
                ? new ATL.Track(file)
                : default;

            if (atlTrack != default)
            {
                var info = atlTrack.GetInfo();
                var duration = TimeSpan.FromSeconds(atlTrack.Duration);

                var track = default(Track);

                lock (lockTracks)
                {
                    if (!tracks.ContainsKey(file))
                    {
                        // This must be checked here to avoid problems due to track duplicates in different playlists

                        track = new Track(
                            path: file,
                            title: atlTrack.Title,
                            artist: atlTrack.Artist,
                            info: info,
                            duration: duration,
                            bpm: atlTrack.BPM);

                        tracks.Add(
                            key: file,
                            value: track);
                    }
                }

                if (track != default)
                {
                    eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);

                    Task.Run(() => TrimTrack(track));
                }
            }
        }

        private IEnumerable<string> GetFiles(IEnumerable<string> paths)
        {
            if (paths?.Any() == true)
            {
                var relevants = paths?
                    .Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

                foreach (var relevant in relevants)
                {
                    if (File.Exists(relevant)
                        && formatsAudio.Any(f => f.IsValidExtension(Path.GetExtension(relevant))))
                    {
                        yield return relevant;
                    }

                    var filePaths = default(IEnumerable<string>);

                    if (File.Exists(relevant)
                        && formatsPlaylist.Any(f => f.IsValidExtension(Path.GetExtension(relevant))))
                    {
                        filePaths = playlistIOFactory.GetPlaylistIO(relevant).FilePaths;
                    }
                    else if (Directory.Exists(relevant))
                    {
                        filePaths = Directory.GetFiles(
                            path: relevant,
                            searchPattern: "*.*",
                            searchOption: SearchOption.TopDirectoryOnly)
                            .Where(p => formatsAudio.Any(f => f.IsValidExtension(Path.GetExtension(p)))
                                || formatsPlaylist.Any(f => f.IsValidExtension(Path.GetExtension(p)))).ToArray();
                    }

                    var files = GetFiles(filePaths).ToArray();

                    foreach (var file in files)
                    {
                        yield return file;
                    }
                }
            }
        }

        private void RenderTrack(Track track)
        {
            if (track.Bitmap == default)
            {
                track.Render();

                if (track.Bitmap != default)
                {
                    eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
                }
            }
        }

        private void TrimTrack(Track track)
        {
            if (!track.IsChecked)
            {
                var trimVolume = settingsService.Contents.Options.TrimVolumePercentage / (float)Constants.PercentageMax;
                var trimRearPortion = settingsService.Contents.Options.TrimRearPercentage / Constants.PercentageMax;

                track.Trim(
                    cueVolume: trimVolume,
                    trimRearPortion: trimRearPortion);

                eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
            }
        }

        #endregion Private Methods
    }
}