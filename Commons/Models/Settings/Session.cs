using System.Collections.Generic;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Models.Settings;

public class Session
{
    #region Public Properties

    public string ControllerName { get; set; }

    public int Height { get; set; } = Defaults.AppHeight;

    public List<Output> Outputs { get; set; } = [];

    public double PanelHeightBottom { get; set; } = Defaults.PanelHeight;

    public double PanelHeightTop { get; set; } = Defaults.PanelHeight;

    public double PanelWidthLeft { get; set; } = Defaults.PanelWidth;

    public double PanelWidthRight { get; set; } = Defaults.PanelWidth;

    public List<Position> Positions { get; set; } = [];

    public string SettingsPath { get; set; }

    public int Width { get; set; } = Defaults.AppWidth;

    public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

    #endregion Public Properties
}