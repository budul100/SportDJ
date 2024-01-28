using System;
using System.Threading.Tasks;
using Prism.Events;
using SharpHook;
using SportDJ.Commons.Events.Key;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.HookService
{
    public class Service
        : IHookService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly TaskPoolGlobalHook hook;

        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            hook = new TaskPoolGlobalHook();

            hook.KeyPressed += OnKeyPressed;
            hook.KeyReleased += OnKeyReleased;

            Task.Run(() => hook.Run());
        }

        #endregion Public Constructors

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool isDisposing)
        {
            if (!isDisposed)
            {
                if (isDisposing)
                {
                    hook.KeyPressed -= OnKeyPressed;
                    hook.KeyReleased -= OnKeyReleased;

                    hook.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            var keyCode = e?.Data.KeyCode.ToString();

            if (!string.IsNullOrWhiteSpace(keyCode))
            {
                eventAggregator.GetEvent<KeyPressedEvent>().Publish(keyCode);
            }
        }

        private void OnKeyReleased(object sender, KeyboardHookEventArgs e)
        {
            var keyCode = e?.Data.KeyCode.ToString();

            if (!string.IsNullOrWhiteSpace(keyCode))
            {
                eventAggregator.GetEvent<KeyReleasedEvent>().Publish(keyCode);
            }
        }

        #endregion Private Methods
    }
}