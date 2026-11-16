using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Avalonia;
using NAudio.Wave;
using NAudio.WaveFormRenderer;
using NAudio.WaveFormRenderer.RendererSettings;
using SkiaSharp;
using SportDJ.Commons.Assets;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Services.TrackService.Extensions;

internal static class TrackExtensions
{
    #region Private Fields

    private const int DPIDefault = 96;
    private const string InfoDelimiter = " - ";

    private static readonly Vector dpi = new(
        x: DPIDefault,
        y: DPIDefault);

    private static readonly Lock lockRender = new();
    private static readonly Lock lockTrim = new();

    #endregion Private Fields

    #region Public Methods

    public static bool Contains(this Track track, string search)
    {
        var result = false;

        if (track != default
            && !string.IsNullOrWhiteSpace(search))
        {
            bool containsPredicate(string s) => !string.IsNullOrWhiteSpace(s)
                && s.Contains(
                    value: search,
                    comparisonType: StringComparison.OrdinalIgnoreCase);

            result = containsPredicate(track.File)
                || containsPredicate(track.Title)
                || containsPredicate(track.Artist)
                || containsPredicate(track.Info);
        }

        return result;
    }

    public static void Render(this Track track)
    {
        if (track != default
            && File.Exists(track.Path))
        {
            var settings = new SoundCloudBlockWaveFormSettings(
                topPeakColor: Constants.ColorPeakTop,
                topSpacerStartColor: Constants.ColorPeakTop,
                bottomPeakColor: Constants.ColorPeakBottom,
                bottomSpacerColor: Constants.ColorPeakBottom)
            {
                BackgroundColor = SKColors.Transparent,
            };

            lock (lockRender)
            {
                try
                {
                    using var input = new AudioFileReader(track.Path);
                    var renderer = new WaveFormRenderer();

                    using SKBitmap skBitmap = WaveFormRenderer.Render(
                        waveStream: input,
                        settings: settings);

                    if (skBitmap != default)
                    {
                        var pixels = skBitmap.GetPixels();

                        var pixelSize = new PixelSize(
                            width: skBitmap.Width,
                            height: skBitmap.Height);

                        track.Bitmap = new Avalonia.Media.Imaging.Bitmap(
                            format: Avalonia.Platform.PixelFormat.Bgra8888,
                            alphaFormat: Avalonia.Platform.AlphaFormat.Premul,
                            data: pixels,
                            size: pixelSize,
                            dpi: dpi,
                            stride: skBitmap.RowBytes);

                        track.IsRendered = true;
                    }
                }
                catch
                {
                    //System.Diagnostics.Debug.WriteLine($"Render Error: {ex.Message}");
                }
            }

            track.IsRendered = true;
        }
    }

    public static bool Set(this IEnumerable<Track> tracks, object tracksLock, Predicate<Track> checker,
        Action<Track> setter)
    {
        var track = default(Track);

        lock (tracksLock)
        {
            if (tracks?.Count() > 0)
            {
                track = tracks.FirstOrDefault(t => checker(t));
            }
        }

        if (track != default)
        {
            setter(track);

            return true;
        }

        return false;
    }

    public static void Trim(this Track track, float cueVolume, double trimRearPortion = 0.1)
    {
        if (track != default
            && File.Exists(track.Path))
        {
            var start = default(long?);
            var end = default(long?);

            var from = default(TimeSpan?);
            var to = default(TimeSpan?);

            var cue = default(long?);

            var rearCheckFrom = trimRearPortion < 1
                ? 1 - trimRearPortion
                : 0;

            lock (lockTrim)
            {
                using var input = new AudioFileReader(track.Path);

                var blockAlign = input.WaveFormat.BlockAlign;
                var buffer = new byte[blockAlign];
                var read = 0;

                while (input.Position < input.Length)
                {
                    try
                    {
                        read = input.Read(
                            buffer: buffer,
                            offset: 0,
                            count: blockAlign);
                    }
                    catch
                    { }

                    if (read == 0)
                    {
                        break;
                    }

                    for (var index = 0; index < read / 4; index++)
                    {
                        var volume = BitConverter.ToSingle(
                            value: buffer,
                            startIndex: index * 4);

                        if (volume > Constants.VolumeMin)
                        {
                            end = input.Position;
                            to = input.CurrentTime;

                            if (!start.HasValue)
                            {
                                start = end;
                                from = to;

                                if (input.Position < input.Length * rearCheckFrom)
                                {
                                    input.Position = (long)(input.Length * rearCheckFrom);
                                    break;
                                }
                            }
                            else if (volume > cueVolume)
                            {
                                cue = input.Position;
                            }
                        }
                    }
                }
            }

            if (start.HasValue
                && end.HasValue
                && start < end)
            {
                track.Start = start.Value;
                track.End = end.Value;
                track.Cue = cue;

                track.Duration = to.Value.Subtract(from.Value);
            }
            else
            {
                track.IsSilent = true;
            }

            track.IsTrimmed = true;
        }
    }

    public static void Update(this Track track)
    {
        var atlTrack = new ATL.Track(track.Path);

        track.Info = atlTrack.GetInfo();
        track.Title = atlTrack.Title;
        track.Artist = atlTrack.Artist;
        track.BPM = atlTrack.BPM;

        if (track.Duration == default)
        {
            track.Duration = TimeSpan.FromSeconds(atlTrack.Duration);
        }

        track.IsUpdated = true;
    }

    #endregion Public Methods

    #region Private Methods

    private static string GetInfo(this ATL.Track atlTrack)
    {
        var content = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(atlTrack.Artist))
        {
            content.Append(atlTrack.Artist);
        }
        if (!string.IsNullOrWhiteSpace(atlTrack.Title))
        {
            if (content.Length > 0)
            {
                content.Append(InfoDelimiter);
            }

            content.Append(atlTrack.Title);
        }

        var result = content.ToString();

        if (string.IsNullOrWhiteSpace(result))
        {
            result = Path.GetFileNameWithoutExtension(atlTrack.Path);
        }

        return result;
    }

    #endregion Private Methods
}