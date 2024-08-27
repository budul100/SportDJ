using Prism.Events;
using SharpHook;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportDJ.Services.HookService
{
    public class Service
        : IHookService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly TaskPoolGlobalHook hook = new();
        private readonly HashSet<object> locks = new();
        private readonly IStorageService<Set> settingsService;

        private Action<string> callback;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IDispatcherService dispatcherService,
            IStorageService<Set> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: () => CheckState(),
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SettingsUpdatedEvent<Set>>().Subscribe(
                action: () => CheckState(),
                keepSubscriberReferenceAlive: true);

            hook.KeyPressed += OnKeyPressed;
            hook.KeyReleased += OnKeyReleased;

            Task.Run(() => hook.Run());
        }

        #endregion Public Constructors

        #region Public Methods

        public void Activate(object obj)
        {
            if (!hook.IsRunning)
                {
                    hook.KeyPressed += OnKeyPressed;
                    hook.KeyReleased += OnKeyReleased;
                }
            }
        }

        public void Deactivate(object obj)
        {
            if (!locks.Contains(obj))
            {
                locks.Add(obj);

                if (locks.Count == 1)
                {
                    hook.KeyPressed -= OnKeyPressed;
                    hook.KeyReleased -= OnKeyReleased;
                }
            }
        }

        public void Dispose()
        {
            Dispose(
                isDisposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Listen(Action<string> callback)
        {
            if (this.callback == default)
            {
                this.callback = callback;
                hook.KeyTyped += OnListening;
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
                    hook.KeyPressed -= OnKeyPressed;
                    hook.KeyReleased -= OnKeyReleased;

                    hook.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void CheckState()
        {
            if (settingsService?.Contents?.Options != default)
            {
                if (settingsService.Contents.Options.DisableKeyHook)
                {
                    Activate(settingsService);
                }
                else
                {
                    Deactivate(settingsService);
                }
            }
        }

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (!locks.Any())
            {
                var keyCode = e?.Data.KeyCode.ToString();

                if (!string.IsNullOrWhiteSpace(keyCode))
                {
                    eventAggregator.GetEvent<KeyPressedEvent>().Publish(keyCode);
                }
            }
        }

        private void OnKeyReleased(object sender, KeyboardHookEventArgs e)
        {
            if (!locks.Any())
            {
                var keyCode = e?.Data.KeyCode.ToString();

                if (!string.IsNullOrWhiteSpace(keyCode))
                {
                    eventAggregator.GetEvent<KeyReleasedEvent>().Publish(keyCode);
                }
            }
        }

        private void OnListening(object sender, KeyboardHookEventArgs e)
        {
            var keyCode = e?.Data.KeyCode.ToString();

            if (callback != default
                && !string.IsNullOrWhiteSpace(keyCode))
            {
                callback.Invoke(keyCode);

                hook.KeyTyped -= OnListening;
                callback = default;
            }
        }

        #endregion Private Methods
    }
}