using NAudio.Wave;
using Prism.Events;
using SportDJ.Commons.Models.Contents;

namespace SportDJ.Services.SignalService.Providers
{
    internal abstract class BaseSampleProvider
        : ISampleProvider
    {
        #region Protected Fields

        protected readonly IEventAggregator eventAggregator;

        protected int averageBytesPerSecond;
        protected ISampleProvider source;
        protected Track track;

        #endregion Protected Fields

        #region Protected Constructors

        protected BaseSampleProvider(IEventAggregator eventAggregator, ISampleProvider source, Track track)
        {
            this.eventAggregator = eventAggregator;

            this.source = source;
            this.track = track;

            averageBytesPerSecond = source.WaveFormat.AverageBytesPerSecond;
        }

        #endregion Protected Constructors

        #region Public Properties

        public WaveFormat WaveFormat => source.WaveFormat;

        #endregion Public Properties

        #region Public Methods

        public abstract int Read(float[] buffer, int offset, int count);

        #endregion Public Methods
    }
}