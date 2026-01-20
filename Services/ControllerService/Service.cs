using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NAudio.Midi;
using Prism.Events;
using SportDJ.Commons.Events.App;
using SportDJ.Commons.Events.Device;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Records;
using SportDJ.Commons.Models.Settings;
using SportDJ.Services.ControllerService.Exceptions;

namespace SportDJ.Services.ControllerService
{
    public class Service
        : IControllerService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IStorageService<Session> sessionService;

        private bool isDisposed;
        private MidiIn midiIn;

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Session> sessionService)
        {
            this.eventAggregator = eventAggregator;
            this.sessionService = sessionService;

            eventAggregator?.GetEvent<AppInitializedEvent>().Subscribe(
                action: InitializeController,
                keepSubscriberReferenceAlive: true);

            eventAggregator?.GetEvent<SettingsLoadedEvent<Session>>().Subscribe(
                action: InitializeController,
                keepSubscriberReferenceAlive: true);

            Update();
        }

        #endregion Public Constructors

        #region Public Properties

        public Controller Controller { get; private set; }

        public IEnumerable<Controller> Controllers { get; private set; } = [];

        public bool IsActive => midiIn != default;

        #endregion Public Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(
                disposing: true);

            GC.SuppressFinalize(
                obj: this);
        }

        public void Set(string name)
        {
            var controller = default(Controller);

            if (!string.IsNullOrWhiteSpace(name))
            {
                if (Controllers.Any())
                {
                    controller = Controllers.SingleOrDefault(d => d.Name == name);
                }
                else
                {
                    throw new NoControllerAvailableException();
                }
            }

            if (controller != Controller)
            {
                CloseController();

                SetController(controller);

                sessionService.Contents.ControllerName = controller.Name;

                sessionService.Save();

                eventAggregator.GetEvent<ControllerUpdatedEvent>().Publish();
            }
        }

        public void Update()
        {
            Controllers = GetControllers().ToArray();

            eventAggregator?.GetEvent<ControllersUpdatedEvent>().Publish();
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    CloseController();
                }

                isDisposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        private static IEnumerable<Controller> GetControllers()
        {
            for (var index = 0; index < MidiIn.NumberOfDevices; index++)
            {
                var deviceInfo = MidiIn.DeviceInfo(index);

                var result = new Controller(
                    deviceInfo.ProductName,
                    index);

                yield return result;
            }
        }

        private void CloseController()
        {
            midiIn?.Dispose();

            midiIn = default;
        }

        private void InitializeController()
        {
            var name = sessionService?.Contents?.ControllerName;

            Set(name);
        }

        private void SetController(Controller controller)
        {
            if (controller != default)
            {
                try
                {
                    midiIn = new MidiIn(controller.Number);

                    Controller = controller;
                }
                catch
                {
                    Debugger.Break();

                    // TODO: Log exception
                }
            }
        }

        #endregion Private Methods
    }
}