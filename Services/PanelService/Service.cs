using System.Collections.Generic;
using Avalonia.Controls;
using Prism.Events;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Events.Settings;
using SportDJ.Commons.Interfaces;
using SportDJ.Commons.Models.Settings;

namespace SportDJ.Services.PanelService
{
    public class Service
        : IPanelService
    {
        #region Private Fields

        private readonly IEventAggregator eventAggregator;
        private readonly IStorageService<Session> sessionService;
        private readonly Dictionary<PanelType, Control> views = [];

        #endregion Private Fields

        #region Public Constructors

        public Service(IEventAggregator eventAggregator, IStorageService<Session> sessionService)
        {
            this.eventAggregator = eventAggregator;
            this.sessionService = sessionService;

            eventAggregator.GetEvent<StackVisibleEvent>().Subscribe(
                action: ShowStack,
                threadOption: ThreadOption.PublisherThread,
                keepSubscriberReferenceAlive: true);
        }

        #endregion Public Constructors

        #region Public Properties

        public PanelType Type { get; private set; }

        public Control View { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public void Register(PanelType panelType, Control view)
        {
            views[panelType] = view;
        }

        #endregion Public Methods

        #region Private Methods

        private void ShowStack(IStackService service)
        {
            Type = service == default
                ? PanelType.None
                : PanelType.Stack;

            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            View = views.TryGetValue(
                key: Type,
                value: out var view)
                ? view
                : default;

            eventAggregator.GetEvent<PanelUpdatedEvent>().Publish(Type);

            sessionService.Contents.PanelVisible = Type != PanelType.None;

            sessionService.Save();

            eventAggregator.GetEvent<SettingsUpdatedEvent<Session>>().Publish();
        }

        #endregion Private Methods
    }
}