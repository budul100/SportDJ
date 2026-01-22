using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Prism.Events;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.PanelService
{
    public class Service(IEventAggregator eventAggregator)
    : IPanelService
    {
        #region Private Fields

        private readonly Dictionary<PanelType, Control> activeViews = [];
        private readonly Dictionary<Type, (PanelType Type, Control View)> registrations = [];

        private bool visibleBottom;
        private bool visibleLeft;
        private bool visibleRight;
        private bool visibleTop;

        #endregion Private Fields

        #region Public Methods

        public void Activate<TService>()
        {
            var serviceType = typeof(TService);

            if (registrations.TryGetValue(
            key: serviceType,
            value: out var registration))
            {
                activeViews[registration.Type] = registration.View;

                SetVisibility(
                type: registration.Type,
                isVisible: true);

                eventAggregator.GetEvent<PanelUpdatedEvent>().Publish(
                    payload: registration.Type);
            }
        }

        public void Deactivate(PanelType type)
        {
            if (activeViews.Remove(type))
            {
                SetVisibility(
                type: type,
                isVisible: false);

                eventAggregator.GetEvent<PanelUpdatedEvent>().Publish(
                    payload: type);
            }
        }

        public void Deactivate<TService>()
        {
            var serviceType = typeof(TService);

            if (registrations.TryGetValue(
            key: serviceType,
            value: out var registration))
            {
                Deactivate(registration.Type);
            }
        }

        public Control GetView(PanelType type)
        {
            return activeViews.TryGetValue(
            key: type,
            value: out var view)
            ? view
            : default;
        }

        public bool IsVisible(PanelType type)
        {
            return type switch
            {
                PanelType.Top => visibleTop,

                PanelType.Bottom => visibleBottom,

                PanelType.Left => visibleLeft,

                PanelType.Right => visibleRight,

                _ => false
            };
        }

        public bool IsVisible<TService>()
        {
            var result = false;

            var serviceType = typeof(TService);

            if (registrations.TryGetValue(
            key: serviceType,
            value: out var registration))
            {
                result = activeViews.ContainsKey(registration.Type);
            }

            return result;
        }

        public void Register<TService>(PanelType type, Control view)
        {
            registrations[typeof(TService)] = (type, view);
        }

        #endregion Public Methods

        #region Private Methods

        private void SetVisibility(PanelType type, bool isVisible)
        {
            switch (type)
            {
                case PanelType.Top:
                    visibleTop = isVisible;
                    break;

                case PanelType.Bottom:
                    visibleBottom = isVisible;
                    break;

                case PanelType.Left:
                    visibleLeft = isVisible;
                    break;

                case PanelType.Right:
                    visibleRight = isVisible;
                    break;
            }
        }

        #endregion Private Methods
    }
}