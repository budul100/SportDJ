using NAudio.Wave;
using SportDJ.Commons.Models.Contents;
using System;
using System.IO;

namespace SportDJ.Services.TrackService.Extensions
{
    internal static class TrackExtensions
    {
        #region Public Methods

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