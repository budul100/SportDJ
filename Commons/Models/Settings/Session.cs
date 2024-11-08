using System.Collections.Generic;
using SportDJ.Commons.Assets;

namespace SportDJ.Commons.Models.Settings
{
    public class Session
    {
        #region Public Properties

        public int Height { get; set; } = Defaults.AppHeightDefault;

        public List<Position> Positions { get; set; } = [];

        public string SettingsPath { get; set; }

        public bool SettingsVisible { get; set; } = true;

        public double SettingsWidth { get; set; } = Defaults.SettingsWidth;

        public int Width { get; set; } = Defaults.AppWidthDefault;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}