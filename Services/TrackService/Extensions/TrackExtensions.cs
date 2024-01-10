using NAudio.Wave;
using NAudio.WaveFormRenderer;
using SportDJ.Commons;
using SportDJ.Commons.Models.Contents;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace SportDJ.Services.TrackService.Extensions
{
    internal static class TrackExtensions
    {
        #region Private Fields

        private static readonly Avalonia.Vector dpi = new Avalonia.Vector(
            x: 96,
            y: 96);

        #endregion Private Fields

        #region Public Methods

        public static void Render(this Track track)
        {
            if (track != default
                && File.Exists(track.Path))
            {
                var settings = new StandardWaveFormRendererSettings
                {
                    BackgroundColor = Color.Transparent,
                    TopPeakPen = new Pen(Constants.ColorPeakTop),
                    BottomPeakPen = new Pen(Constants.ColorPeakBottom)
                };

                using var input = new AudioFileReader(track.Path);

                var renderer = new WaveFormRenderer();
                var peakProvider = new MaxPeakProvider();

                var image = renderer.Render(
                    waveStream: input,
                    peakProvider: peakProvider,
                    settings: settings);

                if (image != default)
                {
                    var temporary = new Bitmap(image);

                    var rectangle = new Rectangle(
                        x: 0,
                        y: 0,
                        width: temporary.Width,
                        height: temporary.Height);

                    var bitmapdata = temporary.LockBits(
                        rect: rectangle,
                        flags: ImageLockMode.ReadWrite,
                        format: PixelFormat.Format32bppArgb);

                    var size = new Avalonia.PixelSize(
                        width: bitmapdata.Width,
                        height: bitmapdata.Height);

                    track.Bitmap = new Avalonia.Media.Imaging.Bitmap(
                        format: Avalonia.Platform.PixelFormat.Bgra8888,
                        alphaFormat: Avalonia.Platform.AlphaFormat.Premul,
                        data: bitmapdata.Scan0,
                        size: size,
                        dpi: dpi,
                        stride: bitmapdata.Stride);

                    temporary.UnlockBits(bitmapdata);
                    temporary.Dispose();
                }
            }
        }

        public static void Trim(this Track track, float cueVolume, double trimRearPortion = 0.1)
        {
            try
            {
                if (track != default
                    && File.Exists(track.Path))
                {
                    using var input = new AudioFileReader(track.Path);

                    lock (input)
                    {
                        var start = default(long?);
                        var end = default(long?);

                        var from = default(TimeSpan?);
                        var to = default(TimeSpan?);

                        var cue = default(long?);

                        var rearCheckFrom = trimRearPortion < 1
                            ? 1 - trimRearPortion
                            : 0;

                        var blockAlign = input.WaveFormat.BlockAlign;
                        var buffer = new byte[blockAlign];
                        var read = 0;

                        while (input.Position < input.Length)
                        {
                            read = input.Read(
                                buffer: buffer,
                                offset: 0,
                                count: blockAlign);

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

                        if (start.HasValue
                            && end.HasValue
                            && start < end)
                        {
                            track.Start = start.Value;
                            track.End = end.Value;
                            track.Cue = cue;

                            track.Length = to.Value.Subtract(from.Value);
                        }
                        else
                        {
                            track.IsSilent = true;
                        }

                        track.IsChecked = true;
                    }
                }
            }
            catch
            { }
        }

        #endregion Public Methods
    }
}