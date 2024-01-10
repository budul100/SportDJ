using System.Collections.Generic;

namespace SportDJ.Commons.Models.Settings
{
    public class Session
    {
        #region Public Properties

        public int Height { get; set; } = Defaults.AppHeightDefault;

        public List<Position> Positions { get; set; } = new List<Position>();

        public int Width { get; set; } = Defaults.AppWidthDefault;

        public string WindowState { get; set; } = nameof(Avalonia.Controls.WindowState.Maximized);

        #endregion Public Properties
    }
}