using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace SportDJ.Shareds.SampleProviders.Windows.Extensions
{
    public static class ContentExtensions
    {
        #region Public Methods

        public static async Task<float[]> ExtractAsync(this string filePath,
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => ExtractSamples(
                filePath: filePath,
                cancellationToken: cancellationToken),
                cancellationToken: cancellationToken);
        }

        #endregion Public Methods

        #region Private Methods

        private static float[] ExtractSamples(string filePath, CancellationToken cancellationToken)
        {
            var result = default(float[]);

            try
            {
                // Open file with FileShare.Read, to not block the playback
                using var stream = new FileStream(
                    path: filePath,
                    mode: FileMode.Open,
                    access: FileAccess.Read,
                    share: FileShare.Read);

                using var reader = new StreamMediaFoundationReader(stream);

                // Convert analysis data to mono
                var sampleProvider = reader.ToSampleProvider();

                var monoProvider = reader.WaveFormat.Channels == 1
                    ? sampleProvider
                    : new StereoToMonoSampleProvider(sampleProvider);

                var buffer = new List<float>();
                var readBuffer = new float[reader.WaveFormat.SampleRate];

                int read;

                while (!cancellationToken.IsCancellationRequested
                    && (read = monoProvider.Read(
                        buffer: readBuffer,
                        offset: 0,
                        count: readBuffer.Length)) > 0)
                {
                    for (var i = 0; i < read; i++)
                    {
                        buffer.Add(readBuffer[i]);
                    }
                }

                result = buffer.ToArray();
            }
            catch
            { }

            return result;
        }

        #endregion Private Methods
    }
}