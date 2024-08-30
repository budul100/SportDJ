using System;
using System.Collections.Generic;
using Prism.Events;
using SharpHook;
using SportDJ.Commons.Events.Grid;
using SportDJ.Commons.Events.Hook;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.Services.HookService
{
    public class Service
        : IHookService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly HashSet<object> locks = [];
        private readonly IStorageService<Set> settingsService;

        private Action<string> callback;
        private TaskPoolGlobalHook hook;
        private bool isDisposed;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Set> settingsService)
        {
            this.eventAggregator = eventAggregator;
            this.settingsService = settingsService;

            eventAggregator.GetEvent<SettingsLoadedEvent<Set>>().Subscribe(
                action: CheckState,
                keepSubscriberReferenceAlive: true);

            eventAggregator.GetEvent<SettingsUpdatedEvent<Set>>().Subscribe(
                action: CheckState,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public bool IsAvailable => hook?.IsDisposed == false;

        public bool IsRunning => IsAvailable
            && hook.IsRunning;

        #endregion Public Properties

        #region Public Methods

        public void Activate(object obj)
        {
            locks.Remove(obj);

            if (locks.Count == 0)
            {
                ActivateHook();
            }
        }

        public void Deactivate(object obj)
        {
            if (!locks.Contains(obj))
            {
                DeactivateHook();

                locks.Add(obj);
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
                    DeactivateHook();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private void ActivateHook()
        {
            if (hook == default)
            {
                hook = new();

                hook.KeyPressed += OnKeyPressed;
                hook.KeyReleased += OnKeyReleased;

                hook.HookEnabled += OnHookEnabled;

                hook.RunAsync();

                eventAggregator.GetEvent<HookUpdatedEvent>().Publish();
            }
        }

        private void CheckState()
        {
            if (settingsService?.Contents?.Options != default)
            {
                if (settingsService.Contents.Options.EnableKeyHook)
                {
                    Activate(settingsService);
                }
                else
                {
                    Deactivate(settingsService);
                }
            }
        }

        private void DeactivateHook()
        {
            if (hook != default)
            {
                hook.KeyPressed -= OnKeyPressed;
                hook.KeyReleased -= OnKeyReleased;

                hook.HookEnabled -= OnHookEnabled;

                hook.Dispose();

                hook = default;

                eventAggregator.GetEvent<HookUpdatedEvent>().Publish();
            }
        }

        private void OnHookEnabled(object sender, HookEventArgs e)
        {
            eventAggregator.GetEvent<HookUpdatedEvent>().Publish();
        }

        private void OnKeyPressed(object sender, KeyboardHookEventArgs e)
        {
            if (locks.Count == 0)
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
            if (locks.Count == 0)
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