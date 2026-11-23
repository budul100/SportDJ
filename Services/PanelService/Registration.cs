using System;
using Avalonia.Controls;
using SportDJ.Commons.Enums;

namespace SportDJ.Services.PanelService;

internal sealed class Registration(PanelType type, Func<Control> viewFactory, Action action)
{
    #region Private Fields

    private Control view;

    #endregion Private Fields

    #region Public Properties

    public Action Action { get; } = action;

    public PanelType Type { get; } = type;

    #endregion Public Properties

    #region Public Methods

    public Control GetView()
    {
        // Created on first use and then kept
        return view ??= viewFactory.Invoke();
    }

    #endregion Public Methods
}
