using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATL.AudioData;
using ATL.Playlist;
using PropertyModels.Extensions;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Track;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Contents;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.TrackService.Extensions;

namespace SportDJ.Services.TrackService;

public class Service
    : ITrackService, IDisposable
{
    #region Private Fields

    private readonly ConcurrentQueue<Track> activeRenders = [];
    private readonly ConcurrentQueue<Track> activeUpdates = [];
    private readonly AudioDataIOFactory audioDataIOFactory;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly IEventAggregator eventAggregator;
    private readonly IEnumerable<ATL.Format> formatsAudio;
    private readonly IEnumerable<ATL.Format> formatsPlaylist;
    private readonly ConcurrentQueue<Track> loadedRenders = [];
    private readonly ConcurrentQueue<Track> loadedTrims = [];
    private readonly ConcurrentQueue<Track> loadedUpdates = [];
    private readonly ConcurrentQueue<Track> otherTrims = [];
    private readonly ConcurrentQueue<Track> otherUpdates = [];
    private readonly PlaylistIOFactory playlistIOFactory;
    private readonly IStorageService<Set> settingsService;
    private readonly ConcurrentDictionary<string, Track> tracks = [];

    private bool isDisposed;

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

        var cancellationToken = cancellationTokenSource.Token;

        Task.Run(
            action: () => ShapeTracks(cancellationToken),
            cancellationToken: cancellationToken);
    }

    #endregion Public Constructors

    #region Public Methods

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    public IEnumerable<Track> Get(IEnumerable<string> paths)
    {
        var filePaths = GetFiles(paths)
            .Select(Path.GetFullPath).ToArray();

        if (filePaths?.Length > 0)
        {
            foreach (var filePath in filePaths)
            {
                var result = GetTrack(filePath);

                if (result != default)
                {
                    otherUpdates.Enqueue(result);
                    otherTrims.Enqueue(result);

                    yield return result;
                }
            }
        }
    }

    public void Play(Track track, bool isPause)
    {
        ArgumentNullException.ThrowIfNull(track);

        if (!isPause)
        {
            track.Counter++;
        }

        var register = new Register
        {
            Start = DateTime.Now
        };

        track.Registers.Add(register);

        activeUpdates.Enqueue(track);
        activeRenders.Enqueue(track);

        eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
    }

    public IEnumerable<Track> Search(string search)
    {
        var result = default(IEnumerable<Track>);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            result = tracks
                .Where(p => p.Value.Contains(search))
                .Select(p => p.Value).ToArray();
        }

        return result;
    }

    public void Stop(Track track)
    {
        ArgumentNullException.ThrowIfNull(track);

        var register = track.Registers.LastOrDefault();

        if (register.Start != default
            && register.End == default)
        {
            register.End = DateTime.Now;
        }
    }

    public void Update(Track track)
    {
        if (track != default)
        {
            loadedUpdates.Enqueue(track);
            loadedTrims.Enqueue(track);
            loadedRenders.Enqueue(track);
        }
    }

    #endregion Public Methods

    #region Protected Methods

    protected virtual void Dispose(bool isDisposing)
    {
        if (!isDisposed)
        {
            if (isDisposing)
            {
                cancellationTokenSource.Cancel();
            }

            isDisposed = true;
        }
    }

    #endregion Protected Methods

    #region Private Methods

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

    private Track GetTrack(string path)
    {
        var result = default(Track);

        if (File.Exists(path))
        {
            result = tracks.TryGetValue(
                key: path,
                value: out var value)
                ? value
                : default;

            if (result == default)
            {
                var file = Path.GetFileNameWithoutExtension(path);

                result = new Track(
                    path: path,
                    file: file)
                {
                    Info = file
                };

                tracks.TryAdd(
                    key: path,
                    value: result);
            }
        }

        return result;
    }

    private void RenderTrack(Track track)
    {
        if (track?.IsRendered == false)
        {
            track.Render();

            if (track.Bitmap != default)
            {
                eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
            }
        }
    }

    private void ShapeTracks(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!activeUpdates.IsEmpty
                && activeUpdates.TryDequeue(out Track activeUpdate))
            {
                UpdateTrack(activeUpdate);
            }
            else if (!activeRenders.IsEmpty
                && activeRenders.TryDequeue(out Track activeRender))
            {
                RenderTrack(activeRender);
            }
            else if (!loadedUpdates.IsEmpty
                && loadedUpdates.TryDequeue(out Track loadedUpdate))
            {
                UpdateTrack(loadedUpdate);
            }
            else if (!loadedTrims.IsEmpty
                && loadedTrims.TryDequeue(out Track loadedTrim))
            {
                TrimTrack(loadedTrim);
            }
            else if (!otherUpdates.IsEmpty
                && otherUpdates.TryDequeue(out Track otherUpdate))
            {
                UpdateTrack(otherUpdate);
            }
            else if (!loadedRenders.IsEmpty
                && loadedRenders.TryDequeue(out Track loadedRender))
            {
                RenderTrack(loadedRender);
            }
            else if (!otherTrims.IsEmpty
                && otherTrims.TryDequeue(out Track otherTrim))
            {
                TrimTrack(otherTrim);
            }
        }
    }

    private void TrimTrack(Track track)
    {
        if (track?.IsTrimmed == false)
        {
            var currentDuration = track.Duration;

            var trimVolume = settingsService.Contents.Options.TrimVolumePercentage
                / (float)Constants.OptionsPercentageMax;
            var trimRearPortion = settingsService.Contents.Options.TrimRearPercentage
                / Constants.OptionsPercentageMax;

            track.Trim(
                cueVolume: trimVolume,
                trimRearPortion: trimRearPortion);

            if (track.Duration != currentDuration)
            {
                eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
            }
        }
    }

    private void UpdateTrack(Track track)
    {
        if (track?.IsUpdated == false)
        {
            var hashCode = track.GetHashCode();

            track.Update();

            if (track.GetHashCode() != hashCode)
            {
                eventAggregator.GetEvent<TrackUpdatedEvent>().Publish(track);
            }
        }
    }

    #endregion Private Methods
}
