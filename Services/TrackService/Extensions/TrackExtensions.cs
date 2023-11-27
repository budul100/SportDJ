using NAudio.Wave;
using SportDJ.Commons.Models.Contents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SportDJ.Services.TrackService.Extensions
{
    internal static class TrackExtensions
    {
        #region Public Methods

        public static IEnumerable<Track> GetTracks(this IEnumerable<Track> tracks)
        {
            if (tracks?.Any() == true)
            {
                tracks.Last().Next = tracks.First();

                var previous = tracks.Last();

                foreach (var track in tracks)
                {
                    track.Previous = previous;
                    previous.Next = track;

                    yield return track;

                    previous = track;
                }
            }
        }

        public static void Trim(this Track track, float trimVolume, double trimRearPortion = 0.1)
        {
            if (track != default
                && File.Exists(track.Path))
            {
                using var input = new AudioFileReader(track.Path);

                var start = TimeSpan.Zero;
                var end = input.TotalTime;

                var startForRearCheck = trimRearPortion < 1
                    ? 1 - trimRearPortion
                    : 0;

                var lastPosition = TimeSpan.Zero;
                var isTrimRearFound = false;

                var blockAlign = input.WaveFormat.BlockAlign;
                var buffer = new byte[blockAlign];
                var read = 0;

                while (input.Position < input.Length)
                {
                    read = input.Read(buffer, 0, blockAlign);

                    if (read == 0)
                    {
                        break;
                    }

                    for (var i = 0; i < read / 4; i++)
                    {
                        if (BitConverter.ToSingle(buffer, i * 4) > trimVolume)
                        {
                            if (start == TimeSpan.Zero)
                            {
                                start = input.CurrentTime;

                                if (input.Position < input.Length * startForRearCheck)
                                {
                                    input.Position = (long)(input.Length * startForRearCheck);
                                }
                            }

                            isTrimRearFound = false;
                            lastPosition = input.CurrentTime;
                        }
                        else if ((start > TimeSpan.Zero) && !isTrimRearFound)
                        {
                            isTrimRearFound = true;
                            end = lastPosition;
                        }
                    }
                }

                if (start <= end)
                {
                    track.Start = start;
                    track.Length = end.Subtract(start);
                    track.Trimmed = true;
                }

                track.Checked = true;
            }
        }

        #endregion Public Methods
    }
}