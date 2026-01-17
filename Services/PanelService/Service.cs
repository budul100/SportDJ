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

        public Control GetView(PanelType region)
        {
            return activeViews.TryGetValue(
                key: region,
                value: out var view)
                ? view
                : default;
        }

        public bool IsVisible(PanelType region)
        {
            return region switch
            {
                PanelType.Top => visibleTop,

                PanelType.Bottom => visibleBottom,

                PanelType.Left => visibleLeft,

                PanelType.Right => visibleRight,

                _ => false
            };
        }

        public void Register<TService>(PanelType region, Control view)
        {
            registrations[typeof(TService)] = (region, view);
        }

        public void Switch<TService>(TService service = default)
        {
            var serviceType = typeof(TService);

            if (registrations.TryGetValue(
                key: serviceType,
                value: out var registration))
            {
                if (service == null)
                {
                    activeViews.Remove(registration.Type);

                    SetVisibility(
                        type: registration.Type,
                        isVisible: false);
                }
                else
                {
                    activeViews[registration.Type] = registration.View;

                    SetVisibility(
                        type: registration.Type,
                        isVisible: true);
                }

                eventAggregator.GetEvent<PanelUpdatedEvent>().Publish(
                    payload: registration.Type);
            }
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