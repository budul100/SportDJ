using System;
using System.Collections.Generic;
using Avalonia.Controls;
using SportDJ.Commons.Enums;
using SportDJ.Commons.Events.Aggregation;
using SportDJ.Commons.Events.Panel;
using SportDJ.Commons.Interfaces;

namespace SportDJ.Services.PanelService;

public class Service(IEventAggregator eventAggregator)
    : IPanelService
{
    #region Private Fields

    private readonly Dictionary<PanelType, Control> activeViews = [];
    private readonly Dictionary<Type, Registration> registrations = [];

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
            activeViews[registration.Type] = registration.GetView();

            SetVisibility(
                type: registration.Type,
                isVisible: true);

            var payload = (registration.Type, serviceType);

            registration.Action?.Invoke();

            eventAggregator.GetEvent<PanelUpdatedEvent>().Publish(
                payload: payload);
        }
    }

    public void Deactivate(PanelType type)
    {
        if (activeViews.Remove(type))
        {
            SetVisibility(
                type: type,
                isVisible: false);

            var payload = (type, default(Type));

            eventAggregator.GetEvent<PanelUpdatedEvent>().Publish(
                payload: payload);
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

    public void Register<TService>(PanelType type, Control view, Action action = default)
    {
        registrations[typeof(TService)] = new Registration(
            type: type,
            viewFactory: () => view,
            action: action);
    }

    public void Register<TService>(PanelType type, Func<Control> viewFactory, Action action = default)
    {
        registrations[typeof(TService)] = new Registration(
            type: type,
            viewFactory: viewFactory,
            action: action);
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
