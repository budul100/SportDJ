using NAudio.Wave;
using NAudio.WaveFormRenderer;
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
                    TopPeakPen = new Pen(Color.LightGreen),
                    BottomPeakPen = new Pen(Color.DarkGreen),
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

        public static void Trim(this Track track, float trimVolume, double trimRearPortion = 0.1)
        {
            if (track != default
                && File.Exists(track.Path))
            {
                using var input = new AudioFileReader(track.Path);

                var start = default(TimeSpan?);
                var end = default(TimeSpan?);

                var startForRearCheck = trimRearPortion < 1
                    ? 1 - trimRearPortion
                    : 0;
                var lastPosition = TimeSpan.Zero;

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

                    for (var i = 0; i < read / 4; i++)
                    {
                        if (BitConverter.ToSingle(buffer, i * 4) > trimVolume)
                        {
                            if (start == default)
                            {
                                start = input.CurrentTime;

                                if (input.Position < input.Length * startForRearCheck)
                                {
                                    input.Position = (long)(input.Length * startForRearCheck);
                                }
                            }

                            end = default;
                            lastPosition = input.CurrentTime;
                        }
                        else if (start != default
                            && end == default)
                        {
                            end = lastPosition;
                        }
                    }
                }

                start ??= TimeSpan.Zero;
                end ??= input.TotalTime;

                if (start <= end)
                {
                    track.Start = start.Value;
                    track.Length = end.Value.Subtract(start.Value);
                    track.Trimmed = true;
                }

                track.Checked = true;
            }
        }

        #endregion Public Methods
    }
}