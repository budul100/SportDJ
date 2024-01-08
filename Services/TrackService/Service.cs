using ATL.AudioData;
using ATL.Playlist;
using Prism.Events;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.TrackService.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
        private readonly PlaylistIOFactory playlistIOFactory;
        private readonly IStorageService<Set> settingsService;
        private readonly Dictionary<string, Track> tracks = new Dictionary<string, Track>();

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            playlistIOFactory = PlaylistIOFactory.GetInstance();
            audioDataIOFactory = AudioDataIOFactory.GetInstance();

            formatsAudio = GetFormatsAudio();
            formatsPlaylist = GetFormatsPlaylist();
        }

        #endregion Public Constructors

        #region Public Methods

        public int GetCount(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var result = tracks[track.Path].Registers.Count;

            return result;
        }

        public IEnumerable<Track> GetTracks(string path)
        {
            var files = GetFiles(path)
                .Select(p => Path.GetFullPath(p)).ToArray();

            if (files?.Any() == true)
            {
                foreach (var file in files)
                {
                    AddTrack(file);

                    var result = tracks.ContainsKey(file)
                        ? tracks[file]
                        : default;

                    if (result != default)
                    {
                        yield return result;
                    }
                }
            }
        }

        public void Play(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var register = new Register
            {
                Start = DateTime.Now
            };

            track.Registers.Add(register);

            eventAggregator.GetEvent<TrackUpdatedEvent>()?.Publish(track);
        }

        public void Stop(Track track)
        {
            if (track is null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            var register = track.Registers.LastOrDefault();

            if (register.Start != default
                && register.End == default)
            {
                register.End = DateTime.Now;
            }
        }

        public void Update(Track track)
        {
            Task.Run(() => TrimTrack(track));
            Task.Run(() => RenderTrack(track));
        }

        #endregion Public Methods

        #region Private Methods

        private void AddTrack(string file)
        {
            if (File.Exists(file)
                && !tracks.ContainsKey(file))
            {
                var atlTrack = new ATL.Track(file);

                if (atlTrack != default)
                {
                    var info = atlTrack.GetInfo();
                    var length = TimeSpan.FromSeconds(atlTrack.Duration);

                    var result = new Track(
                        path: file,
                        title: atlTrack.Title,
                        artist: atlTrack.Artist,
                        info: info,
                        length: length);

                    Task.Run(() => TrimTrack(result));

                    tracks.Add(
                        key: file,
                        value: result);
                }
            }
        }

        private IEnumerable<string> GetFiles(string path)
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
                        searchOption: SearchOption.TopDirectoryOnly)
                        .Where(p => formatsAudio.Any(f => f.IsValidExtension(Path.GetExtension(p)))
                            || formatsPlaylist.Any(f => f.IsValidExtension(Path.GetExtension(p)))).ToArray();
                }

                if (relevants?.Any() == true)
                {
                    foreach (var relevant in relevants)
                    {
                        var files = GetFiles(relevant).ToArray();

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
            if (!track.Checked)
            {
                var trimVolume = settingsService.Contents.Options.TrimVolume;
                var trimRearPortion = settingsService.Contents.Options.TrimRearPortion;

                track.Trim(
                    trimVolume: trimVolume,
                    trimRearPortion: trimRearPortion);

                if (track.Trimmed)
                {
                    eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
                }
            }
        }

        #endregion Private Methods
    }
}