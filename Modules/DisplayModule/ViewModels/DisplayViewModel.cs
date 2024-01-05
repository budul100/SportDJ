using Avalonia.Threading;
using Prism.Mvvm;
using System;

namespace SportDJ.Modules.DisplayModule.ViewModels
{
    public class DisplayViewModel
        : BindableBase, IDisposable
    {
        #region Private Fields

        private const string ClockFormat = "HH:mm:ss";

        private readonly DispatcherTimer clockTimer;

        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public DisplayViewModel()
        {
            clockTimer = GetClockTimer();
            clockTimer.Start();
        }

        #endregion Public Constructors

        #region Public Properties

        public static string Clock => DateTime.Now.ToString(ClockFormat);

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    clockTimer.Stop();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private DispatcherTimer GetClockTimer()
        {
            var result = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            result.Tick += (s, e) => RaisePropertyChanged(nameof(ViewModels.DisplayViewModel.Clock));

            return result;
        }

        #endregion Private Methods
    }
}