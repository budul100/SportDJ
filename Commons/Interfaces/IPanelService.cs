using System;
using Avalonia.Controls;
using SportDJ.Commons.Enums;

namespace SportDJ.Commons.Interfaces;

public interface IPanelService
{
    #region Public Methods

    void Activate<TService>();

    void Deactivate<TService>();

    void Deactivate(PanelType type);

    Control GetView(PanelType type);

    bool IsVisible(PanelType type);

    bool IsVisible<TService>();

    /// <summary>
    /// Creates the view of a registered service now, so the first activation is fast.
    /// </summary>
    void Preload<TService>();

    void Register<TService>(PanelType type, Control view, Action action = default);

    /// <summary>
    /// Registers a view which is created on first activation, so expensive views do not delay the startup.
    /// </summary>
    void Register<TService>(PanelType type, Func<Control> viewFactory, Action action = default);

    #endregion Public Methods
}